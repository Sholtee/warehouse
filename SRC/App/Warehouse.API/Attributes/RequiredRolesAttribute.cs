using System;
using System.Linq;

using Microsoft.AspNetCore.Authorization;


namespace Warehouse.API.Attributes
{
    using Infrastructure.Auth;

    internal sealed class RequiredRolesAttribute : AuthorizeAttribute
    {
        public RequiredRolesAttribute(Roles roles)
        {
            AuthenticationSchemes = SessionCookieAuthenticationHandler.SCHEME;
            Roles = string.Join(',', Enum.GetValues<Roles>().Where(role => role > 0 && roles.HasFlag(role)));
        }
    }
}
