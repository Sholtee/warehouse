using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.OpenApi.Models;

namespace Warehouse.API
{
    using Infrastructure.Auth;
    using Infrastructure.Extensions;
    using Infrastructure.Filters;
    using Infrastructure.Middlewares;

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

            services.AddCookieAuthentication();

            services.AddEndpointsApiExplorer().AddSwaggerGen(options =>
            {
                OpenApiInfo info = new();
                configuration.GetSection("Swagger").Bind(info);

                OpenApiSecurityScheme scheme = new()
                {
                    In = ParameterLocation.Cookie,
                    Name = configuration.GetRequiredValue<string>("Auth:SessionCookieName"),
                    Type = SecuritySchemeType.ApiKey,
                    Description = "JSON Web Token in session cookie."
                };

                options.SwaggerDoc(info.Version, info);
                options.AddSecurityDefinition("session-cookie", scheme);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {scheme, []}
                });
                options.IncludeXmlComments
                (
                    Path.Combine
                    (
                        AppContext.BaseDirectory,
                        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"
                    )
                );
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
