using Microsoft.EntityFrameworkCore;
using BrazilEconomicMonitor.Domain.Entities;

namespace BrazilEconomicMonitor.Infrastructure
{
    public class BrazilEconomicMonitorDbContext : DbContext
    {
        public BrazilEconomicMonitorDbContext(DbContextOptions<BrazilEconomicMonitorDbContext> options) : base(options)
        {
        }
        public DbSet<Series> Series => Set<Series>();
        public DbSet<Observation> Observations => Set<Observation>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Observation>()
                .HasOne(o => o.Series)
                .WithMany(s => s.Observations)
                .HasForeignKey(o => o.SeriesId);

            modelBuilder.Entity<Series>()
                .HasIndex(s => new { s.Source, s.Code })
                .IsUnique();

            modelBuilder.Entity<Observation>()
                .HasIndex(o => new
                {
                    o.SeriesId,
                    o.ObservationDate
                })
                .IsUnique();
        }
    }
}