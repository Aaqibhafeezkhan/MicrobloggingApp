using MicrobloggingApp.Frontend.DTOs;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;

namespace MicrobloggingApp.Frontend.Pages
{
    public class TimelineModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TimelineModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<PostDto>? Posts { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? Search { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public async Task OnGetAsync(int page = 1, string? search = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            CurrentPage = Math.Max(page, 1);
            Search = search;
            StartDate = startDate;
            EndDate = endDate;

            var httpClient = _httpClientFactory.CreateClient("MicrobloggingAPI");
            var query = $"api/posts?page={CurrentPage}&search={WebUtility.UrlEncode(search ?? string.Empty)}" +
                        $"&startDate={(startDate.HasValue ? startDate.Value.ToString("yyyy-MM-dd") : string.Empty)}" +
                        $"&endDate={(endDate.HasValue ? endDate.Value.ToString("yyyy-MM-dd") : string.Empty)}";

            var response = await httpClient.GetFromJsonAsync<PaginatedResponse>(query);
            Posts = response?.Posts;
            TotalPages = response?.TotalPages ?? 0;
        }
    }
}
