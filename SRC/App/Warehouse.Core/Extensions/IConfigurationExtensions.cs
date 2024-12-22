/********************************************************************************
* IConfigurationExtensions.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

using Microsoft.Extensions.Configuration;

namespace Warehouse.Core.Extensions
{
    public static class IConfigurationExtensions
    {
        public static T GetRequiredValue<T>(this IConfiguration self, string key)
        {
            string? val = self.GetRequiredSection(key).Value;
            return (T) Convert.ChangeType(val, typeof(T), null)!;
        }
    }
}
