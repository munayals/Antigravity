using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Antigravity.Api.Models;
using Antigravity.Api.Repositories;
using Antigravity.Api.Services; // Ensure this namespace exists for ISimpleMapper
using System.Data;

namespace Antigravity.Api.Controllers
{
    [ApiController]
    [Route("api/avisos")]
    public class AvisosController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IFontaneriaRepository _repository;
        private readonly ISimpleMapper _mapper;

        public AvisosController(IConfiguration configuration, IFontaneriaRepository repository, ISimpleMapper mapper)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _repository = repository;
            _mapper = mapper;
        }

        private string GetUserEmail() => "demo@example.com";

        [HttpGet]
        public async Task<IActionResult> GetAvisos([FromQuery] string? status, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var avisos = new List<AvisoWithClientDto>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT a.id, a.codcli, a.request_time, a.reason, a.status, a.priority, a.estimated_hours, a.commitment_time, a.user_email,
                           c.descli, c.dircli, c.telefono1, c.pobcli
                    FROM Avisos a
                    JOIN cliente c ON a.codcli = c.codcli
                    WHERE 1=1";

                if (!string.IsNullOrEmpty(status))
                    sql += " AND a.status = @status";
                
                if (startDate.HasValue)
                    sql += " AND a.request_time >= @start";

                if (endDate.HasValue)
                {
                    // End of date
                    sql += " AND a.request_time <= @end";
                }

                sql += " ORDER BY a.request_time DESC";

                using (var command = new SqlCommand(sql, connection))
                {
                    if (!string.IsNullOrEmpty(status))
                        command.Parameters.AddWithValue("@status", status);
                    
                    if (startDate.HasValue)
                        command.Parameters.AddWithValue("@start", startDate.Value);
                    
                    if (endDate.HasValue)
                        command.Parameters.AddWithValue("@end", endDate.Value.Date.AddDays(1).AddTicks(-1));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var aviso = new AvisoWithClientDto
                            {
                                Id = reader.GetInt32(0),
                                ClientId = reader.GetInt32(1),
                                RequestTime = reader.GetDateTime(2),
                                Reason = reader.GetString(3),
                                Status = reader.GetString(4),
                                Priority = reader.IsDBNull(5) ? "NORMAL" : reader.GetString(5),
                                EstimatedHours = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                                CommitmentTime = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                                UserEmail = reader.IsDBNull(8) ? null : reader.GetString(8),
                                
                                ClientName = reader.GetString(9),
                                ClientAddress = reader.IsDBNull(10) ? "" : reader.GetString(10),
                                ClientPhone = reader.IsDBNull(11) ? "" : reader.GetString(11),
                                ClientCity = reader.IsDBNull(12) ? "" : reader.GetString(12)
                            };
                            avisos.Add(aviso);
                        }
                    }
                }
            }
            return Ok(avisos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAviso(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT a.id, a.codcli, a.request_time, a.reason, a.status, a.priority, a.estimated_hours, a.commitment_time, a.user_email,
                           c.descli, c.dircli, c.telefono1, c.pobcli
                    FROM Avisos a
                    JOIN cliente c ON a.codcli = c.codcli
                    WHERE a.id = @id";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var aviso = new AvisoWithClientDto
                            {
                                Id = reader.GetInt32(0),
                                ClientId = reader.GetInt32(1),
                                RequestTime = reader.GetDateTime(2),
                                Reason = reader.GetString(3),
                                Status = reader.GetString(4),
                                Priority = reader.IsDBNull(5) ? "NORMAL" : reader.GetString(5),
                                EstimatedHours = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                                CommitmentTime = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                                UserEmail = reader.IsDBNull(8) ? null : reader.GetString(8),
                                
                                ClientName = reader.GetString(9),
                                ClientAddress = reader.IsDBNull(10) ? "" : reader.GetString(10),
                                ClientPhone = reader.IsDBNull(11) ? "" : reader.GetString(11),
                                ClientCity = reader.IsDBNull(12) ? "" : reader.GetString(12)
                            };
                            return Ok(aviso);
                        }
                    }
                }
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<Aviso>> CreateAviso([FromBody] Aviso aviso)
        {
            // Repository Pattern Implementation as requested
            if (aviso.Id != 0) return BadRequest("No puede modificar un aviso desde este método");

            // Mapping/Preparation
            // Note: Since we use the same model (Aviso) for DTO and Entity, we map implicitly.
            // If using separate DTOs, _mapper.Map<Aviso>(avisoDto) would be here.
            
            if (aviso.CommitmentTime?.Kind == DateTimeKind.Utc) 
                aviso.CommitmentTime = aviso.CommitmentTime?.ToLocalTime();

            aviso.RequestTime = DateTime.Now;
            aviso.UserEmail = GetUserEmail(); // Simulating User.Identity.GetUserId()

            _repository.Add(aviso);
            await _repository.SaveChangesAsync();

            return Ok(aviso); // Return the entity/dto
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAviso(int id, [FromBody] Aviso aviso)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                    UPDATE Avisos 
                    SET reason = @reason, status = @status, priority = @priority, 
                        estimated_hours = @estHours, commitment_time = @commitTime
                    WHERE id = @id";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@reason", aviso.Reason);
                    command.Parameters.AddWithValue("@status", aviso.Status);
                    command.Parameters.AddWithValue("@priority", aviso.Priority);
                    command.Parameters.AddWithValue("@estHours", aviso.EstimatedHours ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@commitTime", aviso.CommitmentTime ?? (object)DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }
            }
            return Ok();
        }
    }
}
