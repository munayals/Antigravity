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
        public DbSet<AvisoStatusHistory> AvisoStatusHistory { get; set; }

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
                    entity.HasOne(a => a.Client)
                          .WithMany()
                          .HasForeignKey(a => a.ClientId);
                });

                modelBuilder.Entity<Client>(entity =>
                {
                    entity.ToTable("cliente"); // Legacy table name
                    entity.Property(e => e.Id).HasColumnName("codcli");
                    entity.Property(e => e.Name).HasColumnName("descli");
                    entity.Property(e => e.Address).HasColumnName("dircli");
                    entity.Property(e => e.Phone).HasColumnName("telefono1");
                    entity.Property(e => e.City).HasColumnName("pobcli");
                    entity.Ignore(e => e.Email);
                });

                modelBuilder.Entity<WorkDay>().ToTable("WorkDays");
                modelBuilder.Entity<Break>().ToTable("Breaks");
                modelBuilder.Entity<SiteVisit>().ToTable("SiteVisits");
            }
            else // PostgreSQL / Generic
            {
                // Normalized Schema
                modelBuilder.Entity<Aviso>(entity =>
                {
                    entity.ToTable("Avisos");
                    entity.HasOne(a => a.Client)
                          .WithMany()
                          .HasForeignKey(a => a.ClientId);
                });
                
                modelBuilder.Entity<Client>(entity =>
                {
                    entity.ToTable("Clients");
                    entity.Property(e => e.Name).IsRequired();
                });

                modelBuilder.Entity<WorkDay>().ToTable("WorkDays");
                modelBuilder.Entity<Break>().ToTable("Breaks");
                modelBuilder.Entity<SiteVisit>().ToTable("SiteVisits");
                
                modelBuilder.Entity<AvisoStatusHistory>(entity =>
                {
                    entity.ToTable("AvisoStatusHistory");
                    entity.HasOne(h => h.Aviso)
                          .WithMany()
                          .HasForeignKey(h => h.AvisoId)
                          .OnDelete(DeleteBehavior.Cascade);
                });
            }
        }
    }
}
