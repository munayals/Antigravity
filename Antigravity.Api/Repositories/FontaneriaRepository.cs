using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Antigravity.Api.Models;
using Antigravity.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Antigravity.Api.Repositories
{
    public class FontaneriaRepository : RepositoryBase, IFontaneriaRepository
    {
        private readonly FontaneriaContext _context;

        public FontaneriaRepository(IConfiguration configuration, FontaneriaContext context) : base(configuration)
        {
            _context = context;
        }


        
        public async Task<SiteVisit> GetSiteVisitByIdAsync(int id)
        {
            var siteVisit = await _context.SiteVisits
                .Include(sv => sv.WorkDay)
                .FirstOrDefaultAsync(sv => sv.Id == id);

            if (siteVisit != null)
            {
                // Manually populate ClientName since Client is conditionally mapped/linked
                // Ideally this should be a proper join if relationships are set up, but for dual-compat logic:
                if (siteVisit.ClientId.HasValue)
                {
                    var client = await _context.Clients.FindAsync(siteVisit.ClientId.Value);
                    if (client != null)
                    {
                        siteVisit.ClientName = client.Name;
                    }
                }
            }
            return siteVisit;
        }

        public void Add(Aviso aviso)
        {
            _context.Avisos.Add(aviso);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
