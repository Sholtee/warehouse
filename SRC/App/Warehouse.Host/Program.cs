/********************************************************************************
* Program.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;

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
    using Infrastructure.Config;

    internal sealed class Program
    {
        public static void Main(string[] args) => new HostBuilder()
            .ConfigureHostConfiguration
            (
                configBuilder => configBuilder
                    .AddCommandLine(args)
                    .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                    .AddLiteralEnvironmentVariables(prefix: "WAREHOUSE_")
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
                    static (context, serverOpts) =>
                    {
                        serverOpts.AddServerHeader = false;
                        serverOpts.Listen
                        (
                            IPAddress.Any,
                            context.Configuration.GetValue("WAREHOUSE_SERVICE_PORT", 1986),
                            static listenOpts => listenOpts.UseHttps
                            (
                                listenOpts
                                    .ApplicationServices
                                    .GetRequiredKeyedService<X509Certificate2>("warehouse-app-cert")
                            )
                        );
                    }
                )    
            )
            .Build()
            .Run();
    }
}
