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

        [System.Text.Json.Serialization.JsonPropertyName("clientId")]
        public int? ClientId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("startTime")]
        public DateTimeOffset? StartTime { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("endTime")]
        public DateTimeOffset? EndTime { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("hours")]
        public double Hours { get; set; }
        
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
        public double? Hours { get; set; } // Added
        public string? AttachmentPath { get; set; }
    }

    public class DayTimelineDto
    {
        public DateTimeOffset Date { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public string TotalDuration { get; set; }
        
        // Stats
        public int MinutesSite { get; set; }
        public int MinutesBreak { get; set; }
        public int MinutesGap { get; set; }
        
        // Formatted Stats
        public string DurationSite { get; set; }
        public string DurationBreak { get; set; }
        public string DurationGap { get; set; }
        
        public string? StartAddress { get; set; }
        public string? EndAddress { get; set; }

        public List<TimelineEventDto> Events { get; set; } = new List<TimelineEventDto>();
    }

    public class TimelineEventDto
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string StartAddress { get; set; } 
        public string EndAddress { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public int DurationMinutes { get; set; }
        public string DurationFormatted { get; set; }
        public bool IsActive { get; set; }
    }


}
