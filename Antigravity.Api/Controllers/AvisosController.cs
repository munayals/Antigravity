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

        private string GetUserEmail() => "demo@example.com";

        [HttpGet]
        public async Task<IActionResult> GetAvisos([FromQuery] string? status, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var query = _context.Avisos
                .Include(a => a.Client)
                .AsQueryable();

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
            if (aviso.Id != 0) return BadRequest("No puede modificar un aviso desde este método");
            
            if (aviso.CommitmentTime?.Kind == DateTimeKind.Utc) 
                aviso.CommitmentTime = aviso.CommitmentTime?.ToLocalTime();

            aviso.RequestTime = DateTime.Now;
            aviso.UserEmail = GetUserEmail();

            _repository.Add(aviso);
            await _repository.SaveChangesAsync();

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
            return Ok();
        }
    }
}
