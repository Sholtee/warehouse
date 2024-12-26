/********************************************************************************
* IHostEnvironmentExtensions.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

using Microsoft.Extensions.Hosting;

namespace Warehouse.Core.Extensions
{
    public static class IHostEnvironmentExtensions
    {
        private static bool Is(this IHostEnvironment environment, string env)
        {
            ArgumentNullException.ThrowIfNull(environment, nameof(environment));
            ArgumentNullException.ThrowIfNull(env, nameof(env));

            return environment.IsEnvironment(env);
        }

        public static bool IsLocal(this IHostEnvironment environment) => environment.Is("local");

        public static bool IsDev(this IHostEnvironment environment) => environment.Is("dev");
    }
}
