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
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Warehouse.Host.Services
{
    using Core.Abstractions;
    using Core.Extensions;

    internal sealed class JwtService(IMemoryCache cache, IConfiguration configuration, IAmazonSecretsManager secretsManager, ILogger<JwtService> logger): IJwtService
    {
        private readonly JwtSecurityTokenHandler _handler = new();

        private readonly string 
            _domain = configuration.GetRequiredValue<string>("AppDomain"),
            _algorithm = configuration.GetValue($"{nameof(JwtService)}:Algorithm", SecurityAlgorithms.HmacSha256);

        private Task<SymmetricSecurityKey?> SecurityKey => cache.GetOrCreateAsync("jwt-secret-key", async entry =>
        {
            entry.AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes
            (
                configuration.GetValue($"{nameof(JwtService)}:CacheExpirationMinutes", 30)
            );

            GetSecretValueResponse resp = await secretsManager.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = $"{configuration.GetRequiredValue<string>("Prefix")}-jwt-secret-key"
            });

            return new SymmetricSecurityKey
            (
                Encoding.UTF8.GetBytes(resp.SecretString)
            );
        });

        public async Task<string> CreateTokenAsync(string user, IEnumerable<string> roles, DateTimeOffset expires)
        {
            JwtSecurityToken token = new
            (
                issuer: _domain,
                audience: _domain,
                claims:
                [
                    new Claim(ClaimTypes.Name, user),
                    ..roles.Select(static role => new Claim(ClaimTypes.Role, role))
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
