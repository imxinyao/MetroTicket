using Microsoft.AspNetCore.Mvc.RazorPages;
using MetroTicketClearingWeb.Models;
using MySql.Data.MySqlClient;

namespace MetroTicketClearingWeb.Pages
{
    public class DbCheckModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public bool DbConnected { get; set; }
        public string DbMessage { get; set; } = string.Empty;
        public List<DbCheckItem> Tables { get; set; } = new();

        public DbCheckModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            string? connStr = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connStr))
            {
                DbConnected = false;
                DbMessage = "未找到数据库连接字符串 DefaultConnection。";
                return;
            }

            try
            {
                using var conn = new MySqlConnection(connStr);
                conn.Open();

                DbConnected = true;
                DbMessage = "数据库连接成功。";

                string[] tableNames =
                {
                    "operator_info",
                    "line_info",
                    "station_info",
                    "section_info",
                    "ticket_transaction",
                    "train_timetable",
                    "train_timetable_detail"
                };

                foreach (var tableName in tableNames)
                {
                    var item = new DbCheckItem
                    {
                        TableName = tableName
                    };

                    string existsSql = @"
                        SELECT COUNT(*)
                        FROM information_schema.tables
                        WHERE table_schema = DATABASE()
                          AND table_name = @tableName;";

                    using (var existsCmd = new MySqlCommand(existsSql, conn))
                    {
                        existsCmd.Parameters.AddWithValue("@tableName", tableName);
                        var existsCount = Convert.ToInt32(existsCmd.ExecuteScalar());
                        item.Exists = existsCount > 0;
                    }

                    if (item.Exists)
                    {
                        string countSql = $"SELECT COUNT(*) FROM `{tableName}`;";
                        using var countCmd = new MySqlCommand(countSql, conn);
                        item.RecordCount = Convert.ToInt32(countCmd.ExecuteScalar());
                        item.Message = "表存在";
                    }
                    else
                    {
                        item.RecordCount = 0;
                        item.Message = "表不存在";
                    }

                    Tables.Add(item);
                }
            }
            catch (Exception ex)
            {
                DbConnected = false;
                DbMessage = $"数据库连接失败：{ex.Message}";
            }
        }
    }
}