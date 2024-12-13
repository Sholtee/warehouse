namespace Warehouse.API.Extensions
{
    internal static class IConfigurationExtensions
    {
        public static T GetRequiredValue<T>(this IConfiguration self, string key)
        {
            string val = self[key] ?? throw new InvalidOperationException("Required configuration value not found");
            return (T)Convert.ChangeType(val, typeof(T));
        }
    }
}
