using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Antigravity.Api.Models
{
    public class Aviso
    {
        public int Id { get; set; }
        
        [Column("codcli")]
        public int ClientId { get; set; }
        
        [Column("request_time")]
        public DateTimeOffset RequestTime { get; set; }
        
        public string Reason { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        
        [Column("estimated_hours")]
        public decimal? EstimatedHours { get; set; }
        
        [Column("commitment_time")]
        public DateTimeOffset? CommitmentTime { get; set; }

        [Column("user_email")]
        public string? UserEmail { get; set; }

        public Client Client { get; set; }
    }

    public class AvisoWithClientDto : Aviso
    {
        public string ClientName { get; set; }
        public string ClientAddress { get; set; } // Domicilio
        public string ClientPhone { get; set; } // Telefono
        public string ClientCity { get; set; } // Pobcli
    }
}
