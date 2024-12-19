using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Warehouse.API
{
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
                .AddApiExplorer()  // for Swagger
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

            services.TryAddSingleton(TimeProvider.System);

            services.AddSessionCookieAuthentication();
            services.AddDbConnection();
            services.AddRepositories();
            services.AddRootUserRegistrar();
            services.AddSwagger(configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {         
            app.UseHttpsRedirection();

            app.AddRootUser();

            app
                .UseRouting()
                .UseAuthorization()
                .UseMiddleware<LoggingMiddleware>()
                .UseEndpoints(static endpoints => endpoints.MapControllers());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                app.UseSwagger().UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", configuration["Swagger:Version"]);
                    options.RoutePrefix = string.Empty;
                });
            }
        }
    }
}
