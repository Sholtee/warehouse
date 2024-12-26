/********************************************************************************
* Registrations.CertStore.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using Services;

    internal static partial class Registrations
    {
        public static IServiceCollection AddCertificateStore(this IServiceCollection services)
        {
            services.AddAwsServices();
            services.TryAddSingleton<CertificateStore>();

            return services;
        }
    }
}
