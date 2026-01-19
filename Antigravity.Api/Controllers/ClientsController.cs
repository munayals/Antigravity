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
                .Select(c => new 
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            return Ok(clients);
        }
    }
}
