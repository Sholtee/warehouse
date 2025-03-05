/********************************************************************************
* RequireLocalStackAttribute.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Warehouse.Tests.Core
{
    public sealed class RequireLocalStackAttribute(params string[] services) : RequireExternalServiceAttribute("localstack/localstack:4.0.3", 4566, "test_localstack", $"SERVICES={string.Join(",", services)}")
    {
        private sealed record LocalStackStatus(Dictionary<string, string> Services);

        protected override void CloseConnection()
        {
        }

        protected override bool TryConnect(object fixture) => TryConnectAsync().GetAwaiter().GetResult();

        private async Task<bool> TryConnectAsync()
        {
            using HttpClient httpClient = new();

            httpClient.BaseAddress = new Uri("http://localhost:4566");

            try
            {
                HttpResponseMessage resp = await httpClient.GetAsync("/_localstack/health");
                if (resp.StatusCode != HttpStatusCode.OK)
                    return false;

                LocalStackStatus? status = await resp.Content.ReadFromJsonAsync<LocalStackStatus>
                (
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );
                foreach(string service in services)
                {
                    if (status?.Services.TryGetValue(service, out string? serviceStatus) is not true || serviceStatus is not "available")
                        return false;
                }

                return true;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }
    }
}
