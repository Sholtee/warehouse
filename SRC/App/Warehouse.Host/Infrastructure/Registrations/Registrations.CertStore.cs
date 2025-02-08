/********************************************************************************
* Registrations.CertStore.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Security.Cryptography.X509Certificates;

using Amazon.SecretsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Warehouse.Host.Infrastructure.Registrations
{

    using Services;

    internal static partial class Registrations
    {
        public static IServiceCollection AddCertificateStore(this IServiceCollection services)
        {
            services.TryAddAWSService<IAmazonSecretsManager>();
            services.TryAddSingleton<CertificateStore>();
            services.TryAddKeyedSingleton<X509Certificate2>("warehouse-app-cert", CertificateStore.GetCertificate);

            return services;
        }
    }
}
