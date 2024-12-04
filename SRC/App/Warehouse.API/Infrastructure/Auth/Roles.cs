namespace Warehouse.API.Infrastructure.Auth
{
    [Flags]
    internal enum Roles
    {
        None = 0,
        User = 1 << 0,
        Admin = 1 << 1
    }
}
