/********************************************************************************
* ProfiledHttpClient.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using StackExchange.Profiling;

namespace Warehouse.Host.Infrastructure.Profiling
{
    internal sealed class ProfiledHttpClient(HttpMessageHandler handler, string category, MiniProfiler? profiler): HttpClient(handler, disposeHandler: true)
    {
        public override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (profiler?.CustomTiming(category, request.RequestUri!.ToString(), request.Method.ToString()))
            {
                return base.Send(request, cancellationToken);
            }
        }

        public override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (profiler?.CustomTiming(category, request.RequestUri!.ToString(), request.Method.ToString()))
            {
                return await base.SendAsync(request, cancellationToken);
            }
        }
    }
}
