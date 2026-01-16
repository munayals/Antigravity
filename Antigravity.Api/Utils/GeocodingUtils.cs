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
                        // Legacy Logic (Next.js Match)
                        var street = addr["road"]?.ToString() 
                                  ?? addr["pedestrian"]?.ToString() 
                                  ?? addr["suburb"]?.ToString()
                                  ?? "";

                        var postcode = addr["postcode"]?.ToString() ?? "";
                        
                        var city = addr["city"]?.ToString() 
                                ?? addr["town"]?.ToString() 
                                ?? addr["village"]?.ToString() 
                                ?? "";
                        
                        // Construct address: "Street, Postcode City"
                        // Logic: `${street}${street ? ", " : ""}${postcode} ${city}`
                        
                        var part1 = street;
                        var part2 = $"{postcode} {city}".Trim();

                        if (string.IsNullOrEmpty(part1)) return part2;
                        if (string.IsNullOrEmpty(part2)) return part1;
                        
                        return $"{part1}, {part2}";
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
