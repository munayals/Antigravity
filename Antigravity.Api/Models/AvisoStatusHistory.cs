using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Antigravity.Api.Models
{
    public class AvisoStatusHistory
    {
        public int Id { get; set; }
        
        [Column("aviso_id")]
        public int AvisoId { get; set; }
        
        public string Status { get; set; } = string.Empty;
        
        [Column("changed_at")]
        public DateTimeOffset ChangedAt { get; set; }
        
        [Column("changed_by")]
        public string? ChangedBy { get; set; }
        
        public string? Notes { get; set; }
        
        public Aviso? Aviso { get; set; }
    }
}
