using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Antigravity.Api.Models;
using Antigravity.Api.Utils;
using System.Data;

namespace Antigravity.Api.Controllers
{
    [ApiController]
    [Route("api/reports/work-reports")]
    public class WorkReportsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly Repositories.IFontaneriaRepository _repository;
        private readonly Services.ISimpleMapper _mapper;

        public WorkReportsController(IConfiguration configuration, 
                                     Repositories.IFontaneriaRepository repository,
                                     Services.ISimpleMapper mapper)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetReports([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            // Note: In a production app, we would use the authenticated user's email from the JWT
            // For now, mirroring the "demo" logic or taking a hardcoded email if we aren't handling auth yet.
            string userEmail = "demo@example.com"; 

            var reports = new List<WorkReportDto>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT 
                        sv.id, sv.site_name, sv.check_in_time, sv.check_out_time,
                        sv.check_in_lat, sv.check_in_lng, sv.check_out_lat, sv.check_out_lng,
                        sv.description, sv.attachment_path,
                        c.descli as client_name
                    FROM SiteVisits sv
                    JOIN WorkDays wd ON sv.work_day_id = wd.id
                    LEFT JOIN cliente c ON sv.client_id = c.codcli
                    WHERE wd.user_email = @email
                    AND sv.check_in_time >= @start
                    AND sv.check_in_time <= @end
                    ORDER BY sv.check_in_time DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@email", userEmail);
                    command.Parameters.AddWithValue("@start", startDate);
                    command.Parameters.AddWithValue("@end", endDate.Date.AddDays(1).AddTicks(-1));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var report = new WorkReportDto
                            {
                                Id = reader.GetInt32(0),
                                SiteName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                StartTime = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                                EndTime = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                                CheckInLoc = new LocationDto { 
                                    Lat = reader.IsDBNull(4) ? null : (double)reader.GetDecimal(4), 
                                    Lng = reader.IsDBNull(5) ? null : (double)reader.GetDecimal(5) 
                                },
                                CheckOutLoc = new LocationDto { 
                                    Lat = reader.IsDBNull(6) ? null : (double)reader.GetDecimal(6), 
                                    Lng = reader.IsDBNull(7) ? null : (double)reader.GetDecimal(7) 
                                },
                                Description = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                AttachmentPath = reader.IsDBNull(9) ? null : reader.GetString(9),
                                ClientName = reader.IsDBNull(10) ? "" : reader.GetString(10)
                            };

                            report.LocationsMatch = report.CheckInLoc.Lat == report.CheckOutLoc.Lat && 
                                                   report.CheckInLoc.Lng == report.CheckOutLoc.Lng;
                            
                            report.CheckInMapUrl = report.CheckInLoc.Lat.HasValue ? 
                                $"https://www.google.com/maps?q={report.CheckInLoc.Lat},{report.CheckInLoc.Lng}" : null;
                            report.CheckOutMapUrl = report.CheckOutLoc.Lat.HasValue ? 
                                $"https://www.google.com/maps?q={report.CheckOutLoc.Lat},{report.CheckOutLoc.Lng}" : null;

                            reports.Add(report);
                        }
                    }
                }
            }

            // Resolve addresses in parallel
            var tasks = reports.Select(async r => {
                if (r.LocationsMatch) {
                    r.CheckInAddress = await GeocodingUtils.GetAddressAsync(r.CheckInLoc.Lat, r.CheckInLoc.Lng);
                    r.CheckOutAddress = r.CheckInAddress;
                } else {
                    r.CheckInAddress = await GeocodingUtils.GetAddressAsync(r.CheckInLoc.Lat, r.CheckInLoc.Lng);
                    r.CheckOutAddress = await GeocodingUtils.GetAddressAsync(r.CheckOutLoc.Lat, r.CheckOutLoc.Lng);
                }
            });
            await Task.WhenAll(tasks);

            return Ok(reports);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReport(int id)
        {
            // 1. Fetch from Repository
            var siteVisit = await _repository.GetSiteVisitByIdAsync(id);
            
            if (siteVisit == null)
            {
                return NotFound();
            }

            // 2. Map to DTO
            var report = _mapper.Map<WorkReportDto>(siteVisit);

            // 3. Resolve Addresses (Logic remains in Controller/Service layer as it involves external API)
            report.CheckInAddress = await GeocodingUtils.GetAddressAsync(report.CheckInLoc?.Lat, report.CheckInLoc?.Lng);
            report.CheckOutAddress = await GeocodingUtils.GetAddressAsync(report.CheckOutLoc?.Lat, report.CheckOutLoc?.Lng);

            return Ok(report);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReport(int id, [FromBody] UpdateWorkReportRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                    UPDATE SiteVisits 
                    SET site_name = @siteName, 
                        client_id = @clientId, 
                        description = @description,
                        attachment_path = @attachmentPath
                    WHERE id = @id";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@siteName", request.SiteName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@clientId", request.ClientId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@description", request.Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@attachmentPath", request.AttachmentPath ?? (object)DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }
            }
            return Ok(new { message = "Report updated successfully" });
        }
    }
}
