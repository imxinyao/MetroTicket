using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace MetroTicketClearingWeb.Pages
{
    public class DataImportModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public DataImportModel(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

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

        public string? Message { get; set; }

        public bool IsSuccess { get; set; }

        public void OnGet()
        {
        }

        public async Task OnPostAsync()
        {
            if (UploadFile == null || UploadFile.Length == 0)
            {
                IsSuccess = false;
                Message = "请选择要上传的 CSV 文件。";
                return;
            }

            if (string.IsNullOrWhiteSpace(TimetableCode) ||
                string.IsNullOrWhiteSpace(TimetableName) ||
                string.IsNullOrWhiteSpace(VersionNo))
            {
                IsSuccess = false;
                Message = "时刻表编号、时刻表名称、版本号不能为空。";
                return;
            }

            try
            {
                var apiBaseUrl = (_configuration["ApiSettings:BaseUrl"] ?? string.Empty).TrimEnd('/');
                var client = _httpClientFactory.CreateClient();

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

                var response = await client.PostAsync($"{apiBaseUrl}/api/timetables/import", form);
                var resultText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    IsSuccess = true;
                    Message = $"导入成功：{resultText}";
                }
                else
                {
                    IsSuccess = false;
                    Message = $"导入失败：{resultText}";
                }
            }
            catch (Exception ex)
            {
                IsSuccess = false;
                Message = $"导入失败：{ex.Message}";
            }
        }
    }
}