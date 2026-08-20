using BrazilEconomicMonitor.DTOs;
using BrazilEconomicMonitor.Infrastructure;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;

namespace BrazilEconomicMonitor.Tests.ExternalServicesContracts
{
    public class TreasuryApiContractTests
    {

        [Fact]
        public async Task ApiCallNotEstablished()
        {

        }

        [Fact]

        public async Task ApiCallReturnsExpectedStructure()
        {
            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://apiapex.tesouro.gov.br/aria/")
            };

        var client = new TreasuryApiClient(httpClient);

        var response = await client.GetFiscalResultAsync(
            seriesCode: "10.07.1",
            startDate: "01/2025",
            endDate: "02/2025");

            using JsonDocument document = JsonDocument.Parse(response);

            JsonElement root = document.RootElement;

            Assert.True(root.TryGetProperty("status", out _));
            Assert.True(root.TryGetProperty("registros", out _)); // keys "status" and "registros" are present in the structure

            Assert.Equal("ok", root.GetProperty("status").GetString()); // status ok

            JsonElement registros =
            root.GetProperty("registros");

            JsonElement record = registros[0];

            Assert.Equal(2, registros.GetArrayLength()); // contains data for two months as requested
            Assert.True(record.TryGetProperty("data", out _)); 
            Assert.True(record.TryGetProperty("valor", out _)); // keys "data" and "valor" are present in the structure
        }

        [Fact]
        public async Task ApiCallContainsExpectedValues()
        {
            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://apiapex.tesouro.gov.br/aria/")
            };

            var client = new TreasuryApiClient(httpClient);

            var response = await client.GetFiscalResultAsync(
                seriesCode: "10.07.1",
                startDate: "01/2025",
                endDate: "02/2025");

            using JsonDocument document = JsonDocument.Parse(response);

            JsonElement root = document.RootElement;

            JsonElement registros =
            root.GetProperty("registros");

            DateTime date =
            registros[0].GetProperty("data").GetDateTime(); // is dateTime type

            decimal value =
            registros[0].GetProperty("valor").GetDecimal();  // is decimal type

            Assert.Equal(new DateTime(2025, 2, 1, 0, 0, 0), date); // retrieves expected date value
        }
    }
}
