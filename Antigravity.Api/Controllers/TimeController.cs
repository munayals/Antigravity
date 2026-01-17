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

                // Stats variables initialization
                var breaks = new List<object>();
                int breakDurationMinutes = 0;
                int siteVisitCount = 0;
                int siteVisitDurationMinutes = 0;

                // Only calculate Calculate stats if there is a workday today (active or finished)
                // Actually, if we want "Daily Stats", we should query regardless of 'isActive' logic, 
                // but strictly speaking, we need a reference to 'today'.
                // The queries below filter by CAST(GETDATE() AS DATE), so they are safe to run even if 'isActive' is false,
                // AS LONG AS there could be data. If 'workDayId' is null, it means no last workday? 
                // No, queries use 'wd.start_time', so they are independent of the specific 'workDayId' variable derived above.
                // So we can ALWAYS run the stats queries for "Today".

                // Stats (Breaks list + count + total duration)
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
                            
                            // Calculate duration for this break
                            var endCalc = bEnd ?? DateTime.Now;
                            breakDurationMinutes += (int)(endCalc - bStart).TotalMinutes;

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

                // Count sites and duration for ALL workdays TODAY
                var siteStatsQuery = @"
                    SELECT 
                        sv.check_in_time, sv.check_out_time
                    FROM SiteVisits sv
                    JOIN WorkDays wd ON sv.work_day_id = wd.id
                    WHERE wd.user_email = @email AND CAST(wd.start_time AS DATE) = CAST(GETDATE() AS DATE)";
                
                using (var command = new SqlCommand(siteStatsQuery, connection))
                {
                    command.Parameters.AddWithValue("@email", GetUserEmail());
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            siteVisitCount++;
                            var checkIn = reader.GetDateTime(0);
                            DateTime? checkOut = reader.IsDBNull(1) ? null : reader.GetDateTime(1);
                            
                            var endCalc = checkOut ?? DateTime.Now;
                            siteVisitDurationMinutes += (int)(endCalc - checkIn).TotalMinutes;
                        }
                    }
                }

                return Ok(new
                {
                    isWorking = isActive, // True only if NOT finished
                    startTime = startTime,
                    endTime = endTime,
                    //startAddress = startAddress,
                    //endAddress = endAddress,
                    //activeSite = activeSite,
                    //activeBreak = activeBreak,
                    dailyStats = new {
                        siteVisitCount = siteVisitCount,
                        siteVisitDurationMinutes = siteVisitDurationMinutes,
                        breaks = breaks,
                        breakDurationMinutes = breakDurationMinutes
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
                    
                    if (result == null) 
                    {
                        // Auto-Start WorkDay
                        var insertDay = @"
                            INSERT INTO WorkDays (user_email, start_time, start_lat, start_lng, status)
                            OUTPUT INSERTED.ID
                            VALUES (@email, @start, @lat, @lng, 'ACTIVE')";

                        using (var insertCmd = new SqlCommand(insertDay, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@email", GetUserEmail());
                            insertCmd.Parameters.AddWithValue("@start", DateTime.Now);
                            insertCmd.Parameters.AddWithValue("@lat", req.Lat ?? (object)DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@lng", req.Lng ?? (object)DBNull.Value);
                            
                            workDayId = (int)await insertCmd.ExecuteScalarAsync();
                        }
                    }
                    else
                    {
                        workDayId = (int)result;
                    }
                }

                var query = @"
                    INSERT INTO SiteVisits (work_day_id, site_name, client_id, aviso_id, check_in_time, check_in_lat, check_in_lng, status)
                    VALUES (@wdId, @name, @clientId, @avisoId, @time, @lat, @lng, 'ACTIVE')";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@wdId", workDayId);
                    command.Parameters.AddWithValue("@name", req.SiteName);
                    command.Parameters.AddWithValue("@clientId", req.ClientId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@avisoId", req.AvisoId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@time", DateTime.Now);
                    command.Parameters.AddWithValue("@lat", req.Lat ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@lng", req.Lng ?? (object)DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }

                // If starting from an Aviso, update its status
                if (req.AvisoId.HasValue)
                {
                    var updateAviso = "UPDATE Avisos SET status = 'EN PROGRESO' WHERE id = @avisoId";
                    using (var command = new SqlCommand(updateAviso, connection))
                    {
                        command.Parameters.AddWithValue("@avisoId", req.AvisoId.Value);
                        await command.ExecuteNonQueryAsync();
                    }
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
                
                // 1. Mark linked Aviso as REALIZADO (if any)
                var updateAviso = @"
                    UPDATE a
                    SET a.status = 'REALIZADO'
                    FROM Avisos a
                    JOIN SiteVisits sv ON sv.aviso_id = a.id
                    JOIN WorkDays wd ON sv.work_day_id = wd.id
                    WHERE wd.user_email = @email 
                    AND wd.end_time IS NULL
                    AND sv.check_out_time IS NULL";

                using (var command = new SqlCommand(updateAviso, connection))
                {
                    command.Parameters.AddWithValue("@email", GetUserEmail());
                    await command.ExecuteNonQueryAsync();
                }

                // 2. Update the SiteVisit to COMPLETED
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
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? DateTime.Today;
            
            // Adjust end to include the full day
            end = end.Date.AddDays(1).AddTicks(-1);

            var timeline = new List<DayTimelineDto>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // 1. Get WorkDays in range
                var workDaysQuery = @"
                    SELECT id, start_time, end_time 
                    FROM WorkDays 
                    WHERE user_email = @email 
                    AND start_time >= @start AND start_time <= @end
                    ORDER BY start_time DESC";

                var workDays = new List<dynamic>();

                using (var command = new SqlCommand(workDaysQuery, connection))
                {
                    command.Parameters.AddWithValue("@email", GetUserEmail());
                    command.Parameters.AddWithValue("@start", start);
                    command.Parameters.AddWithValue("@end", end);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            workDays.Add(new {
                                Id = reader.GetInt32(0),
                                Start = reader.GetDateTime(1),
                                End = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2)
                            });
                        }
                    }
                }

                foreach (var wd in workDays)
                {
                    var dayDto = new DayTimelineDto
                    {
                        Date = wd.Start.Date,
                        StartTime = wd.Start,
                        EndTime = wd.End
                    };

                    var rawEvents = new List<TimelineEventDto>();

                    // 2. Get SiteVisits for this WorkDay
                    var sitesQuery = @"
                        SELECT id, site_name, check_in_time, check_out_time
                        FROM SiteVisits
                        WHERE work_day_id = @wdId
                        ORDER BY check_in_time";
                    
                    using (var cmd = new SqlCommand(sitesQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@wdId", wd.Id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var sStart = reader.GetDateTime(2);
                                var sEnd = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);
                                var now = DateTime.Now;
                                var duration = (int)((sEnd ?? now) - sStart).TotalMinutes;

                                rawEvents.Add(new TimelineEventDto
                                {
                                    Id = "site-" + reader.GetInt32(0),
                                    Type = "SITE",
                                    Title = reader.GetString(1),
                                    Start = sStart,
                                    End = sEnd,
                                    IsActive = sEnd == null,
                                    DurationMinutes = duration,
                                    DurationFormatted = FormatDuration(duration)
                                });
                            }
                        }
                    }

                    // 3. Get Breaks for this WorkDay
                    var breaksQuery = @"
                        SELECT id, start_time, end_time
                        FROM Breaks
                        WHERE work_day_id = @wdId
                        ORDER BY start_time";
                    
                    using (var cmd = new SqlCommand(breaksQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@wdId", wd.Id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var bStart = reader.GetDateTime(1);
                                var bEnd = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2);
                                var now = DateTime.Now;
                                var duration = (int)((bEnd ?? now) - bStart).TotalMinutes;

                                rawEvents.Add(new TimelineEventDto
                                {
                                    Id = "break-" + reader.GetInt32(0),
                                    Type = "BREAK",
                                    Title = "Descanso",
                                    Start = bStart,
                                    End = bEnd,
                                    IsActive = bEnd == null,
                                    DurationMinutes = duration,
                                    DurationFormatted = FormatDuration(duration)
                                });
                            }
                        }
                    }

                    // 4. Sort and Fill Gaps
                    rawEvents = rawEvents.OrderBy(e => e.Start).ToList();
                    var finalEvents = new List<TimelineEventDto>();
                    
                    var cursor = wd.Start; // Start tracking from WorkDay Start
                    // Loop through sorted events
                    foreach (var evt in rawEvents)
                    {
                        // If there is a gap > 1 minute between cursor and event start, add GAP
                        if ((evt.Start - cursor).TotalMinutes > 1)
                        {
                            var gapMins = (int)(evt.Start - cursor).TotalMinutes;
                            finalEvents.Add(new TimelineEventDto
                            {
                                Id = "gap-" + Guid.NewGuid(),
                                Type = "GAP",
                                Title = "Tiempo sin asignar",
                                Start = cursor,
                                End = evt.Start,
                                DurationMinutes = gapMins,
                                DurationFormatted = FormatDuration(gapMins)
                            });
                        }

                        finalEvents.Add(evt);
                        // Move cursor to end of event (or Now if active)
                        cursor = evt.End ?? DateTime.Now;
                    }

                    // Check for gap at the end (until WorkDay End or Now)
                    var dayEnd = wd.End ?? DateTime.Now;
                    if ((dayEnd - cursor).TotalMinutes > 1)
                    {
                        var gapMins = (int)(dayEnd - cursor).TotalMinutes;
                        finalEvents.Add(new TimelineEventDto
                        {
                            Id = "gap-end-" + Guid.NewGuid(),
                            Type = "GAP",
                            Title = "Tiempo sin asignar",
                            Start = cursor,
                            End = dayEnd,
                            DurationMinutes = gapMins,
                            DurationFormatted = FormatDuration(gapMins)
                        });
                    }

                    dayDto.Events = finalEvents;
                    
                    // Calculate Summaries
                    dayDto.MinutesSite = finalEvents.Where(e => e.Type == "SITE").Sum(e => e.DurationMinutes);
                    dayDto.MinutesBreak = finalEvents.Where(e => e.Type == "BREAK").Sum(e => e.DurationMinutes);
                    dayDto.MinutesGap = finalEvents.Where(e => e.Type == "GAP").Sum(e => e.DurationMinutes);
                    
                    dayDto.DurationSite = FormatDuration(dayDto.MinutesSite);
                    dayDto.DurationBreak = FormatDuration(dayDto.MinutesBreak);
                    dayDto.DurationGap = FormatDuration(dayDto.MinutesGap);
                    
                    // Calculate Total Duration of WorkDay
                    var totalMins = (int)(dayEnd - wd.Start).TotalMinutes;
                    dayDto.TotalDuration = FormatDuration(totalMins);

                    timeline.Add(dayDto);
                }
            }

            return Ok(timeline);
        }

        private string FormatDuration(int minutes)
        {
            int h = minutes / 60;
            int m = minutes % 60;
            return $"{h}h {m}m";
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
        public int? ClientId { get; set; }
        public int? AvisoId { get; set; }
    }
}
