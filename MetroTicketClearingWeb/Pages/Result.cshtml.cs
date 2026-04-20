using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MetroTicketClearingWeb.Pages
{
    public class ResultModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ResultModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ApiBaseUrl { get; private set; } = string.Empty;

        public void OnGet()
        {
            ApiBaseUrl = (_configuration["ApiSettings:BaseUrl"] ?? string.Empty).TrimEnd('/');
        }
    }
}