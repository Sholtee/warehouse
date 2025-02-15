/********************************************************************************
* OptionsExtensions.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

using Microsoft.Extensions.DependencyInjection;

namespace Warehouse.Host.Infrastructure.Registrations
{
    internal static class OptionsExtensions
    {
        public static IServiceCollection AddOptions<TOptions, TDependency>(this IServiceCollection services, Action<TOptions, TDependency> configurator) where TOptions : class where TDependency: class => services
            .AddOptions<TOptions>()
            .Configure(configurator)
            .Services;

        public static IServiceCollection AddOptions<TOptions, TDependency1, TDependency2>(this IServiceCollection services, Action<TOptions, TDependency1, TDependency2> configurator) where TOptions : class where TDependency1 : class where TDependency2 : class => services
            .AddOptions<TOptions>()
            .Configure(configurator)
            .Services;
    }
}
