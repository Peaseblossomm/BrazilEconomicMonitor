using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.DTOs;
using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.Services.QueryServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrazilEconomicMonitor.Tests.QueryServices
{
    public class DashboardQueryServiceTest
    {
        [Fact]
        public async Task GetLatestObservationByCountAsync_ListGenerated()
        {
            var connection = new SqliteConnection("filename = :memory:");

            await connection.OpenAsync();

            var dbOptions = new DbContextOptionsBuilder<BrazilEconomicMonitorDbContext>()
                .UseSqlite(connection)
                .Options;

            var _db = new BrazilEconomicMonitorDbContext(dbOptions);

            await _db.Database.EnsureCreatedAsync();

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


            Series primBalSeries = await _db.Series.Where(o => o.Code == "10.07.1").SingleAsync();

            Series nomGdpSeries = await _db.Series.Where(o => o.Code == "4382").SingleAsync();



            var observations = new List<Observation> {
                new Observation
                {
                    SeriesId = primBalSeries.Id,
                    ObservationDate = new DateTime(2026, 1, 1),
                    Value = 50m
                },
                new Observation
                {
                    SeriesId = primBalSeries.Id,
                    ObservationDate = new DateTime(2026, 2, 1),
                    Value = 50m
                },
                new Observation
                {
                    SeriesId = nomGdpSeries.Id,
                    ObservationDate = new DateTime(2026, 1, 1),
                    Value = 100m
                },
                new Observation
                {
                    SeriesId = nomGdpSeries.Id,
                    ObservationDate = new DateTime(2026, 2, 1),
                    Value = 100m
                },
                new Observation
                {
                    SeriesId = nomGdpSeries.Id,
                    ObservationDate = new DateTime(2026, 3, 1),
                    Value = 100m
                }
            };

            _db.Observations.AddRange(observations);
            await _db.SaveChangesAsync();

            var service = new DashboardQueryService(_db);


            List<ObservationResponseDto> observationsResult = await service.GetLatestObservationsByCountAsync("4382", 3, CancellationToken.None);


            Assert.NotNull(observationsResult);

            Assert.Equal(3, observationsResult.Count);

            Assert.Equal(100m, observationsResult[0].Value);

            Assert.Equal(new DateTime(2026, 1, 1), observationsResult[0].Date);

        }

        [Fact]
        public async Task GetLatestObservationByCountAsync_Nulllist()
        {
            var connection = new SqliteConnection("filename = :memory:");

            await connection.OpenAsync();

            var dbOptions = new DbContextOptionsBuilder<BrazilEconomicMonitorDbContext>()
                .UseSqlite(connection)
                .Options;

            var _db = new BrazilEconomicMonitorDbContext(dbOptions);

            await _db.Database.EnsureCreatedAsync();

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


            Series primBalSeries = await _db.Series.Where(o => o.Code == "10.07.1").SingleAsync();

            Series nomGdpSeries = await _db.Series.Where(o => o.Code == "4382").SingleAsync();



            var observations = new List<Observation> {
                new Observation
                {
                    SeriesId = primBalSeries.Id,
                    ObservationDate = new DateTime(2026, 1, 1),
                    Value = 50m
                },
                new Observation
                {
                    SeriesId = primBalSeries.Id,
                    ObservationDate = new DateTime(2026, 2, 1),
                    Value = 50m
                },
                new Observation
                {
                    SeriesId = nomGdpSeries.Id,
                    ObservationDate = new DateTime(2026, 1, 1),
                    Value = 100m
                },
                new Observation
                {
                    SeriesId = nomGdpSeries.Id,
                    ObservationDate = new DateTime(2026, 2, 1),
                    Value = 100m
                },
                new Observation
                {
                    SeriesId = nomGdpSeries.Id,
                    ObservationDate = new DateTime(2026, 3, 1),
                    Value = 100m
                }
            };

            _db.Observations.AddRange(observations);
            await _db.SaveChangesAsync();

            var service = new DashboardQueryService(_db);

            List<ObservationResponseDto> observationsResultZeroList = await service.GetLatestObservationsByCountAsync("4390", 2, CancellationToken.None);

            Assert.Empty(observationsResultZeroList);
        }
    }
}
