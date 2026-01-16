using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Antigravity.Api.Models;
using System.Data;

namespace Antigravity.Api.Controllers
{
    [ApiController]
    [Route("api/time")]
    public class TimeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public TimeController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        private string GetUserEmail() => "demo@example.com";

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // 1. Check if there is an open WorkDay
                var workDayQuery = @"
                    SELECT TOP 1 id, start_time, end_time 
                    FROM WorkDays 
                    WHERE user_email = @email AND end_time IS NULL 
                    ORDER BY start_time DESC";
                
                int? workDayId = null;
                DateTime? startTime = null;

                using (var command = new SqlCommand(workDayQuery, connection))
                {
                    command.Parameters.AddWithValue("@email", GetUserEmail());
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            workDayId = reader.GetInt32(0);
                            startTime = reader.GetDateTime(1);
                        }
                    }
                }

                if (workDayId == null)
                {
                    return Ok(new { isWorking = false });
                }

                // 2. Check if there is an active SiteVisit for this WorkDay
                var siteQuery = @"
                    SELECT TOP 1 id, site_name, check_in_time 
                    FROM SiteVisits 
                    WHERE work_day_id = @workDayId AND check_out_time IS NULL 
                    ORDER BY check_in_time DESC";

                object activeSite = null;
                using (var command = new SqlCommand(siteQuery, connection))
                {
                    command.Parameters.AddWithValue("@workDayId", workDayId.Value);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            activeSite = new
                            {
                                name = reader.GetString(1),
                                checkInTime = reader.GetDateTime(2)
                            };
                        }
                    }
                }

                return Ok(new
                {
                    isWorking = true,
                    startTime = startTime,
                    activeSite = activeSite
                });
            }
        }

        [HttpPost("workday/start")]
        public async Task<IActionResult> StartWorkDay([FromBody] LocationRequest loc)
        {
            // First check if already working
            // For simplicity, just insert a new one if no open one exists
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // Close any potentially open previous days (cleanup) or just insert new
                var query = @"
                    INSERT INTO WorkDays (user_email, start_time, start_lat, start_lng, status)
                    VALUES (@email, @start, @lat, @lng, 'ACTIVE')";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@email", GetUserEmail());
                    command.Parameters.AddWithValue("@start", DateTime.Now);
                    command.Parameters.AddWithValue("@lat", loc.Lat ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@lng", loc.Lng ?? (object)DBNull.Value);
                    
                    await command.ExecuteNonQueryAsync();
                }
            }
            return Ok();
        }

        [HttpPost("workday/end")]
        public async Task<IActionResult> EndWorkDay([FromBody] LocationRequest loc)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                    UPDATE WorkDays 
                    SET end_time = @end, end_lat = @lat, end_lng = @lng, status = 'COMPLETED'
                    WHERE user_email = @email AND end_time IS NULL";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@email", GetUserEmail());
                    command.Parameters.AddWithValue("@end", DateTime.Now);
                    command.Parameters.AddWithValue("@lat", loc.Lat ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@lng", loc.Lng ?? (object)DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }
            }
            return Ok();
        }

        [HttpPost("site/enter")]
        public async Task<IActionResult> EnterSite([FromBody] SiteEnterRequest req)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Get current WorkDay ID
                var wdIdQuery = "SELECT TOP 1 id FROM WorkDays WHERE user_email = @email AND end_time IS NULL ORDER BY start_time DESC";
                int workDayId;
                using (var cmd = new SqlCommand(wdIdQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@email", GetUserEmail());
                    var result = await cmd.ExecuteScalarAsync();
                    if (result == null) return BadRequest("No active work day");
                    workDayId = (int)result;
                }

                var query = @"
                    INSERT INTO SiteVisits (work_day_id, site_name, check_in_time, check_in_lat, check_in_lng, status)
                    VALUES (@wdId, @name, @time, @lat, @lng, 'ACTIVE')";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@wdId", workDayId);
                    command.Parameters.AddWithValue("@name", req.SiteName);
                    command.Parameters.AddWithValue("@time", DateTime.Now);
                    command.Parameters.AddWithValue("@lat", req.Lat ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@lng", req.Lng ?? (object)DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }
            }
            return Ok();
        }

        [HttpPost("site/exit")]
        public async Task<IActionResult> ExitSite([FromBody] LocationRequest loc)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // Update the most recent open site visit for this user's current workday
                // Complex join: SiteVisit -> WorkDay -> User
                var query = @"
                    UPDATE sv
                    SET sv.check_out_time = @time, sv.check_out_lat = @lat, sv.check_out_lng = @lng, sv.status = 'COMPLETED'
                    FROM SiteVisits sv
                    JOIN WorkDays wd ON sv.work_day_id = wd.id
                    WHERE wd.user_email = @email 
                    AND wd.end_time IS NULL
                    AND sv.check_out_time IS NULL";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@email", GetUserEmail());
                    command.Parameters.AddWithValue("@time", DateTime.Now);
                    command.Parameters.AddWithValue("@lat", loc.Lat ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@lng", loc.Lng ?? (object)DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }
            }
            return Ok();
        }
    }

    public class LocationRequest
    {
        public double? Lat { get; set; }
        public double? Lng { get; set; }
    }

    public class SiteEnterRequest : LocationRequest
    {
        public string SiteName { get; set; }
    }
}
