using Amazon.SecretsManager;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Warehouse.API.Infrastructure.Registrations
{
    using Services;

    internal static partial class Registrations
    {
        public static IServiceCollection AddDbConnection(this IServiceCollection services)
        {
            services.TryAddAWSService<IAmazonSecretsManager>();
            services.AddMemoryCache();
            services.TryAddScoped<MySqlConnectionFactory>();
            services.TryAddScoped(static serviceProvider => serviceProvider.GetRequiredService<MySqlConnectionFactory>().CreateConnection());

            return services;
        }
    }
}
