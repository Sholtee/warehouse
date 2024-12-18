using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Warehouse.Tests.Server
{
    public sealed class Startup
    {
        public void ConfigureServices(IServiceCollection services) => services.AddRouting();

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) => app  
            .UseRouting()
            .UseAuthorization()
            .UseEndpoints(static endpoints => endpoints.MapControllers());
    }
}
