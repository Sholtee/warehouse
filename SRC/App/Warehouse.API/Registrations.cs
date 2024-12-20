using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

namespace Warehouse.API.Registrations
{
    public static class Registrations
    {
        public static IMvcCoreBuilder AddControllers(this IMvcCoreBuilder mvcBuilder) => mvcBuilder
            .AddApplicationPart(Assembly.GetExecutingAssembly())
            .AddControllersAsServices();
    }
}
