using Microsoft.AspNetCore.Authorization;

namespace Warehouse.API.Infrastructure.Auth
{
    public sealed class BasicAuthorizeAttribute : AuthorizeAttribute
    {
        public BasicAuthorizeAttribute(Roles roles)
        {
            AuthenticationSchemes = BasicAuthenticationHandler.SCHEME;
            Roles = string.Join(',', Enum.GetValues<Roles>().Where(role => role > 0 && roles.HasFlag(role)));
        }
    }
}
