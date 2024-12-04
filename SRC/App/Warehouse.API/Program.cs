using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

using Amazon.SecretsManager.Model;
using Amazon.SecretsManager;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;

namespace Warehouse.API
{
    internal static class Program
    {
        private static void UsingHttps(WebHostBuilderContext context, KestrelServerOptions serverOpts) => serverOpts.Listen
        (
            IPAddress.Any,
            context.Configuration.GetValue("ApiPort", 3000),
            listenOpts =>
            {
                Dictionary<string, string> cert = JsonSerializer.Deserialize<Dictionary<string, string>>
                (
                    listenOpts
                        .ApplicationServices
                        .GetRequiredService<IAmazonSecretsManager>()
                        .GetSecretValueAsync
                        (
                            new GetSecretValueRequest
                            {
                                SecretId = $"{context.Configuration.GetValue("Prefix", "local")}-api-certificate"
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
            .ConfigureLogging(static (context, loggerConfiguration) =>
            {
                loggerConfiguration.ClearProviders();
                loggerConfiguration.AddSerilog
                (
                    new LoggerConfiguration()
                        .ReadFrom.Configuration(context.Configuration)
                        .CreateLogger()
                );
            })
            .ConfigureWebHostDefaults
            (
                static webBuilder => webBuilder.UseStartup<Startup>().ConfigureKestrel(UsingHttps)    
            )
            .Build()
            .Run();
    }
}
