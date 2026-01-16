using System;

namespace Antigravity.Api.Models
{
    public class SiteVisit
    {
        public int Id { get; set; }
        public int WorkDayId { get; set; }
        public string SiteName { get; set; }
        public int? ClientId { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public decimal? CheckInLat { get; set; }
        public decimal? CheckInLng { get; set; }
        public decimal? CheckOutLat { get; set; }
        public decimal? CheckOutLng { get; set; }
        public string Description { get; set; }
        public string AttachmentPath { get; set; }
        public string Status { get; set; } // 'ACTIVE', 'COMPLETED'

        // Extra property from JOIN (Not strictly in SiteVisits table)
        public string ClientName { get; set; }
    }
}
