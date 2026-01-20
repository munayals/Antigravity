using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Antigravity.Api.Models
{
    public class WorkDay
    {
        public int Id { get; set; }

        [Column("user_email")]
        public string UserEmail { get; set; }

        [Column("start_time")]
        public DateTimeOffset StartTime { get; set; }

        [Column("end_time")]
        public DateTimeOffset? EndTime { get; set; }

        [Column("start_client_time")]
        public DateTimeOffset? StartClientTime { get; set; }

        [Column("end_client_time")]
        public DateTimeOffset? EndClientTime { get; set; }

        [Column("start_lat")]
        public decimal? StartLat { get; set; }

        [Column("start_lng")]
        public decimal? StartLng { get; set; }

        [Column("end_lat")]
        public decimal? EndLat { get; set; }

        [Column("end_lng")]
        public decimal? EndLng { get; set; }

        public string Status { get; set; }

        // Navigation properties (optional, for EF Core)
        public ICollection<SiteVisit> SiteVisits { get; set; }
        public ICollection<Break> Breaks { get; set; }
    }
}
