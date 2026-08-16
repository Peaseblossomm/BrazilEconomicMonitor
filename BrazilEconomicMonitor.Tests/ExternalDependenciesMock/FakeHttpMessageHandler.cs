using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Text;

namespace BrazilEconomicMonitor.Tests.ExternalDependencies
{
    internal class FakeHttpMessageHandler: HttpMessageHandler
    {
        private readonly string _response;

        public HttpRequestMessage? LastRequest { get; private set; }

        public FakeHttpMessageHandler(string response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        {
            LastRequest = request;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_response)
            };

            return Task.FromResult(response);
        }

    }
}
