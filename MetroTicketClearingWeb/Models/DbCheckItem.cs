namespace MetroTicketClearingWeb.Models
{
    public class DbCheckItem
    {
        public string TableName { get; set; } = string.Empty;
        public bool Exists { get; set; }
        public int RecordCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}