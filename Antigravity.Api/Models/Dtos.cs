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
        
        [System.Text.Json.Serialization.JsonPropertyName("siteName")]
        public string? SiteName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("clientName")]
        public string? ClientName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("startTime")]
        public DateTime? StartTime { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("endTime")]
        public DateTime? EndTime { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("checkInLoc")]
        public LocationDto? CheckInLoc { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("checkOutLoc")]
        public LocationDto? CheckOutLoc { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("locationsMatch")]
        public bool LocationsMatch { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("checkInAddress")]
        public string? CheckInAddress { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("checkOutAddress")]
        public string? CheckOutAddress { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("checkInMapUrl")]
        public string? CheckInMapUrl { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("checkOutMapUrl")]
        public string? CheckOutMapUrl { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("attachmentPath")]
        public string? AttachmentPath { get; set; }
    }

    public class ClientDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class UpdateWorkReportRequest
    {
        public string? SiteName { get; set; }
        public int? ClientId { get; set; }
        public string? Description { get; set; }
        public string? AttachmentPath { get; set; }
    }
}
