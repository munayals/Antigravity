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
        public async Task<IActionResult> GetReports([FromQuery] DateTimeOffset startDate, [FromQuery] DateTimeOffset endDate, [FromQuery] string? userEmail = null)
        {
            // If userEmail is provided (likely by Admin), use it. 
            // Otherwise default to the demo user or authenticated user
            userEmail = userEmail ?? "admin@antigravity.com";

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
                    // Use ReportedHours if available, otherwise calculate
                    Hours = sv.ReportedHours.HasValue ? sv.ReportedHours.Value : 
                           (sv.CheckOutTime.HasValue ? (sv.CheckOutTime.Value - sv.CheckInTime).TotalHours : 0),
                    CheckInLoc = new LocationDto
                    {
                        Lat = sv.CheckInLat.HasValue ? (double)sv.CheckInLat.Value : null,
                        Lng = sv.CheckInLng.HasValue ? (double)sv.CheckInLng.Value : null
                    },
                    CheckOutLoc = new LocationDto
                    {
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

            if (request.Hours.HasValue)
            {
                // User requested NOT to modify timestamps, but store value separately
                siteVisit.ReportedHours = request.Hours.Value;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Report updated successfully" });
        }
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] CreateWorkReportRequest request)
        {
            var userEmail = GetUserEmail();
            if (string.IsNullOrEmpty(request.Title) || request.ClientId <= 0 || string.IsNullOrEmpty(request.Date))
            {
                return BadRequest(new { message = "Faltan datos obligatorios (Título, Cliente, Fecha)" });
            }

            if (!DateTimeOffset.TryParse(request.Date, out var date))
            {
                return BadRequest(new { message = "Formato de fecha inválido" });
            }

            // 1. Find or Create WorkDay for that date
            // We need a WorkDay to attach the SiteVisit to.
            var workDay = await _context.WorkDays
                .FirstOrDefaultAsync(w => w.UserEmail == userEmail && w.StartTime.Date == date.Date);

            if (workDay == null)
            {
                workDay = new WorkDay
                {
                    UserEmail = userEmail,
                    StartTime = date.Date.AddHours(8), // Default start
                    EndTime = date.Date.AddHours(17),  // Default end
                    Status = "COMPLETED" // Assume completed if logging manually
                };
                _context.WorkDays.Add(workDay);
                await _context.SaveChangesAsync();
            }

            // 2. Create SiteVisit
            var checkIn = workDay.StartTime; // Default to WD start
                                             // If they provided hours, we calculate checkout. 
                                             // Better logic: use Date + 9am? For now, align with WorkDay start to avoid gaps 
                                             // or just use the Date with arbitrary time if not specified.

            var siteVisit = new SiteVisit
            {
                WorkDayId = workDay.Id,
                SiteName = request.Title,
                ClientId = request.ClientId,
                Description = request.Description,
                AttachmentPath = request.AttachmentPath,
                CheckInTime = checkIn,
                CheckOutTime = checkIn.AddHours(request.Hours > 0 ? request.Hours : 1),
                Status = "COMPLETED"
            };

            _context.SiteVisits.Add(siteVisit);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Reporte creado correctamente", id = siteVisit.Id });
        }

        private string GetUserEmail()
        {
            if (Request.Headers.TryGetValue("X-User-Email", out var email))
            {
                return email.ToString();
            }
            return "admin@antigravity.com";
        }
    }

    public class CreateWorkReportRequest
    {
        public string Title { get; set; }
        public int ClientId { get; set; }
        public string Date { get; set; } // YYYY-MM-DD
        public double Hours { get; set; }
        public string Description { get; set; }
        public string AttachmentPath { get; set; }
    }

}