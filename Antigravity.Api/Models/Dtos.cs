using System;

namespace Antigravity.Api.Models
{
    public class LocationDto
    {
        public double? Lat { get; set; }
        public double? Lng { get; set; }
    }

    public class WorkReportDto
    {
        public int Id { get; set; }
        public string SiteName { get; set; }
        public string ClientName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Description { get; set; }
        public LocationDto CheckInLoc { get; set; }
        public LocationDto CheckOutLoc { get; set; }
        public bool LocationsMatch { get; set; }
        public string CheckInAddress { get; set; }
        public string CheckOutAddress { get; set; }
        public string CheckInMapUrl { get; set; }
        public string CheckOutMapUrl { get; set; }
        public string AttachmentPath { get; set; }
    }

    public class ClientDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class UpdateWorkReportRequest
    {
        public string SiteName { get; set; }
        public int? ClientId { get; set; }
        public string Description { get; set; }
        public string AttachmentPath { get; set; }
    }
}
