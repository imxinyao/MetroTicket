using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

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

        public TimetableHeaderDto? SelectedTimetable { get; set; }

        public List<TimetableTrainGroupDto> GroupedDetails { get; set; } = new();

        public string? ErrorMessage { get; set; }

        [BindProperty]
        public string TimetableCode { get; set; } = string.Empty;

        [BindProperty]
        public string TimetableName { get; set; } = string.Empty;

        [BindProperty]
        public long LineId { get; set; }

        [BindProperty]
        public int Direction { get; set; } = 1;

        [BindProperty]
        public string VersionNo { get; set; } = string.Empty;

        [BindProperty]
        [DataType(DataType.Date)]
        public DateTime EffectiveStartDate { get; set; } = DateTime.Today;

        [BindProperty]
        [DataType(DataType.Date)]
        public DateTime EffectiveEndDate { get; set; } = DateTime.Today.AddMonths(1);

        [BindProperty]
        public string RunCalendarType { get; set; } = "DAILY";

        [BindProperty]
        public string? Remark { get; set; }

        [BindProperty]
        public IFormFile? UploadFile { get; set; }

        public string? ImportMessage { get; set; }

        public bool ImportSuccess { get; set; }

        public async Task OnGetAsync()
        {
            await LoadTimetablesAsync();

            if (TimetableId.HasValue)
            {
                await LoadDetailsAsync(TimetableId.Value);
            }
            else if (Timetables.Count > 0)
            {
                TimetableId = Timetables
                    .OrderBy(t => t.TimetableId)
                    .First()
                    .TimetableId;

                await LoadDetailsAsync(TimetableId.Value);
            }

            BuildSelectedTimetable();
            BuildGroupedDetails();
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

        public async Task<IActionResult> OnPostDeleteAsync(long timetableId)
        {
            var client = CreateApiClient();
            var response = await client.PostAsync($"/api/timetables/{timetableId}/delete", null);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = $"删除失败，HTTP状态码：{(int)response.StatusCode}";
                await LoadTimetablesAsync();

                if (TimetableId.HasValue)
                {
                    await LoadDetailsAsync(TimetableId.Value);
                }

                BuildSelectedTimetable();
                BuildGroupedDetails();
                return Page();
            }

            return RedirectToPage("/Timetable");
        }

        public async Task<IActionResult> OnPostImportAsync()
        {
            await LoadTimetablesAsync();

            if (TimetableId.HasValue)
            {
                await LoadDetailsAsync(TimetableId.Value);
            }

            if (UploadFile == null || UploadFile.Length == 0)
            {
                ImportSuccess = false;
                ImportMessage = "请选择要上传的 CSV 文件。";
                BuildSelectedTimetable();
                BuildGroupedDetails();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(TimetableCode) ||
                string.IsNullOrWhiteSpace(TimetableName) ||
                string.IsNullOrWhiteSpace(VersionNo))
            {
                ImportSuccess = false;
                ImportMessage = "时刻表编号、时刻表名称、版本号不能为空。";
                BuildSelectedTimetable();
                BuildGroupedDetails();
                return Page();
            }

            try
            {
                var client = CreateApiClient();

                using var form = new MultipartFormDataContent();

                form.Add(new StringContent(TimetableCode), "TimetableCode");
                form.Add(new StringContent(TimetableName), "TimetableName");
                form.Add(new StringContent(LineId.ToString()), "LineId");
                form.Add(new StringContent(Direction.ToString()), "Direction");
                form.Add(new StringContent(VersionNo), "VersionNo");
                form.Add(new StringContent(EffectiveStartDate.ToString("yyyy-MM-dd")), "EffectiveStartDate");
                form.Add(new StringContent(EffectiveEndDate.ToString("yyyy-MM-dd")), "EffectiveEndDate");
                form.Add(new StringContent(RunCalendarType), "RunCalendarType");
                form.Add(new StringContent(Remark ?? string.Empty), "Remark");

                using var stream = UploadFile.OpenReadStream();
                using var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
                form.Add(fileContent, "File", UploadFile.FileName);

                var response = await client.PostAsync("/api/timetables/import", form);
                var resultText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ImportSuccess = true;
                    ImportMessage = $"导入成功：{resultText}";

                    await LoadTimetablesAsync();

                    var matched = Timetables
                        .Where(t => t.LineId == LineId && t.Direction == Direction && t.VersionNo == VersionNo)
                        .OrderByDescending(t => t.TimetableId)
                        .FirstOrDefault();

                    if (matched != null)
                    {
                        TimetableId = matched.TimetableId;
                        await LoadDetailsAsync(matched.TimetableId);
                    }
                }
                else
                {
                    ImportSuccess = false;
                    ImportMessage = $"导入失败：{resultText}";
                }
            }
            catch (Exception ex)
            {
                ImportSuccess = false;
                ImportMessage = $"导入失败：{ex.Message}";
            }

            BuildSelectedTimetable();
            BuildGroupedDetails();
            return Page();
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

                Details = Details
                    .OrderBy(d => d.TrainNo)
                    .ThenBy(d => d.StationSeq)
                    .ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"加载时刻表明细异常：{ex.Message}";
            }
        }

        private void BuildSelectedTimetable()
        {
            if (!TimetableId.HasValue || Timetables.Count == 0)
            {
                SelectedTimetable = null;
                return;
            }

            SelectedTimetable = Timetables
                .FirstOrDefault(t => t.TimetableId == TimetableId.Value);
        }

        private void BuildGroupedDetails()
        {
            GroupedDetails = Details
                .GroupBy(d => d.TrainNo ?? string.Empty)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var items = g
                        .OrderBy(x => x.StationSeq)
                        .ToList();

                    var startStation = items.FirstOrDefault()?.StationName ?? "-";
                    var endStation = items.LastOrDefault()?.StationName ?? "-";

                    return new TimetableTrainGroupDto
                    {
                        TrainNo = string.IsNullOrWhiteSpace(g.Key) ? "未命名车次" : g.Key,
                        StartStationName = startStation,
                        EndStationName = endStation,
                        Items = items
                    };
                })
                .ToList();
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
            public string LineName { get; set; } = "";
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

        public class TimetableTrainGroupDto
        {
            public string TrainNo { get; set; } = "";
            public string StartStationName { get; set; } = "-";
            public string EndStationName { get; set; } = "-";
            public List<TimetableDetailDto> Items { get; set; } = new();
        }
    }
}