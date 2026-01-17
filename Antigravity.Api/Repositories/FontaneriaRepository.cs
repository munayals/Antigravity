using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Antigravity.Api.Models;
using Antigravity.Api.Data;

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
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT sv.id, sv.work_day_id, sv.site_name, sv.client_id, 
                           sv.check_in_time, sv.check_out_time, 
                           sv.check_in_lat, sv.check_in_lng, 
                           sv.check_out_lat, sv.check_out_lng, 
                           sv.description, sv.attachment_path, sv.status,
                           c.descli as client_name 
                    FROM SiteVisits sv 
                    LEFT JOIN cliente c ON sv.client_id = c.codcli 
                    WHERE sv.id = @id";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new SiteVisit
                            {
                                Id = reader.GetInt32(0),
                                WorkDayId = reader.GetInt32(1),
                                SiteName = reader.IsDBNull(2) ? null : reader.GetString(2),
                                ClientId = reader.IsDBNull(3) ? null : (int?)reader.GetInt32(3),
                                CheckInTime = reader.GetDateTime(4),
                                CheckOutTime = reader.IsDBNull(5) ? null : (DateTime?)reader.GetDateTime(5),
                                CheckInLat = reader.IsDBNull(6) ? null : (decimal?)reader.GetDecimal(6),
                                CheckInLng = reader.IsDBNull(7) ? null : (decimal?)reader.GetDecimal(7),
                                CheckOutLat = reader.IsDBNull(8) ? null : (decimal?)reader.GetDecimal(8),
                                CheckOutLng = reader.IsDBNull(9) ? null : (decimal?)reader.GetDecimal(9),
                                Description = reader.IsDBNull(10) ? null : reader.GetString(10),
                                AttachmentPath = reader.IsDBNull(11) ? null : reader.GetString(11),
                                Status = reader.IsDBNull(12) ? null : reader.GetString(12),
                                ClientName = reader.IsDBNull(13) ? null : reader.GetString(13)
                            };
                        }
                    }
                }
            }
            return null;
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
