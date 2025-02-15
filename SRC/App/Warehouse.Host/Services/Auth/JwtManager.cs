/********************************************************************************
* JwtManager.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Warehouse.Host.Services
{
    using Core.Abstractions;
    using Core.Auth;
    using Core.Extensions;

    internal sealed class JwtManager
    (
        IMemoryCache cache,
        IHostEnvironment env,
        IConfiguration configuration,
        IAmazonSecretsManager secretsManager,
        ILogger<JwtManager> logger,
        TimeProvider timeProvider
    ) : ITokenManager
    {
        private readonly JwtSecurityTokenHandler _handler = new();

        private readonly string 
            _domain = $"{env.EnvironmentName}-warehouse-app",
            _algorithm = configuration.GetValue($"{nameof(JwtManager)}:Algorithm", SecurityAlgorithms.HmacSha256);

        private Task<SymmetricSecurityKey?> SecurityKey => cache.GetOrCreateAsync("jwt-secret-key", async entry =>
        {
            entry.AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes
            (
                configuration.GetValue($"{nameof(JwtManager)}:CacheExpirationMinutes", 30)
            );

            GetSecretValueResponse resp = await secretsManager.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = $"{env.EnvironmentName}-warehouse-jwt-secret-key"
            });

            return new SymmetricSecurityKey
            (
                Encoding.UTF8.GetBytes(resp.SecretString)
            );
        });

        private async Task<string> CreateTokenAsync(IEnumerable<Claim> claims)
        {
            string token = _handler.WriteToken
            (
                new JwtSecurityToken
                (
                    issuer: _domain,
                    audience: _domain,
                    claims: claims,
                    expires: timeProvider.GetUtcNow().AddMinutes  // overrides the "exp" claim if it is present in "claims"
                    (
                        configuration.GetValue("Auth:SessionExpirationMinutes", 1440)
                    ).DateTime,
                    signingCredentials: new SigningCredentials(await SecurityKey, _algorithm)
                )
            );

            logger.LogInformation("Token created for user: {user}", claims.Single(static c => c.Type == ClaimTypes.Name).Value);
            return token;
        }

        public Task<string> CreateTokenAsync(string user, Roles roles) => CreateTokenAsync
        (
            [
                new Claim(ClaimTypes.Name, user),
                ..roles.SetFlags().Select(static role => new Claim(ClaimTypes.Role, role.ToString()))
            ]
        );

        public async Task<ClaimsIdentity?> GetIdentityAsync(string token)
        {
            TokenValidationResult result = await _handler.ValidateTokenAsync
            (
                token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = _domain,
                    ValidAudience = _domain,
                    IssuerSigningKey = await SecurityKey,
                    ValidAlgorithms = [_algorithm]
                }
            );

            if (!result.IsValid)
            {
                logger.LogInformation("Token validation failed: {reason}", result.Exception?.ToString());
                return null;
            }

            logger.LogInformation("Token validation completed for user: {user}", result.Claims[ClaimTypes.Name]);
            return result.ClaimsIdentity;
        }

        public async Task<string> RefreshTokenAsync(string token)
        {
            ClaimsIdentity? identity = await GetIdentityAsync(token);
            if (identity is null)
                throw new ArgumentException("The given token is not valid", nameof(token));

            return await CreateTokenAsync(identity.Claims);
        }

        public Task<bool> RevokeTokenAsync(string token) =>
            //
            // JWT tokens cannot be revoked as they have no state
            //

            Task.FromResult(false);
    }
}
