/********************************************************************************
* Startup.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Warehouse.Host
{
    using API.Registrations;
    using DAL.Registrations;
    using Infrastructure.Filters;
    using Infrastructure.Middlewares;
    using Infrastructure.Registrations;

    internal sealed class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvcCore(static options =>
                {
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

            services
                .AddExceptionHandler<UnhandledExceptionHandler>()
                .AddAwsServices()
                .AddCertificateStore()
                .AddDbConnection()
                .AddRepositories()
                .AddRootUserRegistrar()
                .AddStatefulAuthentication()
                .AddSwagger()
                .AddRateLimiter()
                .AddHealthCheck()
                .AddProfiler();
        }

        public static void Configure(IApplicationBuilder app)
        {         
            app.AddRootUser();

            app
                .UseHttpsRedirection()
                .UseExceptionHandler(static _ => { })
                .UseProfiling()
                .UseRouting()
                .UseAuthorization()
                .UseRateLimiter()
                .UseMiddleware<LoggingMiddleware>()
                .UseHealthCheck()
                .UseSwagger()
                .UseEndpoints(static endpoints => endpoints.MapControllers());
        }
    }
}
