using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Antigravity.Api.Utils
{
    public static class GeocodingUtils
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _cache 
            = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        static GeocodingUtils()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AntigravityWorkReports/1.0");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "es-ES,es;q=0.9");
        }

        public static async Task<string> GetAddressAsync(double? lat, double? lng)
        {
            if (!lat.HasValue || !lng.HasValue) return null;

            string key = $"{lat.Value:F6},{lng.Value:F6}";
            if (_cache.TryGetValue(key, out string cachedAddress))
            {
                return cachedAddress;
            }

            try
            {
                var latStr = lat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var lngStr = lng.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latStr}&lon={lngStr}&zoom=18&addressdetails=1";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JObject.Parse(content);
                    var addr = data["address"];
                    if (addr != null)
                    {
                        // Debug: Print raw address for troubleshooting
                        Console.WriteLine($"Geocoding Raw Address: {addr}");

                        // Comprehensive fallback list for "Street" based on OSM tags
                        var street = addr["road"]?.ToString() 
                                  ?? addr["living_street"]?.ToString()
                                  ?? addr["residential"]?.ToString()
                                  ?? addr["pedestrian"]?.ToString() 
                                  ?? addr["footway"]?.ToString()
                                  ?? addr["path"]?.ToString()
                                  ?? addr["neighbourhood"]?.ToString()
                                  ?? addr["suburb"]?.ToString()
                                  ?? addr["city_district"]?.ToString()
                                  ?? addr["quarter"]?.ToString()
                                  ?? addr["hamlet"]?.ToString()
                                  ?? "";

                        var number = addr["house_number"]?.ToString() ?? "";

                        var postcode = addr["postcode"]?.ToString() ?? "";
                        
                        var city = addr["city"]?.ToString() 
                                ?? addr["town"]?.ToString() 
                                ?? addr["village"]?.ToString() 
                                ?? "";
                        
                        // Construct address: "Street Number, Postcode City"
                        
                        var fullStreet = string.IsNullOrEmpty(number) ? street : $"{street} {number}";

                        var part1 = fullStreet;
                        if (string.IsNullOrEmpty(part1))
                            part1 =$"({latStr},{ lngStr})";
                        var part2 = $"{postcode} {city}".Trim();

                        if (string.IsNullOrEmpty(part1)) return part2;
                        if (string.IsNullOrEmpty(part2)) return part1;
                        
                        var result = $"{part1}, {part2}";
                        _cache.TryAdd(key, result);
                        return result;
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
