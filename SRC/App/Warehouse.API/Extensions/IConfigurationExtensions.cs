using System;

using Microsoft.Extensions.Configuration;

namespace Warehouse.API.Extensions
{
    internal static class IConfigurationExtensions
    {
        public static T GetRequiredValue<T>(this IConfiguration self, string key)
        {
            string? val = self.GetRequiredSection(key).Value;
            return (T) Convert.ChangeType(val, typeof(T))!;
        }
    }
}
