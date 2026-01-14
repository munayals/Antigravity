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
                        var street = addr["road"]?.ToString() ?? addr["pedestrian"]?.ToString() ?? addr["suburb"]?.ToString() ?? "";
                        var postcode = addr["postcode"]?.ToString() ?? "";
                        var city = addr["city"]?.ToString() ?? addr["town"]?.ToString() ?? addr["village"]?.ToString() ?? "";
                        
                        return $"{street}{(string.IsNullOrEmpty(street) ? "" : ", ")}{postcode} {city}".Trim();
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
