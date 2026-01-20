using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Antigravity.Api.Models;
using Antigravity.Api.Data;
using Antigravity.Api.Utils;
using System.Linq;

namespace Antigravity.Api.Controllers
{
    [ApiController]
    [Route("api/reports/work-reports")]
    public class WorkReportsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly FontaneriaContext _context;
        private readonly Repositories.IFontaneriaRepository _repository;
        private readonly Services.ISimpleMapper _mapper;

        public WorkReportsController(IConfiguration configuration,
                                     FontaneriaContext context,
                                     Repositories.IFontaneriaRepository repository,
                                     Services.ISimpleMapper mapper)
        {
            _configuration = configuration;
            _context = context;
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetReports([FromQuery] DateTimeOffset startDate, [FromQuery] DateTimeOffset endDate)
        {
            string userEmail = "demo@example.com"; 

            // Adjust end date to include the full day
            var endOfDay = endDate.Date.AddDays(1).AddTicks(-1);

            var siteVisits = await _context.SiteVisits
                .Include(sv => sv.WorkDay)
                .Where(sv => sv.WorkDay.UserEmail == userEmail 
                          && sv.CheckInTime >= startDate 
                          && sv.CheckInTime <= endOfDay)
                .OrderByDescending(sv => sv.CheckInTime)
                .ToListAsync();

            var reports = new List<WorkReportDto>();
            
            foreach (var sv in siteVisits)
            {
                 var report = new WorkReportDto
                 {
                     Id = sv.Id,
                     SiteName = sv.SiteName ?? "",
                     ClientId = sv.ClientId,
                     StartTime = sv.CheckInTime,
                     EndTime = sv.CheckOutTime,
                     CheckInLoc = new LocationDto { 
                        Lat = sv.CheckInLat.HasValue ? (double)sv.CheckInLat.Value : null,
                        Lng = sv.CheckInLng.HasValue ? (double)sv.CheckInLng.Value : null
                     },
                     CheckOutLoc = new LocationDto { 
                        Lat = sv.CheckOutLat.HasValue ? (double)sv.CheckOutLat.Value : null,
                        Lng = sv.CheckOutLng.HasValue ? (double)sv.CheckOutLng.Value : null
                     },
                     Description = sv.Description ?? "",
                     AttachmentPath = sv.AttachmentPath,
                     ClientName = "" // Will populate below
                 };

                 // Resolve Client Name
                 if (sv.ClientId.HasValue)
                 {
                     var client = await _context.Clients.FindAsync(sv.ClientId.Value);
                     if (client != null) report.ClientName = client.Name;
                 }

                 report.LocationsMatch = report.CheckInLoc.Lat == report.CheckOutLoc.Lat && 
                                       report.CheckInLoc.Lng == report.CheckOutLoc.Lng;
                 
                 report.CheckInMapUrl = report.CheckInLoc.Lat.HasValue ? 
                     $"https://www.google.com/maps?q={report.CheckInLoc.Lat},{report.CheckInLoc.Lng}" : null;
                 report.CheckOutMapUrl = report.CheckOutLoc.Lat.HasValue ? 
                     $"https://www.google.com/maps?q={report.CheckOutLoc.Lat},{report.CheckOutLoc.Lng}" : null;

                 reports.Add(report);
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
            var siteVisit = await _context.SiteVisits.FindAsync(id);
            if (siteVisit == null) return NotFound();

            siteVisit.SiteName = request.SiteName ?? "";
            siteVisit.ClientId = request.ClientId;
            siteVisit.Description = request.Description ?? "";
            siteVisit.AttachmentPath = request.AttachmentPath ?? "";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Report updated successfully" });
        }
    }
}
