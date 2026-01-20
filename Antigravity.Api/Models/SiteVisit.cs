using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Antigravity.Api.Models
{
    public class SiteVisit
    {
        public int Id { get; set; }

        [Column("work_day_id")]
        public int WorkDayId { get; set; }

        [ForeignKey("WorkDayId")]
        public WorkDay WorkDay { get; set; }

        [Column("site_name")]
        public string? SiteName { get; set; }

        [Column("client_id")]
        public int? ClientId { get; set; }

        [Column("aviso_id")]
        public int? AvisoId { get; set; }

        [Column("check_in_time")]
        public DateTimeOffset CheckInTime { get; set; }

        [Column("check_out_time")]
        public DateTimeOffset? CheckOutTime { get; set; }

        [Column("check_in_lat")]
        public decimal? CheckInLat { get; set; }

        [Column("check_in_lng")]
        public decimal? CheckInLng { get; set; }

        [Column("check_out_lat")]
        public decimal? CheckOutLat { get; set; }

        [Column("check_out_lng")]
        public decimal? CheckOutLng { get; set; }

        public string Description { get; set; } = "";
        
        [Column("attachment_path")]
        public string AttachmentPath { get; set; } = "";

        public string Status { get; set; } = "ACTIVE"; // 'ACTIVE', 'COMPLETED'

        // Extra property from JOIN (Not mapped to DB table directly, handle via DTO projection ideally)
        [NotMapped]
        public string ClientName { get; set; }
    }
}
