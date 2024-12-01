using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

using Amazon.SecretsManager.Model;
using Amazon.SecretsManager;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Warehouse.API
{
    using static Helpers;

    public static class Program
    {
        private static void UsingHttps(KestrelServerOptions serverOpts) => serverOpts.Listen
        (
            IPAddress.Any,
            GetEnvironmentVariable("API_PORT", 1986),
            static listenOpts =>
            {
                IServiceProvider services = listenOpts.ApplicationServices;

                Dictionary<string, string> cert = JsonSerializer.Deserialize<Dictionary<string, string>>
                (
                    services
                        .GetRequiredService<IAmazonSecretsManager>()
                        .GetSecretValueAsync
                        (
                            new GetSecretValueRequest
                            {
                                SecretId = $"{GetEnvironmentVariable("PREFIX", "local")}-api-certificate"
                            }
                        )
                        .GetAwaiter()
                        .GetResult()
                        .SecretString
                )!;

                listenOpts.UseHttps
                (
                    X509Certificate2.CreateFromPem(cert["certificate"], cert["privateKey"])
                );
            }
        );

        public static void Main(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults
            (
                static webBuilder => webBuilder.UseStartup<Startup>().ConfigureKestrel(UsingHttps)             
            )
            .Build()
            .Run();
    }
}
