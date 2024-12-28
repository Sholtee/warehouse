/********************************************************************************
* Program.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Net;

using Amazon;
using Microsoft.AspNetCore.Hosting;
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
    using Services;

    internal sealed class Program
    {
        public static void Main(string[] args) => new HostBuilder()
            .ConfigureHostConfiguration
            (
                configBuilder => configBuilder
                    .AddCommandLine(args)
                    .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                    .AddEnvironmentVariables(prefix: "AWS_")
                    .AddEnvironmentVariables(prefix: "WAREHOUSE_")
            )
            .ConfigureAppConfiguration
            (
                static (context, configBuilder) => configBuilder
                    .SetBasePath(Environment.CurrentDirectory)
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
            )
            .ConfigureLogging
            (
                static (context, loggerBuilder) =>
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
                }
            )
            .ConfigureWebHostDefaults
            (
                static webBuilder => webBuilder.UseStartup<Startup>().ConfigureKestrel
                (
                    static (context, serverOpts) => serverOpts.Listen
                    (
                        IPAddress.Any,
                        context.Configuration.GetValue("WAREHOUSE_SERVICE_PORT", 1986),
                        listenOpts => listenOpts.UseHttps
                        (
                            listenOpts
                                .ApplicationServices
                                .GetRequiredService<CertificateStore>()
                                .GetCertificate("warehouse-app-cert")
                        )
                    )
                )    
            )
            .Build()
            .Run();
    }
}
