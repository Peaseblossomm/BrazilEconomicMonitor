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
        public DbSet<Sources> Sources => Set<Sources>();
        public DbSet<SelicRateIntervals> SelicRateIntervals => Set<SelicRateIntervals>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Observation>()
                .HasOne(o => o.Series)
                .WithMany(s => s.Observations)
                .HasForeignKey(o => o.SeriesId);

            modelBuilder.Entity<Observation>()
                .HasIndex(o => new
                {
                    o.SeriesId,
                    o.ObservationDate
                })
                .IsUnique();

            modelBuilder.Entity<Series>()
                .HasOne(o => o.Sources)
                .WithMany(o => o.Series)
                .HasForeignKey(s => s.SourceId);

            modelBuilder.Entity<Series>()
                .HasIndex(s => new { s.SourceId, s.Code })
                .IsUnique();

            modelBuilder.Entity<SelicRateIntervals>()
                .HasIndex(o => new
                {
                    o.StartDate,
                    o.EndDate
                })
                .IsUnique();
        }
    }
}