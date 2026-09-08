using Hangfire;
using MicrobloggingApp.API.DTOs;
using MicrobloggingApp.API.Hubs;
using MicrobloggingApp.API.Services.Interfaces;
using MicrobloggingApp.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace MicrobloggingApp.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly IUserService _userService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IHubContext<TimelineHub> _hubContext;

        public PostsController(IPostService postService, IUserService userService, IBlobStorageService blobStorageService, IHubContext<TimelineHub> hubContext)
        {
            _postService = postService;
            _userService = userService;
            _blobStorageService = blobStorageService;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Post text is required.");

            if (request.Text.Length > 140)
                return BadRequest("Post text exceeds 140 characters.");

            if (request.Image == null)
                return BadRequest("An image is required.");

            if (request.Image.Length > 2 * 1024 * 1024)
                return BadRequest("Image size exceeds 2MB.");

            var username = User.Identity?.Name;
            var userId = username == null ? null : _userService.GetUserId(username);
            if (!userId.HasValue)
                return Unauthorized();

            await using var stream = request.Image.OpenReadStream();
            var originalImageUrl = await _blobStorageService.UploadFileAsync(request.Image.FileName, stream);

            var processedImageFileName = $"processed-{request.Image.FileName}";
            BackgroundJob.Enqueue<IImageProcessingService>(service =>
                service.ProcessImageAsync(originalImageUrl, processedImageFileName));

            _postService.CreatePost(request.Text, originalImageUrl, userId.Value);

            await _hubContext.Clients.All.SendAsync("ReceiveNewPost", request.Text, originalImageUrl, DateTime.UtcNow.ToString("g"));

            return Ok("Post created successfully.");
        }

        [HttpGet]
        public IActionResult GetTimeline(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string screenSize = "medium",
            [FromQuery] string search = "",
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var timeline = _postService.GetTimeline();

            if (!string.IsNullOrWhiteSpace(search))
                timeline = timeline.Where(post => post.Text.Contains(search, StringComparison.OrdinalIgnoreCase));

            if (startDate.HasValue)
                timeline = timeline.Where(post => post.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                timeline = timeline.Where(post => post.CreatedAt <= endDate.Value);

            var totalPosts = timeline.Count();
            var paginatedTimeline = timeline
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(post => new
                {
                    post.Text,
                    ImagePath = AdjustImageForScreenSize(post.ImagePath, screenSize),
                    post.Latitude,
                    post.Longitude,
                    post.CreatedAt
                });

            return Ok(new
            {
                TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize),
                CurrentPage = page,
                Posts = paginatedTimeline
            });
        }

        [HttpPut("{id}")]
        public IActionResult EditPost(int id, [FromBody] EditPostRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Post text is required.");

            var post = _postService.GetPostById(id);
            if (post == null)
                return NotFound("Post not found.");

            if (request.Text.Length > 140)
                return BadRequest("Post text exceeds 140 characters.");

            post.Text = request.Text;
            _postService.UpdatePost(post);

            return Ok("Post updated successfully.");
        }

        [HttpDelete("{id}")]
        public IActionResult DeletePost(int id)
        {
            var post = _postService.GetPostById(id);
            if (post == null)
                return NotFound("Post not found.");

            _postService.DeletePost(post);
            return Ok("Post deleted successfully.");
        }

        private string AdjustImageForScreenSize(string imagePath, string screenSize)
        {
            return screenSize switch
            {
                "small" => imagePath.Replace("processed", "processed-small"),
                "large" => imagePath.Replace("processed", "processed-large"),
                _ => imagePath
            };
        }
    }
}
