using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MetroTicketClearingWeb.Models;
using MySql.Data.MySqlClient;

namespace MetroTicketClearingWeb.Pages
{
    public class DataImportModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public DataImportResult Result { get; set; } = new();

        public DataImportModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPostImport()
        {
            string? connStr = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connStr))
            {
                Result.Success = false;
                Result.Message = "未找到数据库连接字符串 DefaultConnection。";
                return Page();
            }

            try
            {
                using var conn = new MySqlConnection(connStr);
                conn.Open();

                using var transaction = conn.BeginTransaction();

                try
                {
                    ExecuteNonQuery(conn, transaction, "DELETE FROM ticket_transaction");
                    ExecuteNonQuery(conn, transaction, "DELETE FROM section_info");
                    ExecuteNonQuery(conn, transaction, "DELETE FROM station_info");
                    ExecuteNonQuery(conn, transaction, "DELETE FROM line_info");
                    ExecuteNonQuery(conn, transaction, "DELETE FROM operator_info");

                    ExecuteNonQuery(conn, transaction, @"
                        INSERT INTO operator_info (operator_code, operator_name, contact_person, contact_phone, created_at)
                        VALUES
                        ('OP001', '地铁运营公司A', '张三', '13800000001', NOW()),
                        ('OP002', '地铁运营公司B', '李四', '13800000002', NOW())");

                    ExecuteNonQuery(conn, transaction, @"
                        INSERT INTO line_info (line_code, line_name, operator_id, created_at)
                        VALUES
                        ('L01', '1号线', 1, NOW()),
                        ('L02', '2号线', 1, NOW()),
                        ('L03', '3号线', 2, NOW())");

                    ExecuteNonQuery(conn, transaction, @"
                        INSERT INTO station_info (station_code, station_name, is_transfer, created_at)
                        VALUES
                        ('S001', '火车站', 1, NOW()),
                        ('S002', '人民广场', 1, NOW()),
                        ('S003', '大学城', 0, NOW()),
                        ('S004', '体育中心', 0, NOW()),
                        ('S005', '机场东', 0, NOW())");

                    ExecuteNonQuery(conn, transaction, @"
                        INSERT INTO section_info (line_id, from_station_id, to_station_id, distance_km, is_bidirectional)
                        VALUES
                        (1, 1, 2, 3.50, 1),
                        (1, 2, 3, 4.20, 1),
                        (2, 2, 4, 5.10, 1),
                        (3, 4, 5, 8.30, 1)");

                    ExecuteNonQuery(conn, transaction, @"
                        INSERT INTO ticket_transaction
                        (card_no, entry_time, entry_station_id, exit_time, exit_station_id, pay_amount, payment_type, transaction_type, transaction_status, exception_type, created_at)
                        VALUES
                        ('CARD001', '2026-04-06 08:00:00', 1, '2026-04-06 08:35:00', 3, 4.00, 'CARD', 'EXIT', 'NORMAL', NULL, NOW()),
                        ('CARD002', '2026-04-06 09:10:00', 2, '2026-04-06 09:40:00', 4, 3.00, 'QR', 'EXIT', 'NORMAL', NULL, NOW()),
                        ('CARD003', '2026-04-06 10:00:00', 4, '2026-04-06 10:50:00', 5, 5.00, 'CARD', 'EXIT', 'NORMAL', NULL, NOW())");

                    transaction.Commit();

                    Result.Success = true;
                    Result.Message = "测试数据导入成功。";
                    Result.OperatorCount = GetCount(conn, "operator_info");
                    Result.LineCount = GetCount(conn, "line_info");
                    Result.StationCount = GetCount(conn, "station_info");
                    Result.SectionCount = GetCount(conn, "section_info");
                    Result.TransactionCount = GetCount(conn, "ticket_transaction");
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Result.Success = false;
                Result.Message = $"导入失败：{ex.Message}";
            }

            return Page();
        }

        private static void ExecuteNonQuery(MySqlConnection conn, MySqlTransaction transaction, string sql)
        {
            using var cmd = new MySqlCommand(sql, conn, transaction);
            cmd.ExecuteNonQuery();
        }

        private static int GetCount(MySqlConnection conn, string tableName)
        {
            using var cmd = new MySqlCommand($"SELECT COUNT(*) FROM `{tableName}`", conn);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}