/********************************************************************************
* Registrations.Swagger.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using Core.Extensions;
    using Dtos;
    using Filters;

    internal static partial class Registrations
    {
        /// <summary>
        /// Set ups swagger for the application. Should be called after MVC setup
        /// </summary>
        public static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            string asmPrefix = Assembly.GetExecutingAssembly().GetName().Name!.Split('.', 2)[0];

            Assembly[] appAssemblies = 
            [ 
                ..AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(asm => asm.GetName().Name?.StartsWith(asmPrefix, StringComparison.OrdinalIgnoreCase) is true)
            ];

            return services
                .AddEndpointsApiExplorer()
                .AddSwaggerGen()
                .SetOptions<SwaggerGenOptions>((options, configuration) =>
                {
                    IConfigurationSection swaggerConfig = configuration.GetSection("Swagger");
                    if (!swaggerConfig.Exists())
                        return;

                    OpenApiInfo info = new();
                    swaggerConfig.Bind(info);

                    options.SwaggerDoc(info.Version, info);

                    options.AddSecurityDefinition("session-cookie", new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Cookie,
                        Name = configuration.GetRequiredValue<string>("Auth:SessionCookieName"),
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "session-cookie",
                        Description = "JSON Web Token in session cookie.",
                        Reference = new OpenApiReference
                        {
                            Id = "session-cookie",
                            Type = ReferenceType.SecurityScheme
                        }
                    });

                    options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "basic",
                        Description = "Basic authentication",
                        Reference = new OpenApiReference
                        {
                            Id = "basic",
                            Type = ReferenceType.SecurityScheme
                        }
                    });

                    foreach (string docFile in appAssemblies.Select(static asm => Path.ChangeExtension(asm.Location, "xml")).Where(File.Exists))
                    {
                        options.IncludeXmlComments(docFile);
                    }

                    options.OperationFilter<AuthorizationOperationFilter>();
                    options.DocumentFilter<CustomModelDocumentFilter<ErrorDetails>>();
                    options.DocumentFilter<CustomModelDocumentFilter<HealthCheckResult>>();
                    options.ExampleFilters();
                })

                //
                // Should not be called multiple times as it uses AddSingleton() internally instead 
                // of TryAddSingleton()
                //

                .AddSwaggerExamplesFromAssemblies(appAssemblies);;
        }

        public static IApplicationBuilder UseSwagger(this IApplicationBuilder applicationBuilder)
        {
            IConfigurationSection swaggerConfig = applicationBuilder
                .ApplicationServices
                .GetRequiredService<IConfiguration>()
                .GetSection("Swagger");
            if (!swaggerConfig.Exists())
                return applicationBuilder;

            return SwaggerBuilderExtensions.UseSwagger(applicationBuilder).UseSwaggerUI(options =>
            {
                string version = swaggerConfig.GetRequiredValue<string>("Version");

                options.SwaggerEndpoint($"/swagger/{version}/swagger.json", version);
                options.RoutePrefix = string.Empty;
            });
        }
    }
}
