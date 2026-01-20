using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Antigravity.Api.Models
{
    public class Break
    {
        public int Id { get; set; }

        [Column("work_day_id")]
        public int WorkDayId { get; set; }

        [ForeignKey("WorkDayId")]
        public WorkDay WorkDay { get; set; }

        [Column("start_time")]
        public DateTimeOffset StartTime { get; set; }

        [Column("end_time")]
        public DateTimeOffset? EndTime { get; set; }

        [Column("start_lat")]
        public decimal? StartLat { get; set; }

        [Column("start_lng")]
        public decimal? StartLng { get; set; }

        [Column("end_lat")]
        public decimal? EndLat { get; set; }

        [Column("end_lng")]
        public decimal? EndLng { get; set; }

        public string Status { get; set; }
    }
}
