/********************************************************************************
* OptionsExtensions.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Warehouse.Host.Infrastructure.Registrations
{
    internal static class OptionsExtensions
    {
        public static IServiceCollection SetOptions<TOptions>(this IServiceCollection services, Action<TOptions, IConfiguration> configurator) where TOptions: class => services
            .RemoveAll<IOptions<TOptions>>()
            .AddOptions<TOptions>()
            .Configure(configurator)
            .Services;
    }
}
