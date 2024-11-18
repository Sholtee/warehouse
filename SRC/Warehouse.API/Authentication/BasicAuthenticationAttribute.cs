using Microsoft.AspNetCore.Authorization;

namespace Warehouse.API
{
    public sealed class BasicAuthenticationAttribute : AuthorizeAttribute
    {
        public BasicAuthenticationAttribute()
        {
            AuthenticationSchemes = BasicAuthenticationHandler.SCHEME;
        }
    }
}
