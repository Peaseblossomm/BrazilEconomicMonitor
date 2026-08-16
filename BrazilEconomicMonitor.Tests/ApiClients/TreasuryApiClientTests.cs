using BrazilEconomicMonitor.Infrastructure;
using BrazilEconomicMonitor.Tests.ExternalDependencies;
using System;
using System.Collections.Generic;
using System.Text;

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

            Assert.Contains(
                "codigo_da_serie=10.07.1",
                handler.LastRequest.RequestUri!.ToString()); // Client inserts the expected parameters inside the request URL

            Assert.Contains(
                "data_inicio=01/2025",
                handler.LastRequest.RequestUri!.ToString());

            Assert.Contains(
                "data_fim=12/2025",
                handler.LastRequest.RequestUri!.ToString());

            Assert.Contains(
                "tema=10",
                handler.LastRequest.RequestUri!.ToString());
        }
    }
}
