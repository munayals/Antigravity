using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Antigravity.Api.Utils
{
    public static class GeocodingUtils
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static GeocodingUtils()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AntigravityWorkReports/1.0");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "es-ES,es;q=0.9");
        }

        public static async Task<string> GetAddressAsync(double? lat, double? lng)
        {
            if (!lat.HasValue || !lng.HasValue) return null;

            try
            {
                var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat.Value}&lon={lng.Value}&zoom=18&addressdetails=1";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JObject.Parse(content);
                    var addr = data["address"];
                    if (addr != null)
                    {
                        // Priority: road -> pedestrian -> suburb -> neighbourhood -> street -> square -> ...
                        var street = addr["road"]?.ToString() 
                                  ?? addr["pedestrian"]?.ToString() 
                                  ?? addr["suburb"]?.ToString()
                                  ?? addr["neighbourhood"]?.ToString()
                                  ?? addr["street"]?.ToString()
                                  ?? addr["square"]?.ToString()
                                  ?? addr["tourism"]?.ToString()
                                  ?? addr["historic"]?.ToString()
                                  ?? addr["amenity"]?.ToString()
                                  ?? "";

                        var houseNumber = addr["house_number"]?.ToString() ?? "";
                        var postcode = addr["postcode"]?.ToString() ?? "";
                        
                        // Priority: city -> town -> village -> municipality -> city_district -> county -> state_district
                        var city = addr["city"]?.ToString() 
                                ?? addr["town"]?.ToString() 
                                ?? addr["village"]?.ToString() 
                                ?? addr["municipality"]?.ToString() 
                                ?? addr["city_district"]?.ToString() 
                                ?? addr["county"]?.ToString()
                                ?? addr["state_district"]?.ToString()
                                ?? "";
                        
                        // Construct address parts
                        var parts = new System.Collections.Generic.List<string>();
                        
                        var streetPart = $"{street} {houseNumber}".Trim();
                        if (!string.IsNullOrEmpty(streetPart)) parts.Add(streetPart);
                        
                        var locPart = $"{postcode} {city}".Trim();
                        if (!string.IsNullOrEmpty(locPart)) parts.Add(locPart);

                        // Fallback if empty (e.g. only display name in worst case, or just coord if nothing found)
                        if (parts.Count == 0 && data["display_name"] != null)
                        {
                             return data["display_name"].ToString().Split(',')[0];
                        }

                        return string.Join(", ", parts);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reverse geocoding error: {ex.Message}");
            }
            return null;
        }
    }
}
