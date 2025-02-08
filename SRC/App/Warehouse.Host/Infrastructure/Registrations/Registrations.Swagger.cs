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

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using Core.Extensions;
    using Dtos;
    using Filters;

    internal static partial class Registrations
    {
        public static IServiceCollection AddSwagger(this IServiceCollection services, IConfiguration configuration)
        {
            IConfigurationSection swaggerConfig = configuration.GetSection("Swagger");
            if (!swaggerConfig.Exists())
                return services;

            return services.AddEndpointsApiExplorer().AddSwaggerGen(options =>
            {
                OpenApiInfo info = new();
                swaggerConfig.Bind(info);

                options.SwaggerDoc(info.Version, info);

                options.AddSecurityDefinition("session-cookie", new OpenApiSecurityScheme()
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

                options.AddSecurityDefinition("basic", new OpenApiSecurityScheme()
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

                foreach (string docFile in Directory.EnumerateFiles(AppContext.BaseDirectory, "Warehouse.*.xml", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = false }))
                {
                    //
                    // Ensure we have an XML doc file
                    //

                    if (File.ReadLines(docFile).Skip(1).Take(1).SingleOrDefault()?.Equals("<doc>", StringComparison.OrdinalIgnoreCase) is true)
                    {
                        options.IncludeXmlComments(docFile);
                    }
                }

                options.OperationFilter<AuthorizationOperationFilter>();
                options.DocumentFilter<CustomModelDocumentFilter<ErrorDetails>>();
                options.DocumentFilter<CustomModelDocumentFilter<HealthCheckResult>>();
                options.ExampleFilters();
            });
        }

        public static IApplicationBuilder UseSwagger(this IApplicationBuilder applicationBuilder)
        {
            IConfigurationSection swaggerConfig = applicationBuilder
                .ApplicationServices
                .GetRequiredService<IConfigurationSection>()
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
