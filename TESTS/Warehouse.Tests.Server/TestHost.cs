using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Warehouse.Tests.Server
{
    public sealed class TestHost
    {
        public static void Main(string[] args) => Host
           .CreateDefaultBuilder(args)
           .ConfigureWebHostDefaults(static webBuilder => webBuilder.UseStartup<Startup>())
           .Build()
           .Run();
    }
}
