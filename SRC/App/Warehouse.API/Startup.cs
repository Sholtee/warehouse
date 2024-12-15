using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.OpenApi.Models;

namespace Warehouse.API
{
    using Extensions;

    using Infrastructure.Filters;
    using Infrastructure.Middlewares;
    using Infrastructure.Registrations;

    internal sealed class Startup(IConfiguration configuration)
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvcCore(static options =>
                {
                    options.Filters.Add<UnhandledExceptionFilter>();
                    options.Filters.Add<ValidateModelStateFilter>();
                })
                .AddApiExplorer()  // for swagger
                .AddDataAnnotations()  // support for System.ComponentModel.DataAnnotations
                .AddAuthorization()
                .AddJsonOptions(static options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                })
                .ConfigureApiBehaviorOptions(static options =>
                {
                    options.SuppressModelStateInvalidFilter = true;  // we want to use our own ValidateModelStateFilter
                });

            services.AddSessionCookieAuthentication();
            services.AddDbConnection();
            services.AddRepositories();

            services.AddEndpointsApiExplorer().AddSwaggerGen(options =>
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

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {         
            app.UseHttpsRedirection();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting().UseAuthorization().UseMiddleware<LoggingMiddleware>().UseEndpoints(static endpoints => endpoints.MapControllers());

            app.UseSwagger().UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", configuration["Swagger:Version"]);
                options.RoutePrefix = string.Empty;
            });
        }
    }
}
