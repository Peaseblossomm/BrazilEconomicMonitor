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
    public class CbOlindaInterestRatesExpectationsImportServiceTest
    {
        [Fact]
        public async Task InterestRatesExpectationsImport_ParsedAndUpsertedCorrectly()
        {
            string fakeJson = """
                                {"@odata.context":"https://was-p.bcnet.bcb.gov.br/olinda/servico/Expectativas/versao/v1/odata$metadata#ExpectativasMercadoSelic","value":
                                [{"Indicador":"Selic","Data":"2026-08-28","Reuniao":"R5/2028",
                                "Media":11.0265,"Mediana":11.0000,"DesvioPadrao":0.9702,"Minimo":8.7500,"Maximo":13.7500,"numeroRespondentes":85,"baseCalculo":0},
                                {"Indicador":"Selic","Data":"2026-08-28","Reuniao":"R4/2028","Media":11.2436,"Mediana":11.2500,"DesvioPadrao":0.9017,"Minimo":9.0000,
                                "Maximo":14.0000,"numeroRespondentes":118,"baseCalculo":0},
                                {"Indicador":"Selic","Data":"2026-08-27","Reuniao":"R5/2028","Media":11.0370,"Mediana":11.0000,"DesvioPadrao":0.9727,"Minimo":8.7500,
                                "Maximo":13.7500,"numeroRespondentes":81,"baseCalculo":0},{"Indicador":"Selic","Data":"2026-08-27","Reuniao":"R4/2028",
                                "Media":11.2521,"Mediana":11.2500,"DesvioPadrao":0.8932,"Minimo":9.0000,"Maximo":14.0000,"numeroRespondentes":119,"baseCalculo":0},
                                {"Indicador":"Selic","Data":"2026-08-27","Reuniao":"R3/2028","Media":11.4187,"Mediana":11.5000,"DesvioPadrao":0.8193,"Minimo":9.2500,
                                "Maximo":14.0000,"numeroRespondentes":123,"baseCalculo":0}]
                                }
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
                Name = "Selic Rate R3/2028",

                Code = "ExpectativasMercadoSelic_R3/2028",

                SourceId = source.Id
            };

            db.Series.Add(series);

            await db.SaveChangesAsync();

            var outdatedObservation = new Observation
            {
                Value = 11.25m,

                ObservationDate = new DateTime(2026, 8, 27),
                SeriesId = series.Id
            };

            db.Observations.Add(outdatedObservation); //Problem ima tuka, duplo vpisuva, ne ja pronaoga postoeckava observacija

            await db.SaveChangesAsync();

            IOptions<ImportSettings> options = Options.Create(new ImportSettings
            {
                LookbackMonths = 3
            });

            ILogger<CbOlindaImportService> logger = NullLogger<CbOlindaImportService>.Instance;

            var service = new CbOlindaImportService(client, db, logger, options);

            int count = 3; // fakeJson received, this is not a relevant parameter

            await service.ImportIntrestRatesExpectationsAsync(count, CancellationToken.None);

            List<Series> seriesCreatedByMethod = await db.Series.ToListAsync();

            List<Observation> observations = await db.Observations.OrderBy(o => o.ObservationDate).ToListAsync();

            List<Observation> R5_2028_observations = await db.Observations.Where(o => o.Series.Name == "Selic Rate R5/2028").ToListAsync();

            Observation R5_2028_28082026 = await db.Observations.Where(o => o.ObservationDate == new DateTime (2026, 8, 28) 
            && o.Series.Name == "Selic Rate R5/2028")
                .SingleAsync();


            Assert.Equal(3, seriesCreatedByMethod.Count);

            Assert.Contains(seriesCreatedByMethod, s => s.Name == "Selic Rate R5/2028");

            Assert.Equal(2, R5_2028_observations.Count);

            Assert.Equal(11.0m, R5_2028_28082026.Value);

        }
    }
}
