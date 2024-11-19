using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Warehouse.API
{
    using Auth;

    public class Startup(IConfiguration configuration)
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();  
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationHandler.SCHEME, null);

            services.AddScoped<IPasswordHasher<object>>(static _ => new PasswordHasher<object>());
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHttpsRedirection();

            app.UseRouting().UseAuthorization().UseEndpoints(static endpoints => endpoints.MapControllers());
        }
    }
}
