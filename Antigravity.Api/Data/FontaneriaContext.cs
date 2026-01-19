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
        public DbSet<Client> Clients { get; set; }
        public DbSet<WorkDay> WorkDays { get; set; }
        public DbSet<Break> Breaks { get; set; }
        public DbSet<SiteVisit> SiteVisits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var databaseProvider = this.Database.ProviderName;

            // Common configuration
            modelBuilder.Entity<WorkDay>().HasKey(e => e.Id);
            modelBuilder.Entity<Break>().HasKey(e => e.Id);
            modelBuilder.Entity<SiteVisit>().HasKey(e => e.Id);

            if (databaseProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                // SQL Server Legacy Schema Mapping
                modelBuilder.Entity<Aviso>(entity =>
                {
                    entity.ToTable("Avisos");
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.Id).ValueGeneratedOnAdd();
                });

                modelBuilder.Entity<Client>(entity =>
                {
                    entity.ToTable("cliente"); // Legacy table name
                    entity.Property(e => e.Id).HasColumnName("codcli");
                    entity.Property(e => e.Name).HasColumnName("descli");
                    entity.Property(e => e.Address).HasColumnName("domicilio");
                    entity.Property(e => e.Phone).HasColumnName("telefono");
                    entity.Property(e => e.City).HasColumnName("pobcli");
                    entity.Ignore(e => e.Email); // Assuming legacy table doesn't have Email or map it if it does
                });

                modelBuilder.Entity<WorkDay>().ToTable("WorkDays");
                modelBuilder.Entity<Break>().ToTable("Breaks");
                modelBuilder.Entity<SiteVisit>().ToTable("SiteVisits");
            }
            else // PostgreSQL / Generic
            {
                // Normalized Schema
                modelBuilder.Entity<Aviso>().ToTable("Avisos"); // Keep same table name for consistency or change if needed
                
                modelBuilder.Entity<Client>(entity =>
                {
                    entity.ToTable("Clients");
                    entity.Property(e => e.Name).IsRequired();
                });

                modelBuilder.Entity<WorkDay>().ToTable("WorkDays");
                modelBuilder.Entity<Break>().ToTable("Breaks");
                modelBuilder.Entity<SiteVisit>().ToTable("SiteVisits");
            }
        }
    }
}
