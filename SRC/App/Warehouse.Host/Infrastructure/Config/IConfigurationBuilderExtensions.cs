/********************************************************************************
* IConfigurationBuilderExtensions.cs                                            *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

namespace Warehouse.Host.Infrastructure.Config
{
    internal static class IConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds matching environment variables to the configuration. The prefix WON'T be removed.
        /// </summary>
        public static IConfigurationBuilder AddLiteralEnvironmentVariables(this IConfigurationBuilder self, string prefix)
        {
            return self.AddInMemoryCollection(FromEnvironment(prefix));

            static IEnumerable<KeyValuePair<string, string?>> FromEnvironment(string prefix)
            {
                foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
                {
                    if (de.Key is string keyStr && keyStr.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        yield return new KeyValuePair<string, string?>(keyStr, de.Value?.ToString());
                }
            }
        }
    }
}
