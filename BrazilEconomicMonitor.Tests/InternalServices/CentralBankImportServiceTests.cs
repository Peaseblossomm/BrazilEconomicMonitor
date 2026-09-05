using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.Tests.ExternalDependencies;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using BrazilEconomicMonitor.Domain.Entities;
using Microsoft.Extensions.Options;
using BrazilEconomicMonitor.Settings;
using BrazilEconomicMonitor.Services.InternalServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;


namespace BrazilEconomicMonitor.Tests.InternalServices
{
    public class CentralBankImportServiceTests
    {

        [Fact]
        public async Task ImportDataAsync_ParsedAndStoredToDb()   //using the httpClient, gets the json string, parses it and feeds it into the db
        {
            string fakeApiResponseBody = """  
                 [{ "data":"05/01/2025","valor":"11843110.3"},{ "data":"01/02/2025","valor":"11935727.9"},
                
                { "data":"01/03/2025","valor":"12039140.8"},{ "data":"01/04/2025","valor":"12134427.2"},{ "data":"01/05/2025","valor":"12230341.5"},
                { "data":"01/06/2025","valor":"12304727.1"},{ "data":"01/07/2025","valor":"12380925.6"},{ "data":"01/08/2025","valor":"12443552.8"},
                { "data":"01/09/2025","valor":"12524498.7"},{ "data":"01/10/2025","valor":"12589491.8"},{ "data":"01/11/2025","valor":"12654854.6"},
                { "data":"01/12/2025","valor":"12738565.6"},{ "data":"01/01/2026","valor":"12808404.9"},{ "data":"01/02/2026","valor":"12863688.7"},
                { "data":"01/03/2026","valor":"12964821.6"},{ "data":"01/04/2026","valor":"13038321.0"},{ "data":"01/05/2026","valor":"13106029.2"},
                { "data":"01/06/2026","valor":"13192888.1"}]
             """;

            var handler = new FakeHttpMessageHandler(fakeApiResponseBody);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://fake-centralbank.test")
            };

            var client = new CentralBankApiClient(httpClient);

            var connection = new SqliteConnection("Filename = :memory:");
            await connection.OpenAsync();

            var dbOptions = new DbContextOptionsBuilder<BrazilEconomicMonitorDbContext>()
                .UseSqlite(connection)
                .Options;

            var db = new BrazilEconomicMonitorDbContext(dbOptions);
            await db.Database.EnsureCreatedAsync();

            var source = new Sources
            {
                Name = "Central Bank",
                DocLink = "https://link"
            };

            db.Sources.Add(source);
            await db.SaveChangesAsync();

            var series = new Series
            {
                Name = "Nominal GDP",
                Code = "4382",
                SourceId = source.Id,
            };

            db.Series.Add(series);
            await db.SaveChangesAsync();

            IOptions<ImportSettings> options = Options.Create(new ImportSettings
            {
                LookbackMonths = 6
            });
            ILogger<CentralBankImportService> logger = NullLogger<CentralBankImportService>.Instance;

            var service = new CentralBankImportService(client, db, options, logger);

            await service.ImportFiscalAsync(
            "4382",
            "01/2025",
            "",
            CancellationToken.None);

            List<Observation> observations =
            await db.Observations.OrderBy(o =>o.ObservationDate).ToListAsync();

            Assert.NotEmpty(db.Observations);
            Assert.Equal(18, observations.Count);
            Assert.Equal(observations[0].SeriesId, series.Id); //parses correctly

            Assert.Equal(new DateTime(2025, 1, 1), observations[0].ObservationDate); // any day of the month should be normalized to the 1st of the MM
            Assert.Equal(11843110.3m, observations[0].Value);  // value exists and is of decimal type
        }
        [Fact]
        public async Task UpdateCentralBankDataAsync_()   // Tests that the method assigns the correct Uri for the ImportDataAsync method and that older values are updated
        {
            string fakeApiResponseBody = """
                        [{ "data":"05/01/2026","valor":"11843110.3"},{ "data":"01/02/2026","valor":"11935727.9"},
                         { "data":"01/03/2026","valor":"12039140.8"},{ "data":"01/04/2026","valor":"12134427.2"},
                         { "data":"01/05/2026","valor":"12230341.5"},{ "data":"01/06/2026","valor":"12304727.1"},
                         { "data":"01/07/2026","valor":"12304727.1"}]
                        """;

            var handler = new FakeHttpMessageHandler(fakeApiResponseBody);

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://fake-centralbank.test")
            };

            var client = new CentralBankApiClient(httpClient);

            var connection = new SqliteConnection("Filename = :memory:");
            await connection.OpenAsync();

            var dbOptions = new DbContextOptionsBuilder<BrazilEconomicMonitorDbContext>()
                .UseSqlite(connection)
                .Options;

            var db = new BrazilEconomicMonitorDbContext(dbOptions);
            await db.Database.EnsureCreatedAsync();

            var source = new Sources
            {
                Name = "Central Bank",
                DocLink = "https://link"
            };

            db.Sources.Add(source);
            await db.SaveChangesAsync();

            var series = new Series
            {
                Name = "Nominal GDP",
                Code = "4382",
                SourceId = source.Id,
            };
            Console.WriteLine(series.SourceId); 

            db.Series.Add(series);
            await db.SaveChangesAsync();

            var sixMonthsEarlierObservation = new Observation
            {
                SeriesId = series.Id,
                ObservationDate = new DateTime(2026, 1, 28),
                Value = 10.0m
            };
            var latestObservation = new Observation
            {
                SeriesId = series.Id,
                ObservationDate = new DateTime(2026, 7, 28),
                Value = 10.0m
            };

            db.Observations.AddRange(sixMonthsEarlierObservation, latestObservation);
            await db.SaveChangesAsync();

            IOptions<ImportSettings> options = Options.Create(new ImportSettings
            {
                LookbackMonths = 6
            });

            ILogger<CentralBankImportService> logger = NullLogger<CentralBankImportService>.Instance;

            var service = new CentralBankImportService(client, db, options, logger);

            await service.UpdateCentralBankDataAsync(CancellationToken.None);

            Console.WriteLine(handler.LastRequest.RequestUri);

            Assert.NotNull(handler.LastRequest);

            Assert.Contains(
                "dataInicial=01/01/2026",
                handler.LastRequest.RequestUri!.ToString()); // Client inserts the expected parameters inside the request URL

            Assert.Contains(
                "bcdata.sgs.4382/dados",
                handler.LastRequest.RequestUri!.ToString());

            Assert.Contains(
                "https://fake-centralbank.test/",
                handler.LastRequest.RequestUri!.ToString());

            Observation observationToBeUpdated = await db.Observations.SingleAsync(o => o.ObservationDate == new DateTime(2026, 1, 1));

            Assert.Equal(11843110.3m, observationToBeUpdated.Value); // older data is updated
        }
    }
}
