using System;
using Antigravity.Api.Models;

namespace Antigravity.Api.Services
{
    public interface ISimpleMapper
    {
        T Map<T>(object source);
    }

    public class SimpleMapper : ISimpleMapper
    {
        public T Map<T>(object source)
        {
            if (source == null) return default(T);

            if (typeof(T) == typeof(WorkReportDto) && source is SiteVisit visit)
            {
                var dto = new WorkReportDto
                {
                    Id = visit.Id,
                    SiteName = visit.SiteName ?? "",
                    ClientName = visit.ClientName ?? "",
                    ClientId = visit.ClientId,
                    StartTime = visit.CheckInTime,
                    EndTime = visit.CheckOutTime,
                    Description = visit.Description ?? "",
                    AttachmentPath = visit.AttachmentPath,
                    CheckInLoc = new LocationDto 
                    { 
                        Lat = visit.CheckInLat.HasValue ? (double)visit.CheckInLat.Value : null,
                        Lng = visit.CheckInLng.HasValue ? (double)visit.CheckInLng.Value : null
                    },
                    CheckOutLoc = new LocationDto
                    {
                        Lat = visit.CheckOutLat.HasValue ? (double)visit.CheckOutLat.Value : null,
                        Lng = visit.CheckOutLng.HasValue ? (double)visit.CheckOutLng.Value : null
                    }
                };

                // Logic properties
                dto.LocationsMatch = dto.CheckInLoc.Lat == dto.CheckOutLoc.Lat && 
                                     dto.CheckInLoc.Lng == dto.CheckOutLoc.Lng;

                dto.CheckInMapUrl = dto.CheckInLoc.Lat.HasValue ? 
                                    $"https://www.google.com/maps?q={dto.CheckInLoc.Lat},{dto.CheckInLoc.Lng}" : null;
                dto.CheckOutMapUrl = dto.CheckOutLoc.Lat.HasValue ? 
                                     $"https://www.google.com/maps?q={dto.CheckOutLoc.Lat},{dto.CheckOutLoc.Lng}" : null;

                // Return as T (cast required)
                return (T)(object)dto;
            }

            throw new NotImplementedException($"Mapping not implemented for {source.GetType().Name} to {typeof(T).Name}");
        }
    }
}
