/********************************************************************************
* JwtService.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
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

    internal sealed class JwtService
    (
        IMemoryCache cache,
        IHostEnvironment env,
        IConfiguration configuration,
        IAmazonSecretsManager secretsManager,
        ILogger<JwtService> logger
    ): IJwtService
    {
        private readonly JwtSecurityTokenHandler _handler = new();

        private readonly string 
            _domain = $"{env.EnvironmentName}-warehouse-app",
            _algorithm = configuration.GetValue($"{nameof(JwtService)}:Algorithm", SecurityAlgorithms.HmacSha256);

        private Task<SymmetricSecurityKey?> SecurityKey => cache.GetOrCreateAsync("jwt-secret-key", async entry =>
        {
            entry.AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes
            (
                configuration.GetValue($"{nameof(JwtService)}:CacheExpirationMinutes", 30)
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

        public async Task<string> CreateTokenAsync(string user, Roles roles, DateTimeOffset expires)
        {
            JwtSecurityToken token = new
            (
                issuer: _domain,
                audience: _domain,
                claims:
                [
                    new Claim(ClaimTypes.Name, user),
                    ..roles.SetFlags().Select(static role => new Claim(ClaimTypes.Role, role.ToString()))
                ],
                expires: expires.DateTime,
                signingCredentials: new SigningCredentials(await SecurityKey, _algorithm)
            );

            logger.LogInformation("Token created for user: {user}", user);

            return _handler.WriteToken(token);
        }

        public async Task<TokenValidationResult> ValidateTokenAsync(string token) => await _handler.ValidateTokenAsync
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
    }
}
