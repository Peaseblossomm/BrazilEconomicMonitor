using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrazilEconomicMonitor.Tests.InternalServices
{
    public class DataTransformationService_PrimBalanceOverNomGdpTest: IAsyncLifetime
    {
        private SqliteConnection _connection = null!;
        private BrazilEconomicMonitorDbContext _db = null!;
        private DataTransformationService _service = null!;

        public async Task InitializeAsync()
        {
            _connection = new SqliteConnection("filename = :memory:");

            await _connection.OpenAsync();

            var dbOptions = new DbContextOptionsBuilder<BrazilEconomicMonitorDbContext>()
                .UseSqlite(_connection)
                .Options;

            _db = new BrazilEconomicMonitorDbContext(dbOptions);
            await _db.Database.EnsureCreatedAsync();

            ILogger<DataTransformationService> logger = NullLogger<DataTransformationService>.Instance;

            _service = new DataTransformationService(_db, logger);
        }
        public async Task DisposeAsync()
        {
            await _db.DisposeAsync();
            await _connection.DisposeAsync();
        }

        [Fact]
        public async Task PrimaryBalanceOverGdp_CorrectValue_Upserted()
        {

            var sources = new List<Sources>
            {
                new Sources
                {
                    Name = "Treasury",
                    DocLink = "linkT"
                },

                new Sources
                {
                    Name = "Central Bank",
                    DocLink = "linkCB"
                },

                new Sources
                {
                    Name = "Derived Value",
                    DocLink = "none"
                }
            };

            _db.Sources.AddRange(sources);
            await _db.SaveChangesAsync();

            var series = new List<Series>
            {
                new Series
                {
                    Name = "Primary Balance",
                    Code = "10.07.1",
                    SourceId = sources[0].Id

                },

                new Series
                {
                    Name = "Nominal GDP",
                    Code = "4382",
                    SourceId = sources[1].Id

                }
            };

            _db.Series.AddRange(series);
            await _db.SaveChangesAsync();

            var observations = new List<Observation> {
                new Observation
                {
                    SeriesId = series[0].Id,
                    ObservationDate = new DateTime(2026, 1, 1),
                    Value = 100m
                },
                new Observation
                {
                    SeriesId = series[0].Id,
                    ObservationDate = new DateTime(2026, 2, 1),
                    Value = 50m
                },
                new Observation
                {
                    SeriesId = series[1].Id,
                    ObservationDate = new DateTime(2026, 1, 1),
                    Value = 100m
                },
                new Observation
                {
                    SeriesId = series[1].Id,
                    ObservationDate = new DateTime(2026, 2, 1),
                    Value = 100m
                },
                new Observation
                {
                    SeriesId = series[1].Id,
                    ObservationDate = new DateTime(2026, 3, 1),
                    Value = 100m
                }
            };

            _db.Observations.AddRange(observations);
            await _db.SaveChangesAsync();

            Console.WriteLine($"Date0: {observations[0].ObservationDate:yyyy-MM-dd}, " +
        $"Date1: {observations[1].ObservationDate:yyyy-MM-dd}, " +
        $"Date2: {observations[2].ObservationDate:yyyy-MM-dd}, "+
        $"Date3: {observations[3].ObservationDate:yyyy-MM-dd}, " +
        $"Date4: {observations[4].ObservationDate:yyyy-MM-dd}, ");

            await _service.PrimaryBalanceOverGdp(CancellationToken.None);

            List<Observation> derivedObservations = await _db.Observations.Where(o => o.Series.IsRaw == false).OrderByDescending(o => o.ObservationDate).ToListAsync();


            Assert.Equal(2, derivedObservations.Count);

            Assert.Equal(new DateTime(2026, 2, 1), derivedObservations[0].ObservationDate);

            Assert.Equal(50m, derivedObservations[0].Value);

        }
    }
}
