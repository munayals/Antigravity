using System.Threading.Tasks;
using Antigravity.Api.Models;

namespace Antigravity.Api.Repositories
{
    public interface IFontaneriaRepository
    {
        Task<SiteVisit> GetSiteVisitByIdAsync(int id);
        void Add(Aviso aviso);
        Task SaveChangesAsync();
    }
}
