/********************************************************************************
* Program.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Net;
using System.Text.Json;

using Amazon;
using Amazon.SecretsManager.Model;
using Amazon.SecretsManager;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using ServiceStack.Logging.Serilog;
using ServiceStack.OrmLite;

namespace Warehouse.Host
{
    using Core.Abstractions;
    using Core.Extensions;

    internal class Program
    {
        private sealed record CertificateSecret(string Certificate, string PrivateKey);

        internal static void UsingHttps(WebHostBuilderContext context, KestrelServerOptions serverOpts) => serverOpts.Listen
        (
            IPAddress.Any,
            context.Configuration.GetRequiredValue<int>("ApiPort"),
            static listenOpts =>
            {
                IServiceProvider serviceProvider = listenOpts.ApplicationServices;

                CertificateSecret cert = JsonSerializer.Deserialize<CertificateSecret>
                (
                    serviceProvider
                        .GetRequiredService<IAmazonSecretsManager>()
                        .GetSecretValueAsync
                        (
                            new GetSecretValueRequest
                            {
                                SecretId = $"{serviceProvider.GetRequiredService<IConfiguration>().GetRequiredValue<string>("Prefix")}-api-certificate"
                            }
                        )
                        .GetAwaiter()
                        .GetResult()
                        .SecretString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                )!;

                listenOpts.UseHttps
                (
                    serviceProvider
                        .GetRequiredService<IX509CertificateFactory>()
                        .CreateFromPem(cert.Certificate, cert.PrivateKey)
                );
            }
        );

        public static void Main(string[] args) => new HostBuilder()
            .ConfigureDefaults(args)
            .ConfigureLogging(static (context, loggerBuilder) =>
            {
                loggerBuilder.ClearProviders();

                Serilog.ILogger logger = new LoggerConfiguration()
                    .ReadFrom
                    .Configuration(context.Configuration)
                    .CreateLogger();

                loggerBuilder.AddSerilog(logger);
                OrmLiteConfig.ResetLogFactory(new SerilogFactory(logger));

                if (logger.IsEnabled(LogEventLevel.Debug))
                {
                    AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;
                }
            })
            .ConfigureWebHostDefaults
            (
                static webBuilder => webBuilder.UseStartup<Startup>().ConfigureKestrel(UsingHttps)    
            )
            .Build()
            .Run();
    }
}
