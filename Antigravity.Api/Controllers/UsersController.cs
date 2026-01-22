using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Antigravity.Api.Data;
using Antigravity.Api.Models;
using Antigravity.Api.Utils;

namespace Antigravity.Api.Controllers
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly FontaneriaContext _context;

        public UsersController(FontaneriaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .OrderBy(u => u.Name)
                .Select(u => new 
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Role,
                    u.CreatedAt
                }) // Project to anonymous type to exclude PasswordHash
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return Ok(new 
            {
                user.Id,
                user.Name,
                user.Email,
                user.Role
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Email already in use.");
            }

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                Role = request.Role,
                PasswordHash = PasswordHasher.HashPassword(request.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new 
            { 
                user.Id, 
                user.Name, 
                user.Email, 
                user.Role 
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Check email uniqueness if email changed
            if (user.Email != request.Email && await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Email already in use.");
            }

            user.Name = request.Name;
            user.Email = request.Email;
            user.Role = request.Role;

            // Update password only if provided and not empty
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = PasswordHasher.HashPassword(request.Password);
            }

            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                user.Id, 
                user.Name, 
                user.Email, 
                user.Role 
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            
            if (user == null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid email or password.");
            }

            return Ok(new 
            { 
                user.Id, 
                user.Name, 
                user.Email, 
                user.Role 
            });
        }
    }
}
