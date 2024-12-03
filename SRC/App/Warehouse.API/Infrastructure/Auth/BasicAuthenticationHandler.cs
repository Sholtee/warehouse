using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Warehouse.API.Infrastructure.Auth
{
    public sealed class BasicAuthenticationHandler
    (
        IPasswordHasher<object> passwordHasher,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        IAmazonSecretsManager secretsManager,
        IMemoryCache cache,
        IConfiguration configuration,
        UrlEncoder encoder
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SCHEME = "Basic";

        private const string PREFIX = $"{SCHEME} ";

        private sealed class BasicAuthenticationClient : IIdentity
        {
            public required string AuthenticationType { get; init; }
            public required bool IsAuthenticated { get; init; }
            public required string Name { get; init; }
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Endpoint? endpoint = Context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() is not null)
            {
                return AuthenticateResult.NoResult();
            }

            if (!Request.Headers.TryGetValue("Authorization", out StringValues value))
            {
                return AuthenticateResult.Fail("Missing Authorization header");
            }

            string authorizationHeader = value.ToString();

            if (!authorizationHeader.StartsWith(PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.Fail("Authorization header does not start with 'Basic'");
            }

            //
            // B64 string is always longer than the original
            //

            Span<byte> rawData = new byte[(authorizationHeader.Length - PREFIX.Length) * sizeof(char)];

            if (!Convert.TryFromBase64String(authorizationHeader[PREFIX.Length..], rawData, out int bytesWritten))
            {
                return AuthenticateResult.Fail("Invalid Base64 string");
            }

            if (Encoding.UTF8.GetString(rawData.Slice(0, bytesWritten)).Split(':', 2) is not [string clientId, string clientSecret])
            {
                return AuthenticateResult.Fail("Invalid Authorization header format");
            }

            //
            // Grab the available user list (preferably from cache)
            //

            string usersKey = $"{configuration.GetValue("Prefix", "local")}-api-users";
            IReadOnlyDictionary<string, string> users = (await cache.GetOrCreateAsync<IReadOnlyDictionary<string, string>>(usersKey, async entry =>
            {
                entry.AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes
                (
                    configuration.GetValue("AuthenticationHanlder:CacheExpirationMinutes", 30)
                );

                GetSecretValueResponse resp = await secretsManager.GetSecretValueAsync(new GetSecretValueRequest
                {
                    SecretId = usersKey
                });

                return JsonSerializer.Deserialize<Dictionary<string, string>>(resp.SecretString)!.AsReadOnly();
            }))!;

            //
            // Verify the client id - client secret pair
            //

            if (!users.TryGetValue(clientId, out string? secretHash) || passwordHasher.VerifyHashedPassword(null!, secretHash, clientSecret) != PasswordVerificationResult.Success)
            {
                return AuthenticateResult.Fail(string.Format("The secret is incorrect for the client '{0}'", clientId));
            }

            return AuthenticateResult.Success
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

                                //
                                // TODO: get the assignable roles from SSM
                                //

                                new Claim(ClaimTypes.Role, Roles.User.ToString()),
                                new Claim(ClaimTypes.Role, Roles.Admin.ToString())
                            ]
                        )
                    ),
                    Scheme.Name
                )
            );
        }
    }
}
