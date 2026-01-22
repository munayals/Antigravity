using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Antigravity.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Name { get; set; }

        [JsonIgnore] // Never send password hash to client
        public string PasswordHash { get; set; }

        [Required]
        public string Role { get; set; } = "User"; // Admin, User

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    }

    public class CreateUserRequest
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    public class UpdateUserRequest
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Password { get; set; } // Optional, only if changing
        public string Role { get; set; }
    }
}
