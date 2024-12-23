/********************************************************************************
* Program.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.IO;
using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;


namespace Warehouse.Tests.Host
{
    public sealed class Program
    {
        public static void Main(string[] args) => new HostBuilder()
           .ConfigureDefaults(args)
           .ConfigureHostConfiguration(static configBuilder =>
           {
               configBuilder.AddJsonFile("appsettings.json");
               configBuilder.AddJsonFile("appsettings.local.json");

               Stream stm = new MemoryStream();
               JsonSerializer.Serialize(stm, new { ASPNETCORE_ENVIRONMENT = "local" });
               stm.Position = 0;

               configBuilder.AddJsonStream(stm);  // will close the input stream
           })
           .ConfigureWebHostDefaults(static webBuilder => webBuilder.UseStartup<Startup>())
           .Build()
           .Run();
    }
}
