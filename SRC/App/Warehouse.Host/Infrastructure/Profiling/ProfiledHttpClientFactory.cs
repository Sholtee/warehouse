/********************************************************************************
* ProfiledHttpClientFactory.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Net;
using System.Net.Http;

using Amazon.Runtime;
using StackExchange.Profiling;

namespace Warehouse.Host.Infrastructure.Profiling
{
    internal sealed class ProfiledHttpClientFactory : HttpClientFactory
    {
        public override HttpClient CreateHttpClient(IClientConfig clientConfig)
        {
            #pragma warning disable CA2000 // Handler is disposed by the client
            HttpClientHandler httpMessageHandler = new();
            #pragma warning restore CA2000

            if (clientConfig.MaxConnectionsPerServer is not null)
                httpMessageHandler.MaxConnectionsPerServer = clientConfig.MaxConnectionsPerServer.Value;

            httpMessageHandler.AllowAutoRedirect = clientConfig.AllowAutoRedirect;
            httpMessageHandler.AutomaticDecompression = DecompressionMethods.None;
            httpMessageHandler.CheckCertificateRevocationList = true;

            IWebProxy? proxy = clientConfig.GetWebProxy();
            if (proxy is not null)
            {
                httpMessageHandler.Proxy = proxy;
                if (clientConfig.ProxyCredentials is not null)
                    httpMessageHandler.Proxy.Credentials = clientConfig.ProxyCredentials;
            }

            ProfiledHttpClient httpClient = new(httpMessageHandler, "AWS", MiniProfiler.Current);

            if (clientConfig.Timeout is not null)
                httpClient.Timeout = clientConfig.Timeout.Value;

            return httpClient;
        }
    }
}
