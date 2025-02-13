/********************************************************************************
* SessionCookieAuthenticationHandler.cs                                         *
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Warehouse.Host.Infrastructure.Auth
{
    using Core.Abstractions;

    internal sealed class SessionCookieAuthenticationHandler
    (
        IOptionsMonitor<AuthenticationSchemeOptions> options,
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
                return AuthenticateResult.Fail("Missing session cookie");
            }

            TokenValidationResult validationResult = await tokenManager.ValidateTokenAsync(session.Token);
            if (!validationResult.IsValid)
            {
                return AuthenticateResult.Fail(validationResult.Exception);
            }

            if (session.SlidingExpiration)
            {
                session.Token = await tokenManager.RefreshTokenAsync(session.Token);
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
