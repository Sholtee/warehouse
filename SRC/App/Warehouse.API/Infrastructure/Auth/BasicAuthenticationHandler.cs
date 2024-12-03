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
        IPasswordHasher<string> passwordHasher,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        IAmazonSecretsManager secretsManager,
        IMemoryCache cache,
        IConfiguration configuration,
        UrlEncoder encoder
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SCHEME = "Basic";

        #region Private
        private const string PREFIX = $"{SCHEME} ";

        private sealed class BasicAuthenticationClient : IIdentity
        {
            public required string AuthenticationType { get; init; }
            public required bool IsAuthenticated { get; init; }
            public required string Name { get; init; }
        }

        private sealed class UserDescriptor
        {
            public required List<string> Groups { get; init; }
            public required string PasswordHash { get; init; }
        }

        private sealed class GroupRoles
        {
            public required List<string> Roles { get; init; }
            public List<string>? Includes { get; init; }
        }

        private HashSet<string> GetAvailableRoles(List<string> groups)
        {
            HashSet<string> roles = [];

            Dictionary<string, GroupRoles> groupRoles = [];
            configuration.GetRequiredSection("AuthenticationHanlder:GroupRoles").Bind(groupRoles);
            groups.ForEach(ExtendRoles);

            return roles;

            void ExtendRoles(string group)
            {
                if (groupRoles.TryGetValue(group, out GroupRoles? gr))
                {
                    roles.UnionWith(gr.Roles);
                    gr.Includes?.ForEach(ExtendRoles);
                }
            }
        }
        #endregion

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.GetEndpoint()?.Metadata?.GetMetadata<IAllowAnonymous>() is not null)
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
            IReadOnlyDictionary<string, UserDescriptor> users = (await cache.GetOrCreateAsync(usersKey, async entry =>
            {
                entry.AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes
                (
                    configuration.GetValue("AuthenticationHanlder:CacheExpirationMinutes", 30)
                );

                GetSecretValueResponse resp = await secretsManager.GetSecretValueAsync(new GetSecretValueRequest
                {
                    SecretId = usersKey
                });

                return (IReadOnlyDictionary<string, UserDescriptor>) JsonSerializer.Deserialize<Dictionary<string, UserDescriptor>>(resp.SecretString)!.AsReadOnly();
            }))!;

            //
            // Verify the client id - client secret pair
            //

            if (!users.TryGetValue(clientId, out UserDescriptor? userDescriptor) || passwordHasher.VerifyHashedPassword(clientId, userDescriptor.PasswordHash, clientSecret) != PasswordVerificationResult.Success)
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
                                ..GetAvailableRoles(userDescriptor.Groups).Select(static role => new Claim(ClaimTypes.Role, role))
                            ]
                        )
                    ),
                    Scheme.Name
                )
            );
        }
    }
}
