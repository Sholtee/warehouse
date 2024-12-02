using System.Text.Json;
using System.Text.Json.Serialization;

using Amazon.SecretsManager;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Warehouse.API
{
    using Infrastructure.Auth;
    using Infrastructure.Filters;

    public class Startup(IConfiguration configuration)
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvcCore(static options =>
                {
                    options.Filters.Add<UnhandledExceptionFilter>();
                    options.Filters.Add<ValidateModelStateFilter>();
                })
                .AddDataAnnotations()  // support for System.ComponentModel.DataAnnotations
                .AddAuthorization()
                .AddJsonOptions(static options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

            services
                .AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationHandler.SCHEME, null);

            services
                .AddScoped<IPasswordHasher<object>>(static _ => new PasswordHasher<object>())
                .AddMemoryCache()

                //
                // All AmazonServiceClient objects are thread safe
                // 

                .AddAWSService<IAmazonSecretsManager>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {         
            app.UseHttpsRedirection();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting().UseAuthorization().UseEndpoints(static endpoints => endpoints.MapControllers());
        }
    }
}
