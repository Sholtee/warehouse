using System;
using System.Linq;

using Microsoft.AspNetCore.Authorization;


namespace Warehouse.Core.Attributes
{
    using Auth;

    public sealed class RequiredRolesAttribute : AuthorizeAttribute
    {
        public RequiredRolesAttribute(Roles roles)
        {
            AuthenticationSchemes = Authentication.SCHEME;
            Roles = string.Join(',', Enum.GetValues<Roles>().Where(role => role > 0 && roles.HasFlag(role)));
        }
    }
}
