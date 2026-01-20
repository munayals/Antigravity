using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Antigravity.Api.Services
{
    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        
        // Configuration for CallMeBot (Free API)
        // You need to get an API Key from the bot.
        // 1. Add phone number +34 623 78 64 49 to your contacts
        // 2. Send "I allow callmebot to send me messages" to the bot
        // 3. Receive API Key
        private const string PHONE_NUMBER = "34658930906"; // User provided number
        private const string API_KEY = "1655363"; 

        public WhatsAppService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task SendNotificationAsync(string message)
        {
            try
            {
                // LOGGING (Mock Implementation)
                Console.WriteLine("=================================================");
                Console.WriteLine($"[WhatsApp Mock] To: +{PHONE_NUMBER}");
                Console.WriteLine($"[WhatsApp Mock] Message: {message}");
                Console.WriteLine("=================================================");

                // REAL IMPLEMENTATION
                if (!string.IsNullOrEmpty(API_KEY))
                {
                    var encodedMessage = HttpUtility.UrlEncode(message);
                    //var url = $"https://api.callmebot.com/whatsapp.php?phone={PHONE_NUMBER}&text={encodedMessage}&apikey={API_KEY}";
                    
                    //var response = await _httpClient.GetAsync(url);
                    //if (!response.IsSuccessStatusCode)
                    //{
                    //    Console.WriteLine($"[WhatsApp Error] Failed to send message. Status: {response.StatusCode}");
                    //}
                }
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WhatsApp Service Error] {ex.Message}");
            }
        }
    }
}
