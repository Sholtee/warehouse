using Microsoft.AspNetCore.Authorization;

namespace Warehouse.API.Infrastructure.Auth
{
    internal sealed class RequiredRolesAttribute : AuthorizeAttribute
    {
        public RequiredRolesAttribute(Roles roles)
        {
            AuthenticationSchemes = CookieAuthentication.SCHEME;
            Roles = string.Join(',', Enum.GetValues<Roles>().Where(role => role > 0 && roles.HasFlag(role)));
        }
    }
}
