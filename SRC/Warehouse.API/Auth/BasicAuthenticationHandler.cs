using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Text.Encodings.Web;

namespace Warehouse.API.Auth
{
    public sealed class BasicAuthenticationHandler(IPasswordHasher<object> passwordHasher, IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SCHEME = "Basic";

        private const string PREFIX = $"{SCHEME} ";

        private sealed class BasicAuthenticationClient : IIdentity
        {
            public required string AuthenticationType { get; init; }
            public required bool IsAuthenticated { get; init; }
            public required string Name { get; init; }
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Endpoint? endpoint = Context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() is not null)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!Request.Headers.TryGetValue("Authorization", out StringValues value))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));
            }

            string authorizationHeader = value.ToString();

            if (!authorizationHeader.StartsWith(PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.Fail("Authorization header does not start with 'Basic'"));
            }

            //
            // B64 string is always longer than the original
            //

            Span<byte> rawData = new byte[(authorizationHeader.Length - PREFIX.Length) * sizeof(char)];

            if (!Convert.TryFromBase64String(authorizationHeader[PREFIX.Length..], rawData, out int bytesWritten))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Base64 string"));
            }

            if (Encoding.UTF8.GetString(rawData.Slice(0, bytesWritten)).Split(':', 2) is not [string clientId, string clientSecret])
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header format"));
            }

            if (clientId != "test" || passwordHasher.VerifyHashedPassword(null!, passwordHasher.HashPassword(null!, "test"), clientSecret) != PasswordVerificationResult.Success)
            {
                return Task.FromResult(AuthenticateResult.Fail(string.Format("The secret is incorrect for the client '{0}'", clientId)));
            }

            return Task.FromResult
            (
                AuthenticateResult.Success
                (
                    new AuthenticationTicket
                    (
                        new ClaimsPrincipal
                        (
                            new ClaimsIdentity
                            (
                                new BasicAuthenticationClient
                                {
                                    AuthenticationType = SCHEME,
                                    IsAuthenticated = true,
                                    Name = clientId
                                }, 
                                [
                                    new Claim(ClaimTypes.Name, clientId),
                                    new Claim(ClaimTypes.Role, Roles.User.ToString()),
                                    new Claim(ClaimTypes.Role, Roles.Admin.ToString())
                                ]
                            )
                        ),
                        Scheme.Name
                    )
                )
            );
        }
    }
}
