using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

using Amazon.SecretsManager.Model;
using Amazon.SecretsManager;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using ServiceStack.Logging.Serilog;
using ServiceStack.OrmLite;

namespace Warehouse.API
{
    using Extensions;
    using Services;

    internal class Program
    {
        internal static void UsingHttps(WebHostBuilderContext context, KestrelServerOptions serverOpts) => serverOpts.Listen
        (
            IPAddress.Any,
            context.Configuration.GetRequiredValue<int>("ApiPort"),
            static listenOpts =>
            {
                IServiceProvider serviceProvider = listenOpts.ApplicationServices;

                Dictionary<string, string> cert = JsonSerializer.Deserialize<Dictionary<string, string>>
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
                        .SecretString
                )!;

                listenOpts.UseHttps
                (
                    serviceProvider
                        .GetRequiredService<IX509CertificateFactory>()
                        .CreateFromPem(cert["certificate"], cert["privateKey"])
                );
            }
        );

        public static void Main(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureLogging(static (context, loggerConfiguration) =>
            {
                loggerConfiguration.ClearProviders();

                Serilog.ILogger logger = new LoggerConfiguration()
                    .ReadFrom
                    .Configuration(context.Configuration)
                    .CreateLogger();

                loggerConfiguration.AddSerilog(logger);
                OrmLiteConfig.ResetLogFactory(new SerilogFactory(logger));
            })
            .ConfigureWebHostDefaults
            (
                static webBuilder => webBuilder.UseStartup<Startup>().ConfigureKestrel(UsingHttps)    
            )
            .Build()
            .Run();
    }
}
