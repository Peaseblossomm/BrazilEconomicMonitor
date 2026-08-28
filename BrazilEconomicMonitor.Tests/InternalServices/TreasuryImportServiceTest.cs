using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.Settings;
using BrazilEconomicMonitor.Services;
using BrazilEconomicMonitor.Tests.ExternalDependencies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrazilEconomicMonitor.Tests.InternalServices
{

    public class TreasuryImportServiceTest
    {


        [Fact]
        public async Task ImportFiscalAsync_ParsedCorrectly()
        {
            string fakeJsonBody = """
                
                        { "next":"https://apiapex.tesouro.gov.br/aria//v1/series-temporais/custom/resultado-fiscal?data_inicio=03/2026&data_fim=05/2026&tema=10&codigo_da_serie=10.08.1&page=2&pageSize=1000","pageSize":1000,"registros":
                        [{ "nomeTema":"Resultado Fiscal do Governo Central - Valores Mensais","nomeSubtema":"Juros Nominais","codigoTema":"10","data":"2026-05-01T00:00:00.000Z"
                        ,"codigoSubtema":"10.08","valor":-97894.60247674999,"codigoSerie":"10.08.1","nomeSerie":"Juros Nominais"},
                        { "nomeTema":"Resultado Fiscal do Governo Central - Valores Mensais","nomeSubtema":"Juros Nominais","codigoTema":"10","data":"2026-04-01T00:00:00.000Z",
                        "codigoSubtema":"10.08","valor":-76166.93540718002,"codigoSerie":"10.08.1","nomeSerie":"Juros Nominais"},
                        { "nomeTema":"Resultado Fiscal do Governo Central - Valores Mensais","nomeSubtema":"Juros Nominais","codigoTema":"10","data":"2026-03-01T00:00:00.000Z",
                        "codigoSubtema":"10.08","valor":-112180.04695486999,"codigoSerie":"10.08.1","nomeSerie":"Juros Nominais"}]
                        ,"page":1,"status":"ok"} 
                
                     """;
            var handler = new FakeHttpMessageHandler(fakeJsonBody);

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://fake-treasury.test")
            };

            var client = new TreasuryApiClient(httpClient);

            var connection = new SqliteConnection("filename = :memory:");
            await connection.OpenAsync();

            var dbOptions = new DbContextOptionsBuilder<BrazilEconomicMonitorDbContext>()
                .UseSqlite(connection)
                .Options;

            var db = new BrazilEconomicMonitorDbContext(dbOptions);
            await db.Database.EnsureCreatedAsync();


            var source = new Sources
            {
                Name = "Treasury",
                DocLink = "https://link"
            };

            db.Sources.Add(source);
            await db.SaveChangesAsync();

            var series = new Series
            {
                Name = "Test Series",
                Code = "666",
                SourceId = source.Id
            };

            db.Series.Add(series);
            await db.SaveChangesAsync();

            IOptions<ImportSettings> options = Options.Create(new ImportSettings
            {
                CentralBankLookbackMonths = 3
            });

            var service = new TreasuryImportService(client, db, options);

            await service.ImportFiscalAsync(
                "666",
                "03/2026",
                "06/2026",
                CancellationToken.None);

            List<Observation> observations = await db.Observations.OrderBy(o => o.ObservationDate).ToListAsync();

            Assert.NotEmpty(observations);
            Assert.Equal(3, observations.Count);

            Assert.Equal(new DateTime(2026, 3, 1), observations[0].ObservationDate);
            Assert.Equal(-112180.04695486999m, observations[0].Value);
        }

        [Fact]
        public async Task UpdateTreasuryDataAsyncTest_CorrectUriFormation()
        {
            string fakeJsonBody = """
                                    {"next":"https://apiapex.tesouro.gov.br/aria//v1/series-temporais/custom/resultado-fiscal?data_inicio=02/2026&data_fim=05/2026&tema=10&codigo_da_serie=10.08.1&page=2&pageSize=1000","pageSize":1000,"registros":
                                    [{"nomeTema":"Resultado Fiscal do Governo Central - Valores Mensais","nomeSubtema":"Juros Nominais","codigoTema":"10","data":"2026-05-01T00:00:00.000Z","codigoSubtema":"10.08","valor":-97894.60247674999,"codigoSerie":"10.08.1",
                                                        "nomeSerie":"Juros Nominais"},{"nomeTema":"Resultado Fiscal do Governo Central - Valores Mensais","nomeSubtema":"Juros Nominais","codigoTema":"10","data":"2026-04-01T00:00:00.000Z",
                                                        "codigoSubtema":"10.08","valor":-76166.93540718002,"codigoSerie":"10.08.1","nomeSerie":"Juros Nominais"},{"nomeTema":"Resultado Fiscal do Governo Central - Valores Mensais",
                                                        "nomeSubtema":"Juros Nominais","codigoTema":"10","data":"2026-03-01T00:00:00.000Z","codigoSubtema":"10.08","valor":-112180.04695486999,"codigoSerie":"10.08.1","nomeSerie":"Juros Nominais"},
                                                        {"nomeTema":"Resultado Fiscal do Governo Central - Valores Mensais","nomeSubtema":"Juros Nominais","codigoTema":"10","data":"2026-02-01T00:00:00.000Z","codigoSubtema":"10.08","valor":-78236.69163956001,
                                                        "codigoSerie":"10.08.1","nomeSerie":"Juros Nominais"}],"page":1,"status":"ok"}
                                  """;

            var handler = new FakeHttpMessageHandler(fakeJsonBody);

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://fake-treasury.test")
            };

            var client = new TreasuryApiClient(httpClient);

            var connection = new SqliteConnection("filename = :memory:");
            await connection.OpenAsync();

            var dbOptions = new DbContextOptionsBuilder<BrazilEconomicMonitorDbContext>()
                .UseSqlite(connection)
                .Options;

            var db = new BrazilEconomicMonitorDbContext(dbOptions);
            await db.Database.EnsureCreatedAsync();


            var source = new Sources
            {
                Name = "Treasury",
                DocLink = "https://link"
            };

            db.Sources.Add(source);
            await db.SaveChangesAsync();

            var series = new Series
            {
                Name = "Test Series",
                Code = "666",
                SourceId = source.Id
            };

            db.Series.Add(series);
            await db.SaveChangesAsync();

            var ThreeMonthsEarlierObservation = new Observation
            {
                SeriesId = series.Id,
                ObservationDate = new DateTime(2026, 2, 28),
                Value = 10.0m
            };

            var latestObservation = new Observation
            {
                SeriesId = series.Id,
                ObservationDate = new DateTime(2026, 5, 28),
                Value = 10.0m
            };

            db.Observations.AddRange(ThreeMonthsEarlierObservation, latestObservation);
            await db.SaveChangesAsync();

            IOptions<ImportSettings> options = Options.Create(new ImportSettings
            {
                TreasuryLookbackMonths = 3
            });

            var service = new TreasuryImportService(client, db, options);

            await service.UpdateTreasuryDataAsync(CancellationToken.None);

            Console.WriteLine(handler.LastRequest.RequestUri);

            Assert.NotNull(handler.LastRequest);

            Assert.Contains("?data_inicio=02/2026", handler.LastRequest.RequestUri!.ToString());
            Assert.Contains("&codigo_da_serie=666", handler.LastRequest.RequestUri!.ToString());

            var observationToBeUpdated = await db.Observations.SingleAsync(o => o.ObservationDate == new DateTime(2026, 2, 1));

            Assert.Equal(-78236.69163956001m, observationToBeUpdated.Value);

        }
    }
}
