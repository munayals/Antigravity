using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Antigravity.Api.Models;
using Antigravity.Api.Data;
using Antigravity.Api.Utils;
using Antigravity.Api.Services;

namespace Antigravity.Api.Controllers
{
    [ApiController]
    [Route("api/time")]
    public class TimeController : ControllerBase
    {
        private readonly FontaneriaContext _context;
        private readonly IWhatsAppService _whatsAppService;

        public TimeController(FontaneriaContext context, IWhatsAppService whatsAppService)
        {
            _context = context;
            _whatsAppService = whatsAppService;
        }

        private string GetUserEmail() => "demo@example.com";

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var email = GetUserEmail();
            var now = DateTimeOffset.Now;
            var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
            var todayEnd = todayStart.AddDays(1);

            // 1. Get the latest WorkDay (Active OR Finished)
            var lastWorkDay = await _context.WorkDays
                .Where(w => w.UserEmail == email)
                .OrderByDescending(w => w.StartTime)
                .FirstOrDefaultAsync();

            bool isActive = (lastWorkDay != null && lastWorkDay.EndTime == null);
            bool isToday = (lastWorkDay != null && lastWorkDay.StartTime >= todayStart && lastWorkDay.StartTime < todayEnd);

            // If the last workday was NOT today AND is not active, treat as no workday today
            if (!isToday && !isActive)
            {
                lastWorkDay = null;
            }

            // Stats variables
            var breaksList = new List<object>();
            int breakDurationMinutes = 0;
            int siteVisitCount = 0;
            int siteVisitDurationMinutes = 0;

            // Get stats for TODAY
            // Breaks
            var breaks = await _context.Breaks
                .Include(b => b.WorkDay)
                .Where(b => b.WorkDay.UserEmail == email && b.StartTime >= todayStart && b.StartTime < todayEnd)
                .OrderByDescending(b => b.StartTime)
                .ToListAsync();

            var breakResults = breaks.Select(b =>
            {
                var endCalc = b.EndTime ?? DateTimeOffset.Now;
                return new
                {
                    Break = b,
                    Duration = (int)(endCalc - b.StartTime).TotalMinutes,
                    StartAddress = (string)null,
                    EndAddress = (string)null
                };
            }).ToList();

            foreach (var res in breakResults)
            {
                breakDurationMinutes += res.Duration;
                breaksList.Add(new
                {
                    startTime = res.Break.StartTime,
                    endTime = res.Break.EndTime,
                    startAddress = res.StartAddress,
                    endAddress = res.EndAddress
                });
            }

            // Site Visits
            var siteVisits = await _context.SiteVisits
                .Include(sv => sv.WorkDay)
                .Where(sv => sv.WorkDay.UserEmail == email && sv.CheckInTime >= todayStart && sv.CheckInTime < todayEnd)
                .ToListAsync();

            foreach (var sv in siteVisits)
            {
                siteVisitCount++;
                var endCalc = sv.CheckOutTime ?? DateTimeOffset.Now;
                siteVisitDurationMinutes += (int)(endCalc - sv.CheckInTime).TotalMinutes;
            }

            // Get all sessions for TODAY
            var todayWorkDays = await _context.WorkDays
                .Where(w => w.UserEmail == email && w.StartTime >= todayStart && w.StartTime < todayEnd)
                .OrderBy(w => w.StartTime)
                .ToListAsync();

            var sessionsList = new List<object>();
            foreach (var wd in todayWorkDays)
            {
                sessionsList.Add(new
                {
                    startTime = wd.StartTime,
                    endTime = wd.EndTime,
                    startAddress = (string)null, // Placeholder to avoid breaking frontend if it expects strings
                    endAddress = (string)null
                });
            }

            // 2. Get the latest active site visit (if any)
            var activeSite = await _context.SiteVisits
                .Include(sv => sv.WorkDay)
                .Where(sv => sv.WorkDay.UserEmail == email && sv.WorkDay.EndTime == null && sv.CheckOutTime == null)
                .OrderByDescending(sv => sv.CheckInTime)
                .FirstOrDefaultAsync();

