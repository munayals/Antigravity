using System.Threading.Tasks;

namespace Antigravity.Api.Services
{
    public interface IWhatsAppService
    {
        Task SendNotificationAsync(string message);
    }
}
