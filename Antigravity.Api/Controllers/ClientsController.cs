using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Antigravity.Api.Models;
using Antigravity.Api.Data;

namespace Antigravity.Api.Controllers
{
    [ApiController]
    [Route("api/clients")]
    public class ClientsController : ControllerBase
    {
        private readonly FontaneriaContext _context;

        public ClientsController(FontaneriaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetClients()
        {
            var clients = await _context.Clients
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(clients);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetClientById(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return NotFound();

            return Ok(client);
        }

        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] Client client)
        {
            if (string.IsNullOrWhiteSpace(client.Name))
                return BadRequest("El nombre del cliente es obligatorio");

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return Ok(client);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClient(int id, [FromBody] Client client)
        {
            var existing = await _context.Clients.FindAsync(id);
            if (existing == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(client.Name))
                return BadRequest("El nombre del cliente es obligatorio");

            existing.Name = client.Name;
            existing.Address = client.Address;
            existing.Phone = client.Phone;
            existing.Email = client.Email;
            existing.City = client.City;

            await _context.SaveChangesAsync();

            return Ok(existing);
        }
    }
}
