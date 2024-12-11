using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Warehouse.API.Infrastructure.Auth
{
    using Extensions;

    internal sealed class CookieAuthenticationHandler
    (
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        IJwtService jwtService,
        IConfiguration configuration,
        UrlEncoder encoder
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.GetEndpoint()?.Metadata?.GetMetadata<IAllowAnonymous>() is not null)
            {
                return AuthenticateResult.NoResult();
            }

            if (!Request.Cookies.TryGetValue(configuration.GetRequiredValue<string>("Auth:SessionCookieName"), out string? token))
            {
                return AuthenticateResult.Fail("Missing session cookie");
            }

            TokenValidationResult validationResult = await jwtService.ValidateToken(token);
            if (!validationResult.IsValid)
            {
                return AuthenticateResult.Fail(validationResult.Exception);
            }

            return AuthenticateResult.Success
            (
                new AuthenticationTicket
                (
                    new ClaimsPrincipal(validationResult.ClaimsIdentity),
                    Scheme.Name
                )
            );
        }
    }
}