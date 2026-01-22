using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Antigravity.Api.Models;
using Antigravity.Api.Repositories;
using Antigravity.Api.Services;
using Antigravity.Api.Data;
using System.Data;

namespace Antigravity.Api.Controllers
{
    [ApiController]
    [Route("api/avisos")]
    public class AvisosController : ControllerBase
    {
        private readonly FontaneriaContext _context;
        private readonly IFontaneriaRepository _repository;
        private readonly ISimpleMapper _mapper;

        public AvisosController(FontaneriaContext context, IFontaneriaRepository repository, ISimpleMapper mapper)
        {
            _context = context;
            _repository = repository;
            _mapper = mapper;
        }

        private string GetUserEmail()
        {
            if (Request.Headers.TryGetValue("X-User-Email", out var email))
            {
                return email.ToString();
            }
            return "admin@antigravity.com";
        }

        [HttpGet]
        public async Task<IActionResult> GetAvisos([FromQuery] string? status, [FromQuery] DateTimeOffset? startDate, [FromQuery] DateTimeOffset? endDate, [FromQuery] string? userEmail = null)
        {
            var query = _context.Avisos
                .Include(a => a.Client)
                .AsQueryable();

            // If userEmail is provided, filter by it. 
            // If NOT provided, do NOT filter (Admin view or legacy).
            // NOTE: For non-admins, the frontend MUST pass the email to see only their avisos.
            if (!string.IsNullOrEmpty(userEmail))
            {
                query = query.Where(a => a.UserEmail == userEmail);
            }

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            if (startDate.HasValue)
                query = query.Where(a => a.RequestTime >= startDate.Value);

            if (endDate.HasValue)
            {
                var endOfDate = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.RequestTime <= endOfDate);
            }

            query = query.OrderByDescending(a => a.RequestTime);

            var result = await query.Select(a => new AvisoWithClientDto
            {
                Id = a.Id,
                ClientId = a.ClientId,
                RequestTime = a.RequestTime,
                Reason = a.Reason,
                Status = a.Status,
                Priority = a.Priority ?? "NORMAL",
                EstimatedHours = a.EstimatedHours,
                CommitmentTime = a.CommitmentTime,
                UserEmail = a.UserEmail,
                ClientName = a.Client.Name,
                ClientAddress = a.Client.Address ?? "",
                ClientPhone = a.Client.Phone ?? "",
                ClientCity = a.Client.City ?? ""
            }).ToListAsync();


            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAviso(int id)
        {
            var a = await _context.Avisos
                .Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (a == null) return NotFound();

            var dto = new AvisoWithClientDto
            {
                Id = a.Id,
                ClientId = a.ClientId,
                RequestTime = a.RequestTime,
                Reason = a.Reason,
                Status = a.Status,
                Priority = a.Priority ?? "NORMAL",
                EstimatedHours = a.EstimatedHours,
                CommitmentTime = a.CommitmentTime,
                UserEmail = a.UserEmail,
                ClientName = a.Client.Name,
                ClientAddress = a.Client.Address ?? "",
                ClientPhone = a.Client.Phone ?? "",
                ClientCity = a.Client.City ?? ""
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<Aviso>> CreateAviso([FromBody] Aviso aviso)
        {
            if (aviso.Id != 0) return BadRequest(new { message = "No puede modificar un aviso desde este método" });

            // Log incoming data for debugging
            Console.WriteLine($"Creating aviso - ClientId: {aviso.ClientId}, Reason: {aviso.Reason}, Priority: {aviso.Priority}, Status: {aviso.Status}");

            // Validate model state
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                var errorDetails = string.Join(", ", errors);
                Console.WriteLine($"Model validation failed: {errorDetails}");
                return BadRequest(new { message = "Errores de validación", errors = errors, details = errorDetails });
            }

            // Validate required fields manually with specific messages
            if (aviso.ClientId <= 0)
                return BadRequest(new { message = "El campo 'Cliente' es obligatorio. Por favor selecciona un cliente." });

            if (string.IsNullOrWhiteSpace(aviso.Reason))
                return BadRequest(new { message = "El campo 'Motivo' es obligatorio. Por favor describe el motivo del aviso." });

            if (string.IsNullOrWhiteSpace(aviso.Priority))
                return BadRequest(new { message = "El campo 'Prioridad' es obligatorio. Por favor selecciona una prioridad." });

            // Set default status if not provided
            if (string.IsNullOrWhiteSpace(aviso.Status))
            {
                aviso.Status = "RECEPCIONADO";
                Console.WriteLine("Status was empty, set to RECEPCIONADO");
            }

            aviso.RequestTime = DateTimeOffset.Now;
            aviso.UserEmail = GetUserEmail();

            _repository.Add(aviso);
            await _repository.SaveChangesAsync();

            await AddStatusHistory(aviso.Id, aviso.Status ?? "RECEPCIONADO", "Aviso creado");

            return Ok(aviso);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAviso(int id, [FromBody] Aviso aviso)
        {
            var existing = await _context.Avisos.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Reason = aviso.Reason;
            existing.Status = aviso.Status;
            existing.Priority = aviso.Priority;
            existing.EstimatedHours = aviso.EstimatedHours;
            existing.CommitmentTime = aviso.CommitmentTime;

            await _context.SaveChangesAsync();

            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAviso(int id)
        {
            var aviso = await _context.Avisos.FindAsync(id);
            if (aviso == null) return NotFound();

            // Optional: Check if it can be deleted (e.g. not if it has related records like SiteVisits if FK restricts)
            // For now, simple delete
            _context.Avisos.Remove(aviso);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Aviso eliminado correctamente" });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] StatusChangeRequest request)
        {
            var aviso = await _context.Avisos.FindAsync(id);
            if (aviso == null) return NotFound();

            var oldStatus = aviso.Status;
            aviso.Status = request.NewStatus;

            await _context.SaveChangesAsync();

            // TODO: Uncomment when AvisoStatusHistory table is created
            // Add to history
            // await AddStatusHistory(id, request.NewStatus, request.Notes);

            return Ok(new { oldStatus, newStatus = request.NewStatus });
        }

        [HttpGet("{id}/status-history")]
        public async Task<IActionResult> GetStatusHistory(int id)
        {
            var history = await _context.AvisoStatusHistory
                .Where(h => h.AvisoId == id)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();

            return Ok(history);
        }

        private async Task AddStatusHistory(int avisoId, string status, string? notes = null)
        {
            var history = new AvisoStatusHistory
            {
                AvisoId = avisoId,
                Status = status,
                ChangedAt = DateTimeOffset.Now,
                ChangedBy = GetUserEmail(),
                Notes = notes
            };

            _context.AvisoStatusHistory.Add(history);
            await _context.SaveChangesAsync();
        }
    }

    public class StatusChangeRequest
    {
        public string NewStatus { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
