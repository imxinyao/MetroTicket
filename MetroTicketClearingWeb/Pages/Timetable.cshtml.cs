using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MetroTicketClearingWeb.Pages
{
    public class TimetableModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public TimetableModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public long? TimetableId { get; set; }

        public List<TimetableHeaderDto> Timetables { get; set; } = new();
        public List<TimetableDetailDto> Details { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadTimetablesAsync();

            if (TimetableId.HasValue)
            {
                await LoadDetailsAsync(TimetableId.Value);
            }
            else if (Timetables.Count > 0)
            {
                TimetableId = Timetables[0].TimetableId;
                await LoadDetailsAsync(TimetableId.Value);
            }
        }

        public async Task<IActionResult> OnPostPublishAsync(long timetableId)
        {
            var client = CreateApiClient();
            var response = await client.PostAsync($"/api/timetables/{timetableId}/publish", null);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = $"发布失败，HTTP状态码：{(int)response.StatusCode}";
            }

            return RedirectToPage("/Timetable", new { TimetableId = timetableId });
        }

        public async Task<IActionResult> OnPostDisableAsync(long timetableId)
        {
            var client = CreateApiClient();
            var response = await client.PostAsync($"/api/timetables/{timetableId}/disable", null);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = $"停用失败，HTTP状态码：{(int)response.StatusCode}";
            }

            return RedirectToPage("/Timetable", new { TimetableId = timetableId });
        }

        private async Task LoadTimetablesAsync()
        {
            try
            {
                var client = CreateApiClient();
                var response = await client.GetAsync("/api/timetables");

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage = $"加载时刻表列表失败，HTTP状态码：{(int)response.StatusCode}";
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                Timetables = JsonSerializer.Deserialize<List<TimetableHeaderDto>>(json, options) ?? new List<TimetableHeaderDto>();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"加载时刻表列表异常：{ex.Message}";
            }
        }

        private async Task LoadDetailsAsync(long timetableId)
        {
            try
            {
                var client = CreateApiClient();
                var response = await client.GetAsync($"/api/timetables/{timetableId}/details");

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage = $"加载时刻表明细失败，HTTP状态码：{(int)response.StatusCode}";
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                Details = JsonSerializer.Deserialize<List<TimetableDetailDto>>(json, options) ?? new List<TimetableDetailDto>();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"加载时刻表明细异常：{ex.Message}";
            }
        }

        private HttpClient CreateApiClient()
        {
            var client = _httpClientFactory.CreateClient();

            var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                throw new Exception("未配置 ApiSettings:BaseUrl");
            }

            client.BaseAddress = new Uri(apiBaseUrl);
            return client;
        }

        public string GetDirectionText(int direction)
        {
            return direction == 1 ? "上行" : direction == 2 ? "下行" : "未知";
        }

        public string GetStatusText(int status)
        {
            return status == 0 ? "草稿" : status == 1 ? "已发布" : status == 2 ? "停用" : "未知";
        }

        public string GetActiveText(int isActive)
        {
            return isActive == 1 ? "当前生效" : "非当前版本";
        }

        public class TimetableHeaderDto
        {
            public long TimetableId { get; set; }
            public string TimetableCode { get; set; } = "";
            public string TimetableName { get; set; } = "";
            public long LineId { get; set; }
            public int Direction { get; set; }
            public string DirectionText { get; set; } = "";
            public string VersionNo { get; set; } = "";
            public DateTime EffectiveStartDate { get; set; }
            public DateTime EffectiveEndDate { get; set; }
            public string RunCalendarType { get; set; } = "";
            public int Status { get; set; }
            public string StatusText { get; set; } = "";
            public int IsActive { get; set; }
            public string ActiveText { get; set; } = "";
            public string? Remark { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }

        public class TimetableDetailDto
        {
            public long DetailId { get; set; }
            public long TimetableId { get; set; }
            public string TrainNo { get; set; } = "";
            public long StationId { get; set; }
            public int StationSeq { get; set; }
            public string StationCode { get; set; } = "";
            public string StationName { get; set; } = "";
            public string? ArrivalTime { get; set; }
            public string? DepartureTime { get; set; }
            public int StopMinutes { get; set; }
            public int IsOriginStation { get; set; }
            public int IsTerminalStation { get; set; }
            public string OriginText { get; set; } = "";
            public string TerminalText { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }
    }
}