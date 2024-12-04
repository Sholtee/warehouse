using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Amazon.SecretsManager;
using Microsoft.OpenApi.Models;

namespace Warehouse.API
{
    using Infrastructure.Auth;
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
                });

            services.AddBasicAuth();

            //
            // All AmazonServiceClient objects are thread safe
            // 

            services.AddAWSService<IAmazonSecretsManager>();

            services.AddEndpointsApiExplorer().AddSwaggerGen(options =>
            {
                OpenApiInfo info = new();
                configuration.GetSection("Swagger").Bind(info);

                options.SwaggerDoc(info.Version, info);
                options.AddSecurityDefinition(BasicAuth.Scheme.Scheme, BasicAuth.Scheme);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {BasicAuth.Scheme, []}
                });
                options.IncludeXmlComments
                (
                    Path.Combine
                    (
                        AppContext.BaseDirectory,
                        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"
                    )
                );
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
