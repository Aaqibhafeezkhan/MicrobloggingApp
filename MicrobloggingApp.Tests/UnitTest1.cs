using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MicrobloggingApp.API.Controllers;
using MicrobloggingApp.API.DTOs;
using MicrobloggingApp.API.Helpers;
using MicrobloggingApp.API.Hubs;
using MicrobloggingApp.API.Services.Interfaces;
using MicrobloggingApp.Core;
using MicrobloggingApp.Data;
using Moq;

namespace MicrobloggingApp.Tests
{
    public class JwtTokenHelperTests
    {
        private const string SecretKey = "01234567890123456789012345678901";
        private const string Issuer = "MicrobloggingApp";
        private const string Audience = "MicrobloggingApp.Client";

        [Fact]
        public void GenerateToken_UsesConfiguredIssuerAudienceAndUsername()
        {
            var helper = new JwtTokenHelper(CreateConfiguration());

            var token = helper.GenerateToken("aqib");
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);

            Assert.Equal("aqib", principal.FindFirstValue(ClaimTypes.Name));
        }

        [Fact]
        public void Constructor_ThrowsWhenIssuerIsMissing()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"] = SecretKey,
                    ["JwtSettings:Audience"] = Audience
                })
                .Build();

            var exception = Assert.Throws<InvalidOperationException>(() => new JwtTokenHelper(configuration));

            Assert.Equal("JwtSettings:Issuer is required.", exception.Message);
        }

        private static IConfiguration CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"] = SecretKey,
                    ["JwtSettings:Issuer"] = Issuer,
                    ["JwtSettings:Audience"] = Audience
                })
                .Build();
        }
    }

    public class LoginControllerValidationTests
    {
        [Fact]
        public void Login_ReturnsBadRequestForNullRequest()
        {
            var userService = new Mock<IUserService>();
            var controller = new LoginController(userService.Object, null!);

            var result = controller.Login(null!);

            Assert.IsType<BadRequestObjectResult>(result);
            userService.Verify(service => service.ValidateUser(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("", "password")]
        [InlineData("aqib", "")]
        [InlineData(" ", "password")]
        [InlineData("aqib", " ")]
        public void Login_ReturnsBadRequestForBlankCredentials(string username, string password)
        {
            var userService = new Mock<IUserService>();
            var controller = new LoginController(userService.Object, null!);

            var result = controller.Login(new LoginRequest { Username = username, Password = password });

            Assert.IsType<BadRequestObjectResult>(result);
            userService.Verify(service => service.ValidateUser(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }

    public class PostsControllerValidationTests
    {
        [Fact]
        public async Task CreatePost_ReturnsBadRequestForNullRequest()
        {
            var postService = new Mock<IPostService>();
            var userService = new Mock<IUserService>();
            var controller = CreateController(postService, userService);

            var result = await controller.CreatePost(null!);

            Assert.IsType<BadRequestObjectResult>(result);
            postService.Verify(service => service.CreatePost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task CreatePost_ReturnsBadRequestForBlankText()
        {
            var postService = new Mock<IPostService>();
            var userService = new Mock<IUserService>();
            var controller = CreateController(postService, userService);

            var result = await controller.CreatePost(new CreatePostRequest { Text = " " });

            Assert.IsType<BadRequestObjectResult>(result);
            postService.Verify(service => service.CreatePost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void EditPost_ReturnsBadRequestForNullRequest()
        {
            var postService = new Mock<IPostService>();
            var userService = new Mock<IUserService>();
            var controller = CreateController(postService, userService);

            var result = controller.EditPost(1, null!);

            Assert.IsType<BadRequestObjectResult>(result);
            postService.Verify(service => service.GetPostById(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void GetTimeline_ReturnsBadRequestForUnsupportedScreenSize()
        {
            var controller = CreateController(new Mock<IPostService>(), new Mock<IUserService>());

            var result = controller.GetTimeline(screenSize: "tablet");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void GetTimeline_ReturnsBadRequestForInvalidDateRange()
        {
            var controller = CreateController(new Mock<IPostService>(), new Mock<IUserService>());

            var result = controller.GetTimeline(
                startDate: new DateTime(2026, 10, 2),
                endDate: new DateTime(2026, 10, 1));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        private static PostsController CreateController(Mock<IPostService> postService, Mock<IUserService> userService)
        {
            return new PostsController(
                postService.Object,
                userService.Object,
                new Mock<IBlobStorageService>().Object,
                new Mock<IHubContext<TimelineHub>>().Object);
        }
    }

    public class PostsControllerAuthorizationTests
    {
        [Fact]
        public void EditPost_AllowsOwner()
        {
            var postService = new Mock<IPostService>();
            var userService = new Mock<IUserService>();
            var post = new Post { Id = 1, UserId = 7, Text = "old" };
            postService.Setup(service => service.GetPostById(1)).Returns(post);
            userService.Setup(service => service.GetUserId("aqib")).Returns(7);

            var controller = CreateController(postService, userService);
            controller.ControllerContext.HttpContext.User = CreatePrincipal("aqib");

            var result = controller.EditPost(1, new EditPostRequest { Text = "new" });

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("new", post.Text);
            postService.Verify(service => service.UpdatePost(post), Times.Once);
        }

        [Fact]
        public void EditPost_ForbidsNonOwner()
        {
            var postService = new Mock<IPostService>();
            var userService = new Mock<IUserService>();
            var post = new Post { Id = 1, UserId = 7, Text = "old" };
            postService.Setup(service => service.GetPostById(1)).Returns(post);
            userService.Setup(service => service.GetUserId("aqib")).Returns(8);

            var controller = CreateController(postService, userService);
            controller.ControllerContext.HttpContext.User = CreatePrincipal("aqib");

            var result = controller.EditPost(1, new EditPostRequest { Text = "new" });

            Assert.IsType<ForbidResult>(result);
            Assert.Equal("old", post.Text);
            postService.Verify(service => service.UpdatePost(It.IsAny<Post>()), Times.Never);
        }

        [Fact]
        public void DeletePost_AllowsOwner()
        {
            var postService = new Mock<IPostService>();
            var userService = new Mock<IUserService>();
            var post = new Post { Id = 1, UserId = 7 };
            postService.Setup(service => service.GetPostById(1)).Returns(post);
            userService.Setup(service => service.GetUserId("aqib")).Returns(7);

            var controller = CreateController(postService, userService);
            controller.ControllerContext.HttpContext.User = CreatePrincipal("aqib");

            var result = controller.DeletePost(1);

            Assert.IsType<OkObjectResult>(result);
            postService.Verify(service => service.DeletePost(post), Times.Once);
        }

        [Fact]
        public void DeletePost_ForbidsNonOwner()
        {
            var postService = new Mock<IPostService>();
            var userService = new Mock<IUserService>();
            var post = new Post { Id = 1, UserId = 7 };
            postService.Setup(service => service.GetPostById(1)).Returns(post);
            userService.Setup(service => service.GetUserId("aqib")).Returns(8);

            var controller = CreateController(postService, userService);
            controller.ControllerContext.HttpContext.User = CreatePrincipal("aqib");

            var result = controller.DeletePost(1);

            Assert.IsType<ForbidResult>(result);
            postService.Verify(service => service.DeletePost(It.IsAny<Post>()), Times.Never);
        }

        [Fact]
        public void EditPost_ReturnsNotFoundForMissingPost()
        {
            var postService = new Mock<IPostService>();
            var userService = new Mock<IUserService>();
            postService.Setup(service => service.GetPostById(1)).Returns((Post?)null);

            var controller = CreateController(postService, userService);

            var result = controller.EditPost(1, new EditPostRequest { Text = "new" });

            Assert.IsType<NotFoundObjectResult>(result);
        }

        private static PostsController CreateController(Mock<IPostService> postService, Mock<IUserService> userService)
        {
            return new PostsController(
                postService.Object,
                userService.Object,
                new Mock<IBlobStorageService>().Object,
                new Mock<IHubContext<TimelineHub>>().Object);
        }

        private static ClaimsPrincipal CreatePrincipal(string username)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, username) },
                "Test"));
        }
    }
}
