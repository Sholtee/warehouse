
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Warehouse.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services
                .AddSingleton<IPasswordHasher<object>>(_ => new PasswordHasher<object>())
                .AddAuthentication().AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationHandler.SCHEME, null);

            using WebApplication app = builder.Build();

            app.UseHttpsRedirection();

            app.MapControllers();

            app.Run();
        }
    }
}
