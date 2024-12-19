using System;
using System.IO;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Warehouse.API.Infrastructure.Registrations
{
    using Extensions;
    using Infrastructure.Filters;

    internal static class Swagger
    {
        public static IServiceCollection AddSwagger(this IServiceCollection services, IConfiguration configuration) => services.AddEndpointsApiExplorer().AddSwaggerGen(options =>
        {
            OpenApiInfo info = new();
            configuration.GetRequiredSection("Swagger").Bind(info);

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

            options.IncludeXmlComments
            (
                Path.Combine
                (
                    AppContext.BaseDirectory,
                    $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"
                )
            );

            options.OperationFilter<AuthorizationOperationFilter>();
            options.DocumentFilter<CustomModelDocumentFilter<ErrorDetails>>();
        });
    }
}
