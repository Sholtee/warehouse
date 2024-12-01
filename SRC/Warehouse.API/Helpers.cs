namespace Warehouse.API
{
    internal static class Helpers
    {
        public static T? GetEnvironmentVariable<T>(string variable, T @default)
        {
            string? val = Environment.GetEnvironmentVariable(variable);

            return val is null
                ? @default
                : (T) Convert.ChangeType(val, typeof(T));
        }
    }
}
