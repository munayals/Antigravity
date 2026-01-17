using Microsoft.EntityFrameworkCore;
using Antigravity.Api.Models;

namespace Antigravity.Api.Data
{
    public class FontaneriaContext : DbContext
    {
        public FontaneriaContext(DbContextOptions<FontaneriaContext> options) : base(options)
        {
        }

        public DbSet<Aviso> Avisos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Aviso>(entity =>
            {
                entity.ToTable("Avisos");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
            });
        }
    }
}
