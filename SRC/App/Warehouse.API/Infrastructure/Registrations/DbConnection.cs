using Amazon.SecretsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceStack.OrmLite;


namespace Warehouse.API.Infrastructure.Registrations
{
    using Services;

    internal static partial class Registrations
    {
        public static IServiceCollection AddDbConnection(this IServiceCollection services)
        {
            services.TryAddAWSService<IAmazonSecretsManager>();
            services.AddMemoryCache();
            services.TryAddSingleton<MySqlConnectionFactory>();
            services.TryAddScoped(static serviceProvider => serviceProvider.GetRequiredService<MySqlConnectionFactory>().CreateConnection());

            OrmLiteConfig.DialectProvider = MySqlDialect.Provider;
            services.TryAddSingleton<IOrmLiteDialectProvider>(MySqlDialect.Provider);

            return services;
        }
    }
}
