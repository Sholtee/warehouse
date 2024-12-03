namespace Warehouse.API
{
    internal static class IConfigurationExtensions
    {
        public static T Get<T>(this IConfiguration self, string key, T @default)
        {
            string? val = self[key];

            return val is null
                ? @default
                : (T) Convert.ChangeType(val, typeof(T));
        }
    }
}
