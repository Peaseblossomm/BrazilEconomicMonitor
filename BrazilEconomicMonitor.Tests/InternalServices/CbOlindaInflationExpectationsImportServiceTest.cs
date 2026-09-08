using BrazilEconomicMonitor.Domain.Entities;
using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.Services.InternalServices;
using BrazilEconomicMonitor.Settings;
using BrazilEconomicMonitor.Tests.ExternalDependencies;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrazilEconomicMonitor.Tests.InternalServices
{
    public class CbOlindaImportService_InflationExpectationsTest
    {
        [Fact]
        public async Task InflationExpectationsTest_ParsedAndUpsertedCorrectly()
        {
            string fakeJson = """
                
                                    { "@odata.context":"https://was-p.bcnet.bcb.gov.br/olinda/servico/Expectativas/versao/v1/odata$metadata#ExpectativasMercadoInflacao12Meses"
                                    ,"value":[ { "Indicador":"IPCA Serviços","Data":"2021-09-14","Suavizada":"S","Media":4.8967,"Mediana":4.8967,"DesvioPadrao":0.0000,
                                    "Minimo":4.8967,"Maximo":4.8967,"numeroRespondentes":1,"baseCalculo":1},
                                    { "Indicador":"IPCA Serviços","Data":"2021-09-15","Suavizada":"S","Media":5.3022,"Mediana":5.3022,"DesvioPadrao":0.4102,
                                    "Minimo":4.8920,"Maximo":5.7123,"numeroRespondentes":2,"baseCalculo":1}
                                    ]}
                              """;

            var handler = new FakeHttpMessageHandler(fakeJson);

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://fake-olinda.test")
            };

            var client = new CbOlindaApiClient(httpClient);

            var connection = new SqliteConnection("filename=:memory:");

            await connection.OpenAsync();

            var dbOptions = new DbContextOptionsBuilder<BrazilEconomicMonitorDbContext>()
                .UseSqlite(connection)
                .Options;

            var db = new BrazilEconomicMonitorDbContext(dbOptions);

            await db.Database.EnsureCreatedAsync();

            var source = new Sources
            {
                Name = "Central Bank Olinda",

                DocLink = "https://link"
            };

            db.Sources.Add(source);

            await db.SaveChangesAsync();

            var series = new Series
            {
                Name = "Inflation Expectation 12 months",

                Code = "ExpectativasMercadoInflacao12Meses",

                SourceId = source.Id
            };

            db.Series.Add(series);

            await db.SaveChangesAsync();

            var outdatedObservation = new Observation
            {
                Value = 8.777m,

                ObservationDate = new DateTime(2021, 9, 14),

                SeriesId = series.Id
            };

            db.Observations.Add(outdatedObservation);

            await db.SaveChangesAsync();

            IOptions<ImportSettings> options = Options.Create(new ImportSettings
            {
                LookbackMonths = 3
            });

            ILogger<CbOlindaImportService> logger = NullLogger<CbOlindaImportService>.Instance;

            var service = new CbOlindaImportService(client, db, logger, options);

            int count = 3; // fakeJson received, this is not a relevant parameter 

            await service.ImportInflationExpectationsAsync(count, CancellationToken.None); // fakeJson response 


            List<Observation> observations = await db.Observations.OrderBy(o => o.ObservationDate).ToListAsync();


            Assert.NotEmpty(observations);

            Assert.Equal(2, observations.Count());

            Assert.Equal(new DateTime(2021, 9, 14), observations[0].ObservationDate); // parsed DateTime correctly

            Assert.Equal(4.8967m, observations[0].Value); // parsed decimal correctly and updated the existing outdatedObservation










        }

    }
}