            object activeSiteDto = null;
            if (activeSite != null)
            {
                string addr = null;
                // Removed Geocoding for dashboard
                // if (activeSite.CheckInLat.HasValue && activeSite.CheckInLng.HasValue)
                //    addr = await GeocodingUtils.GetAddressAsync((double)activeSite.CheckInLat, (double)activeSite.CheckInLng);

                activeSiteDto = new
                {
                    name = activeSite.SiteName,
                    checkInTime = activeSite.CheckInTime,
                    checkInAddress = addr
                };
            }

            // 3. Get the latest active break (if any)
            var activeBreak = await _context.Breaks
                .Include(b => b.WorkDay)
                .Where(b => b.WorkDay.UserEmail == email && b.WorkDay.EndTime == null && b.EndTime == null)
                .OrderByDescending(b => b.StartTime)
                .FirstOrDefaultAsync();

            object activeBreakDto = null;
            if (activeBreak != null)
            {
                string addr = null;
                // Removed Geocoding for dashboard
                // if (activeBreak.StartLat.HasValue && activeBreak.StartLng.HasValue)
                //    addr = await GeocodingUtils.GetAddressAsync((double)activeBreak.StartLat, (double)activeBreak.StartLng);

                activeBreakDto = new
                {
                    startTime = activeBreak.StartTime,
                    startAddress = addr
                };
            }

            // 4. Main session address (for the "Inicio" row on top if active)
            string lastStartAddress = null;
            if (lastWorkDay != null && lastWorkDay.StartLat.HasValue && lastWorkDay.StartLng.HasValue)
            {
                // lastStartAddress = await GeocodingUtils.GetAddressAsync((double)lastWorkDay.StartLat, (double)lastWorkDay.StartLng);
            }

            return Ok(new
            {
                isWorking = isActive,
                startTime = lastWorkDay?.StartTime,
                endTime = lastWorkDay?.EndTime,
                startAddress = lastStartAddress,
                activeSite = activeSiteDto,
                activeBreak = activeBreakDto,
                dailyStats = new
                {
                    siteVisitCount = siteVisitCount,
                    siteVisitDurationMinutes = siteVisitDurationMinutes,
                    breaks = breaksList,
                    breakDurationMinutes = breakDurationMinutes,
                    workSessions = sessionsList
                }
            });
        }

