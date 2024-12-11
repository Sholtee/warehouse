using Microsoft.AspNetCore.Authorization;

namespace Warehouse.API.Infrastructure.Attributes
{
    using Auth;

    internal sealed class RequiredRolesAttribute : AuthorizeAttribute
    {
        public RequiredRolesAttribute(Roles roles)
        {
            AuthenticationSchemes = "session-cookie";
            Roles = string.Join(',', Enum.GetValues<Roles>().Where(role => role > 0 && roles.HasFlag(role)));
        }
    }
}
