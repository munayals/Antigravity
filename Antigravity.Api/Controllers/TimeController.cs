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

                // 1. Get the latest WorkDay for this user (Active OR Finished)
                var workDayQuery = @"
                    SELECT TOP 1 id, start_time, end_time, start_lat, start_lng, end_lat, end_lng 
                    FROM WorkDays 
                    WHERE user_email = @email 
                    ORDER BY start_time DESC";
                
                int? workDayId = null;
                DateTime? startTime = null;
                DateTime? endTime = null;
                double? startLat = null, startLng = null;
                double? endLat = null, endLng = null;

                using (var command = new SqlCommand(workDayQuery, connection))
                {
                    command.Parameters.AddWithValue("@email", GetUserEmail());
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            workDayId = reader.GetInt32(0);
                            startTime = reader.GetDateTime(1);
                            if (!reader.IsDBNull(2)) endTime = reader.GetDateTime(2);
                            if (!reader.IsDBNull(3)) startLat = (double)reader.GetDecimal(3);
                            if (!reader.IsDBNull(4)) startLng = (double)reader.GetDecimal(4);
                            if (!reader.IsDBNull(5)) endLat = (double)reader.GetDecimal(5);
                            if (!reader.IsDBNull(6)) endLng = (double)reader.GetDecimal(6);
                        }
                    }
                }

                // If no record, or the latest record is from a previous day and is completed...
                // Then "Jornada no iniciada".
                // logic: If (active) OR (isToday) -> Show details.
                // Else -> Show empty.

                bool isActive = (workDayId != null && endTime == null);
                bool isToday = (workDayId != null && startTime.Value.Date == DateTime.Today);

                if (!isActive && !isToday)
                {
                     return Ok(new { isWorking = false });
                }

                // Resolve Addresses
                string startAddress = null;
                string endAddress = null;
                if (startLat != null && startLng != null)
                {
                    startAddress = await Antigravity.Api.Utils.GeocodingUtils.GetAddressAsync(startLat.Value, startLng.Value);
                }
                if (endLat != null && endLng != null)
                {
                    endAddress = await Antigravity.Api.Utils.GeocodingUtils.GetAddressAsync(endLat.Value, endLng.Value);
                }

                // ... The rest of the logic (Sites, Breaks, Stats) depends on workDayId
                
                // 3. Status checks (Sites/Breaks) - only relevant if ACTIVE (or strictly for stats)
                // Actually, stats (daily site count) are relevant even if finished today.
                // Active Site/Break checks are only relevant if isWorking/isActive.

                object activeSite = null;
                object activeBreak = null;
                
                // Only check for active site/break if the day is physically active
                if (isActive) 
                {
                    var siteQuery = @"
                        SELECT TOP 1 id, site_name, check_in_time, check_in_lat, check_in_lng 
                        FROM SiteVisits 
                        WHERE work_day_id = @workDayId AND check_out_time IS NULL 
                        ORDER BY check_in_time DESC";

                    using (var command = new SqlCommand(siteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@workDayId", workDayId.Value);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                double? sLat = reader.IsDBNull(3) ? null : (double)reader.GetDecimal(3);
                                double? sLng = reader.IsDBNull(4) ? null : (double)reader.GetDecimal(4);
                                string sAddr = null;
                                if (sLat.HasValue && sLng.HasValue) 
                                    sAddr = await Antigravity.Api.Utils.GeocodingUtils.GetAddressAsync(sLat, sLng);

                                activeSite = new
                                {
                                    name = reader.GetString(1),
                                    checkInTime = reader.GetDateTime(2),
                                    checkInAddress = sAddr
                                };
                            }
                        }
                    }

                    var breakQuery = @"
                        SELECT TOP 1 id, start_time, start_lat, start_lng
                        FROM Breaks 
                        WHERE work_day_id = @workDayId AND end_time IS NULL 
                        ORDER BY start_time DESC";

                    using (var command = new SqlCommand(breakQuery, connection))
                    {
                        command.Parameters.AddWithValue("@workDayId", workDayId.Value);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                double? sLat = reader.IsDBNull(2) ? null : (double)reader.GetDecimal(2);
                                double? sLng = reader.IsDBNull(3) ? null : (double)reader.GetDecimal(3);
                                string sAddr = null;
                                if (sLat.HasValue && sLng.HasValue) 
                                    sAddr = await Antigravity.Api.Utils.GeocodingUtils.GetAddressAsync(sLat, sLng);

                                activeBreak = new
                                {
                                    startTime = reader.GetDateTime(1),
                                    startAddress = sAddr
                                };
                            }
                        }
                    }
                }

                // Stats (Breaks list + Site Count) - Valid for ALL records TODAY
                var breaks = new List<object>();
                var breaksQuery = @"
                    SELECT b.id, b.start_time, b.end_time, b.status, 
                           b.start_lat, b.start_lng, b.end_lat, b.end_lng
                    FROM Breaks b
                    JOIN WorkDays wd ON b.work_day_id = wd.id
                    WHERE wd.user_email = @email AND CAST(wd.start_time AS DATE) = CAST(GETDATE() AS DATE)
                    ORDER BY b.start_time DESC";

                using (var command = new SqlCommand(breaksQuery, connection))
                {
                    command.Parameters.AddWithValue("@email", GetUserEmail());
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var bStart = reader.GetDateTime(1);
                            DateTime? bEnd = reader.IsDBNull(2) ? null : reader.GetDateTime(2);
                            
                            double? sLat = reader.IsDBNull(4) ? null : (double)reader.GetDecimal(4);
                            double? sLng = reader.IsDBNull(5) ? null : (double)reader.GetDecimal(5);
                            double? eLat = reader.IsDBNull(6) ? null : (double)reader.GetDecimal(6);
                            double? eLng = reader.IsDBNull(7) ? null : (double)reader.GetDecimal(7);

                            string bStartAddress = null;
                            if (sLat.HasValue && sLng.HasValue)
                                bStartAddress = await Antigravity.Api.Utils.GeocodingUtils.GetAddressAsync(sLat, sLng);

                            string bEndAddress = null;
                            if (eLat.HasValue && eLng.HasValue)
                                bEndAddress = await Antigravity.Api.Utils.GeocodingUtils.GetAddressAsync(eLat, eLng);

                            breaks.Add(new { 
                                startTime = bStart, 
                                endTime = bEnd, 
                                startAddress = bStartAddress, 
                                endAddress = bEndAddress 
                            });
                        }
                    }
                }

                // Count sites for ALL workdays TODAY
                var siteCountQuery = @"
                    SELECT COUNT(*) 
                    FROM SiteVisits sv
                    JOIN WorkDays wd ON sv.work_day_id = wd.id
                    WHERE wd.user_email = @email AND CAST(wd.start_time AS DATE) = CAST(GETDATE() AS DATE)";
                
                int siteVisitCount = 0;
                using (var command = new SqlCommand(siteCountQuery, connection))
                {
                    command.Parameters.AddWithValue("@email", GetUserEmail());
                    siteVisitCount = (int)await command.ExecuteScalarAsync();
                }

                return Ok(new
                {
                    isWorking = isActive, // True only if NOT finished
                    startTime = startTime,
                    endTime = endTime,
                    startAddress = startAddress,
                    endAddress = endAddress,
                    activeSite = activeSite,
                    activeBreak = activeBreak,
                    dailyStats = new {
                        siteVisitCount = siteVisitCount,
                        breaks = breaks
                    }
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

        [HttpPost("break/start")]
        public async Task<IActionResult> StartBreak([FromBody] LocationRequest loc)
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
                    INSERT INTO Breaks (work_day_id, start_time, start_lat, start_lng, status)
                    VALUES (@wdId, @time, @lat, @lng, 'ACTIVE')";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@wdId", workDayId);
                    command.Parameters.AddWithValue("@time", DateTime.Now);
                    command.Parameters.AddWithValue("@lat", loc.Lat ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@lng", loc.Lng ?? (object)DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }
            }
            return Ok();
        }

        [HttpPost("break/end")]
        public async Task<IActionResult> EndBreak([FromBody] LocationRequest loc)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                var query = @"
                    UPDATE b
                    SET b.end_time = @time, b.end_lat = @lat, b.end_lng = @lng, b.status = 'COMPLETED'
                    FROM Breaks b
                    JOIN WorkDays wd ON b.work_day_id = wd.id
                    WHERE wd.user_email = @email 
                    AND wd.end_time IS NULL
                    AND b.end_time IS NULL";

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
