/********************************************************************************
* AuthenticationHandler.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Warehouse.Host.Infrastructure.Auth
{
    using Core.Abstractions;
    using Core.Extensions;

    internal sealed class AuthenticationHandler
    (
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        IConfiguration configuration,
        ILoggerFactory logger,
        ISessionManager session,
        ITokenManager tokenManager,
        UrlEncoder encoder
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.GetEndpoint()?.Metadata?.GetMetadata<IAllowAnonymous>() is not null)
            {
                return AuthenticateResult.NoResult();
            }

            if (string.IsNullOrEmpty(session.Token))
            {
                return AuthenticateResult.Fail("Missing session token");
            }

            ClaimsIdentity? identity = await tokenManager.GetIdentityAsync(session.Token);
            if (identity is null)
            {
                return AuthenticateResult.Fail("Failed to get the identity from the token");
            }

            if (configuration.GetRequiredValue<bool>("Auth:SlidingExpiration"))
            {
                session.Token = await tokenManager.RefreshTokenAsync(session.Token);
            }

            return AuthenticateResult.Success
            (
                new AuthenticationTicket
                (
                    new ClaimsPrincipal(identity),
                    Scheme.Name
                )
            );
        }
    }
}