        [HttpPost("workday/start")]
        public async Task<IActionResult> StartWorkDay([FromBody] LocationRequest loc)
        {
            try 
            {
                var email = GetUserEmail();
                
                var workDay = new WorkDay
                {
                    UserEmail = email,
                    StartTime = DateTimeOffset.Now,
                    StartLat = loc.Lat.HasValue ? (decimal)loc.Lat.Value : null,
                    StartLng = loc.Lng.HasValue ? (decimal)loc.Lng.Value : null,
                    StartClientTime = loc.ClientTime,
                    Status = "ACTIVE"
                };

                _context.WorkDays.Add(workDay);
                await _context.SaveChangesAsync();
                
                await _whatsAppService.SendNotificationAsync($"🕒 Jornada iniciada a las {DateTime.Now:HH:mm}");

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in StartWorkDay: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"INNER ERROR: {ex.InnerException.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("workday/end")]
        public async Task<IActionResult> EndWorkDay([FromBody] LocationRequest loc)
        {
            var email = GetUserEmail();
            var workDay = await _context.WorkDays
                .Where(w => w.UserEmail == email && w.EndTime == null)
                .OrderByDescending(w => w.StartTime)
                .FirstOrDefaultAsync();

            if (workDay != null)
            {
                workDay.EndTime = DateTimeOffset.Now;
                workDay.EndLat = loc.Lat.HasValue ? (decimal)loc.Lat.Value : null;
                workDay.EndLng = loc.Lng.HasValue ? (decimal)loc.Lng.Value : null;
                workDay.EndClientTime = loc.ClientTime;
                workDay.Status = "COMPLETED";

                await _context.SaveChangesAsync();
                await _whatsAppService.SendNotificationAsync($"🏁 Jornada finalizada a las {DateTime.Now:HH:mm}. Duración total: {(workDay.EndTime - workDay.StartTime)?.ToString(@"hh\:mm")}");
            }
            return Ok();
        }

        [HttpPost("site/enter")]
        public async Task<IActionResult> EnterSite([FromBody] SiteEnterRequest req)
        {
            var email = GetUserEmail();

            // Get current WorkDay
            var workDay = await _context.WorkDays
                .Where(w => w.UserEmail == email && w.EndTime == null)
                .OrderByDescending(w => w.StartTime)
                .FirstOrDefaultAsync();

            if (workDay == null)
            {
                // Auto-start WorkDay if not exists
                workDay = new WorkDay
                {
                    UserEmail = email,
                    StartTime = DateTimeOffset.Now,
                    StartLat = req.Lat.HasValue ? (decimal)req.Lat.Value : null,
                    StartLng = req.Lng.HasValue ? (decimal)req.Lng.Value : null,
                    Status = "ACTIVE"
                };
                _context.WorkDays.Add(workDay);
                await _context.SaveChangesAsync();
                await _whatsAppService.SendNotificationAsync($"🕒 Jornada iniciada (auto) por entrada a sitio");
            }

            var siteVisit = new SiteVisit
            {
                WorkDayId = workDay.Id,
                SiteName = req.SiteName,
                ClientId = req.ClientId,
                AvisoId = req.AvisoId,
                CheckInTime = DateTimeOffset.Now,
                CheckInLat = req.Lat.HasValue ? (decimal)req.Lat.Value : null,
                CheckInLng = req.Lng.HasValue ? (decimal)req.Lng.Value : null,
                CheckInClientTime = req.ClientTime,
                Status = "ACTIVE",
                Description = "",       
                AttachmentPath = ""     
            };

            _context.SiteVisits.Add(siteVisit);

            // Update Aviso status if linked
            if (req.AvisoId.HasValue)
            {
                var aviso = await _context.Avisos.FindAsync(req.AvisoId.Value);
                if (aviso != null)
                {
                    aviso.Status = "EN PROGRESO";
                }
            }

            await _context.SaveChangesAsync();
            await _whatsAppService.SendNotificationAsync($"➡️ Entrada a SITIO: {req.SiteName} a las {DateTime.Now:HH:mm}");

            return Ok();
        }

        [HttpPost("site/exit")]
        public async Task<IActionResult> ExitSite([FromBody] LocationRequest loc)
        {
            var email = GetUserEmail();
            
            var siteVisit = await _context.SiteVisits
                .Include(sv => sv.WorkDay)
                .Where(sv => sv.WorkDay.UserEmail == email && sv.WorkDay.EndTime == null && sv.CheckOutTime == null)
                .OrderByDescending(sv => sv.CheckInTime)
                .FirstOrDefaultAsync();

            if (siteVisit != null)
            {
                siteVisit.CheckOutTime = DateTimeOffset.Now;
                siteVisit.CheckOutLat = loc.Lat.HasValue ? (decimal)loc.Lat.Value : null;
                siteVisit.CheckOutLng = loc.Lng.HasValue ? (decimal)loc.Lng.Value : null;
                siteVisit.CheckOutClientTime = loc.ClientTime;
                siteVisit.Status = "COMPLETED";

                // Mark linked Aviso as REALIZADO
                if (siteVisit.AvisoId.HasValue)
                {
                    var aviso = await _context.Avisos.FindAsync(siteVisit.AvisoId.Value);
                    if (aviso != null)
                    {
                        aviso.Status = "REALIZADO";
                    }
                }

                await _context.SaveChangesAsync();
                await _whatsAppService.SendNotificationAsync($"⬅️ Salida de SITIO: {siteVisit.SiteName} a las {DateTime.Now:HH:mm}. Duración: {(siteVisit.CheckOutTime - siteVisit.CheckInTime)?.ToString(@"hh\:mm")}");
            }

            return Ok();
        }

        [HttpPost("break/start")]
        public async Task<IActionResult> StartBreak([FromBody] LocationRequest loc)
        {
            var email = GetUserEmail();

            var workDay = await _context.WorkDays
                .Where(w => w.UserEmail == email && w.EndTime == null)
                .OrderByDescending(w => w.StartTime)
                .FirstOrDefaultAsync();

            if (workDay == null) return BadRequest("No active work day");

            var breakObj = new Break
            {
                WorkDayId = workDay.Id,
                StartTime = DateTimeOffset.Now,
                StartLat = loc.Lat.HasValue ? (decimal)loc.Lat.Value : null,
                StartLng = loc.Lng.HasValue ? (decimal)loc.Lng.Value : null,
                StartClientTime = loc.ClientTime,
                Status = "ACTIVE"
            };

            _context.Breaks.Add(breakObj);
            await _context.SaveChangesAsync();
            await _whatsAppService.SendNotificationAsync($"☕ Descanso iniciado a las {DateTime.Now:HH:mm}");

            return Ok();
        }

        [HttpPost("break/end")]
        public async Task<IActionResult> EndBreak([FromBody] LocationRequest loc)
        {
            var email = GetUserEmail();

            var breakObj = await _context.Breaks
                .Include(b => b.WorkDay)
                .Where(b => b.WorkDay.UserEmail == email && b.WorkDay.EndTime == null && b.EndTime == null)
                .OrderByDescending(b => b.StartTime)
                .FirstOrDefaultAsync();

            if (breakObj != null)
            {
                breakObj.EndTime = DateTimeOffset.Now;
                breakObj.EndLat = loc.Lat.HasValue ? (decimal)loc.Lat.Value : null;
                breakObj.EndLng = loc.Lng.HasValue ? (decimal)loc.Lng.Value : null;
                breakObj.EndClientTime = loc.ClientTime;
                breakObj.Status = "COMPLETED";

                await _context.SaveChangesAsync();
                await _whatsAppService.SendNotificationAsync($"✅ Descanso finalizado a las {DateTime.Now:HH:mm}");
            }

            return Ok();
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] DateTimeOffset? startDate, [FromQuery] DateTimeOffset? endDate)
        {
            var email = GetUserEmail();
            var start = startDate ?? DateTimeOffset.Now.Date;
            var end = endDate ?? DateTimeOffset.Now.Date;
            end = end.Date.AddDays(1).AddTicks(-1);

            var timeline = new List<DayTimelineDto>();

            var workDays = await _context.WorkDays
                .Where(w => w.UserEmail == email && w.StartTime >= start && w.StartTime <= end)
                .OrderByDescending(w => w.StartTime)
                .ToListAsync();

            // Prepare parallel geocoding tasks for all days
            var dayTasks = workDays.Select(async wd => {
                var dayDto = new DayTimelineDto
                {
                    Date = wd.StartTime.Date,
                    StartTime = wd.StartTime,
                    EndTime = wd.EndTime
                };

                // Resolve Start Address
                if (wd.StartLat.HasValue && wd.StartLng.HasValue)
                {
                    dayDto.StartAddress = await GeocodingUtils.GetAddressAsync((double)wd.StartLat, (double)wd.StartLng);
                }

                // Resolve End Address
                if (wd.EndLat.HasValue && wd.EndLng.HasValue)
                {
                    dayDto.EndAddress = await GeocodingUtils.GetAddressAsync((double)wd.EndLat, (double)wd.EndLng);
                }
                
                return new { WorkDay = wd, Dto = dayDto };
            });

            var processedDays = await Task.WhenAll(dayTasks);

            foreach (var item in processedDays)
            {
                var wd = item.WorkDay;
                var dayDto = item.Dto;

                var rawEvents = new List<TimelineEventDto>();

                // Site Visits
                var siteVisits = await _context.SiteVisits
                    .Where(sv => sv.WorkDayId == wd.Id)
                    .OrderBy(sv => sv.CheckInTime)
                    .ToListAsync();

                foreach (var sv in siteVisits)
                {
                    var sEnd = sv.CheckOutTime;
                    var duration = (int)((sEnd ?? DateTimeOffset.Now) - sv.CheckInTime).TotalMinutes;
                    

                    
                    string startAddr = null;
                    if (sv.CheckInLat.HasValue && sv.CheckInLng.HasValue)
                    {
                         startAddr = await GeocodingUtils.GetAddressAsync((double)sv.CheckInLat.Value, (double)sv.CheckInLng.Value);
                    }

                    string endAddr = null;
                    if (sv.CheckOutLat.HasValue && sv.CheckOutLng.HasValue)
                    {
                         endAddr = await GeocodingUtils.GetAddressAsync((double)sv.CheckOutLat.Value, (double)sv.CheckOutLng.Value);
                    }

                    rawEvents.Add(new TimelineEventDto
                    {
                        Id = "site-" + sv.Id,
                        Type = "SITE",
                        Title = sv.SiteName,
                        SubTitle = sv.Description, // Use description if available, or keep empty
                        StartAddress = startAddr,
                        EndAddress = endAddr,
                        Start = sv.CheckInTime,
                        End = sEnd,
                        IsActive = sEnd == null,
                        DurationMinutes = duration,
                        DurationFormatted = FormatDuration(duration)
                    });
                }

                // Breaks
                var breaks = await _context.Breaks
                    .Where(b => b.WorkDayId == wd.Id)
                    .OrderBy(b => b.StartTime)
                    .ToListAsync();

                foreach (var b in breaks)
                {
                    var bEnd = b.EndTime;
                    var duration = (int)((bEnd ?? DateTimeOffset.Now) - b.StartTime).TotalMinutes;
                    

                    
                    string bStartAddr = null;
                    if (b.StartLat.HasValue && b.StartLng.HasValue)
                    {
                        bStartAddr = await GeocodingUtils.GetAddressAsync((double)b.StartLat.Value, (double)b.StartLng.Value);
                    }

                    string bEndAddr = null;
                    if (b.EndLat.HasValue && b.EndLng.HasValue)
                    {
                        bEndAddr = await GeocodingUtils.GetAddressAsync((double)b.EndLat.Value, (double)b.EndLng.Value);
                    }

                    rawEvents.Add(new TimelineEventDto
                    {
                        Id = "break-" + b.Id,
                        Type = "BREAK",
                        Title = "Descanso",
                        SubTitle = null,
                        StartAddress = bStartAddr,
                        EndAddress = bEndAddr,
                        Start = b.StartTime,
                        End = bEnd,
                        IsActive = bEnd == null,
                        DurationMinutes = duration,
                        DurationFormatted = FormatDuration(duration)
                    });
                }

                // Sort and Fill Gaps (reuse logic)
                rawEvents = rawEvents.OrderBy(e => e.Start).ToList();
                var finalEvents = new List<TimelineEventDto>();
                var cursor = wd.StartTime;

                foreach (var evt in rawEvents)
                {
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
                    cursor = evt.End ?? DateTimeOffset.Now;
                }

                var dayEnd = wd.EndTime ?? DateTimeOffset.Now;
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
                dayDto.MinutesSite = finalEvents.Where(e => e.Type == "SITE").Sum(e => e.DurationMinutes);
                dayDto.MinutesBreak = finalEvents.Where(e => e.Type == "BREAK").Sum(e => e.DurationMinutes);
                dayDto.MinutesGap = finalEvents.Where(e => e.Type == "GAP").Sum(e => e.DurationMinutes);

                dayDto.DurationSite = FormatDuration(dayDto.MinutesSite);
                dayDto.DurationBreak = FormatDuration(dayDto.MinutesBreak);
                dayDto.DurationGap = FormatDuration(dayDto.MinutesGap);

                var totalMins = (int)((wd.EndTime ?? DateTimeOffset.Now) - wd.StartTime).TotalMinutes;
                dayDto.TotalDuration = FormatDuration(totalMins);

                timeline.Add(dayDto);
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
        public DateTimeOffset? ClientTime { get; set; }
    }

    public class SiteEnterRequest : LocationRequest
    {
        public string SiteName { get; set; }
        public int? ClientId { get; set; }
        public int? AvisoId { get; set; }
    }
}
