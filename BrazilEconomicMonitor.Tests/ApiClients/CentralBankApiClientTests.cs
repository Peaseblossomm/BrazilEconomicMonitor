using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.Tests.ExternalDependencies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
                BaseAddress = new Uri("https://fake-centralbank.test")
            };

            ILogger<CentralBankApiClient> logger = NullLogger<CentralBankApiClient>.Instance;

            var client = new CentralBankApiClient(httpClient, logger);

            string result = await client.GetFiscalResultsAsync(
                seriesCode: "4382",
                startDate: "01/01/2026",
                endDate: "01/07/2026");

            Assert.Equal(fakeJson, result); //handler provides the expected json
            Assert.NotNull(handler.LastRequest);

            Uri requestUri = handler.LastRequest.RequestUri!;

            var query = QueryHelpers.ParseQuery(requestUri.Query);

            Assert.Equal(
                "01/01/2026",
                query["dataInicial"]); // Client inserts the expected parameters inside the request URL

            Assert.Equal(
                "01/07/2026",
                query["dataFinal"]);

            Assert.Equal(
                "json",
                query["formato"]);

            Assert.Contains(
                "bcdata.sgs.4382/dados",
                handler.LastRequest.RequestUri!.ToString());

            Assert.Contains(
                "https://fake-centralbank.test/",
                handler.LastRequest.RequestUri!.ToString());
        }
    }
}
