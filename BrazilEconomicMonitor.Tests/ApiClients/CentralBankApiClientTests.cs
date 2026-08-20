using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.Tests.ExternalDependencies;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrazilEconomicMonitor.Tests.ApiClients
{
    public class CentralBankApiClientTests
    {
        [Fact]
        public async Task GetFiscalResultAsync_returnsResponse()
        {
            string fakeJson = """
    {
        fakeCategory: []
    }
    """;
            var handler = new FakeHttpMessageHandler(fakeJson);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://fake-centralbank.test/")
            };

            var client = new CentralBankApiClient(httpClient);

            string result = await client.GetFiscalResultsAsync(
                seriesCode: "4382",
                startDate: "01/01/2026",
                endDate: "01/07/2026");

            Assert.Equal(fakeJson, result); //handler provides the expected json
            Assert.NotNull(handler.LastRequest);

            Assert.Contains(
                "dataInicial=01/01/2026",
                handler.LastRequest.RequestUri!.ToString()); // Client inserts the expected parameters inside the request URL

            Assert.Contains(
                "dataFinal=01/07/2026",
                handler.LastRequest.RequestUri!.ToString());

            Assert.Contains(
                ".sgs.4382",
                handler.LastRequest.RequestUri!.ToString());
            Assert.Contains(
                "https://api.bcb.gov.br/dados/serie/bcdata.sgs.4382/dados?formato=json&dataInicial=01/01/2026&dataFinal=01/07/2026",
                handler.LastRequest.RequestUri!.ToString());
        }
    }
}
