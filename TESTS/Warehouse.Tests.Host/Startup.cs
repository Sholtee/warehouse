/********************************************************************************
* Startup.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Warehouse.Tests.Host
{
    public sealed class Startup
    {
        public void ConfigureServices(IServiceCollection services) => services.AddRouting();

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app
                .UseRouting()
                .UseEndpoints(static endpoints => endpoints.MapControllers());
        }
    }
}
