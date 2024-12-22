using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Warehouse.Host
{
    using API.Registrations;
    using Core.Abstractions;
    using DAL.Registrations;
    using Infrastructure.Filters;
    using Infrastructure.Middlewares;
    using Infrastructure.Registrations;
    using Services;

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
                .AddControllers()
                .AddApiExplorer()  // for Swagger
                .AddDataAnnotations()  // support for System.ComponentModel.DataAnnotations
                .AddAuthorization()
                .AddJsonOptions(static options =>
                {
                    JsonSerializerOptions jsonOptions = options.JsonSerializerOptions;

                    jsonOptions.PropertyNameCaseInsensitive = true;
                    jsonOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
                    jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                    jsonOptions.ReadCommentHandling = JsonCommentHandling.Skip;
                })
                .ConfigureApiBehaviorOptions(static options =>
                {
                    options.SuppressModelStateInvalidFilter = true;  // we want to use our own ValidateModelStateFilter
                });

            services.TryAddSingleton(TimeProvider.System);
            services.TryAddSingleton<IX509CertificateFactory, X509CertificateFactory>();

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
                app.UseSwagger(configuration);
            }
        }
    }
}
