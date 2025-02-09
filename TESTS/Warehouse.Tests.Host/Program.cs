/********************************************************************************
* Program.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;


namespace Warehouse.Tests.Host
{
    public sealed class Program
    {
        public static void Main() => new HostBuilder()
           .ConfigureHostConfiguration
           (
               static configBuilder => configBuilder.AddJsonFile("appsettings.json")
           )
           .ConfigureWebHostDefaults
           (
               static webBuilder => webBuilder.UseEnvironment("local").UseStartup<Startup>()
           )
           .Build()
           .Run();
    }
}
