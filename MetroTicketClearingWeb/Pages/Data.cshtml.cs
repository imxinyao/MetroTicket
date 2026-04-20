using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MetroTicketClearingWeb.Pages
{
    public class DataModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public DataModel(IConfiguration configuration)
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