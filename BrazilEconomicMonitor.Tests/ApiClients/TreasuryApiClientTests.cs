using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.Tests.ExternalDependencies;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace BrazilEconomicMonitor.Tests.ApiClients
{
    public class TreasuryApiClientTests
    {
        [Fact]
        public async Task GetFiscalResultAsync_ReturnsResponse()
        {
            // Arrange
            string fakeJson = """
    {
        "registros": []
    }
    """;

            var handler = new FakeHttpMessageHandler(fakeJson);

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://fake-treasury.test/")
            };

            var client = new TreasuryApiClient(httpClient);


            // Act
            string result = await client.GetFiscalResultAsync(
                seriesCode: "10.07.1",
                startDate: "01/2025",
                endDate: "12/2025");

            // Assert
            Assert.Equal(fakeJson, result); //handler provides the expected json

            Assert.NotNull(handler.LastRequest);

            Uri requestUri = handler.LastRequest.RequestUri!;

            var query = QueryHelpers.ParseQuery(requestUri.Query);

            Assert.Equal("01/2025", query["data_inicio"]);

            Assert.Equal("10.07.1", query["codigo_da_serie"]);

            Assert.Equal("12/2025", query["data_fim"]);

            Assert.Equal("10", query["tema"]);
        }
    }
}
