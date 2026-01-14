using Microsoft.EntityFrameworkCore;

namespace Antigravity.DAL.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Aquí podrás añadir tus DbSets (tablas), por ejemplo:
    // public DbSet<Usuario> Usuarios { get; set; }
}
