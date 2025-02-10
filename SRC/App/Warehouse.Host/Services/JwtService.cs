/********************************************************************************
* JwtService.cs                                                                 *
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
    using Warehouse.DAL;

    internal sealed class JwtService
    (
        IMemoryCache cache,
        IHostEnvironment env,
        IConfiguration configuration,
        IAmazonSecretsManager secretsManager,
        ILogger<JwtService> logger,
        TimeProvider timeProvider
    ) : IJwtService
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

        private async Task<string> CreateTokenAsync(IEnumerable<Claim> claims) => _handler.WriteToken
        (
            new JwtSecurityToken
            (
                issuer: _domain,
                audience: _domain,
                claims: claims,
                expires: timeProvider.GetUtcNow().AddMinutes
                (
                    configuration.GetValue("Auth:SessionExpirationMinutes", 1440)
                ).DateTime,
                signingCredentials: new SigningCredentials(await SecurityKey, _algorithm)
            )
        );

        public async Task<string> CreateTokenAsync(string user, Roles roles)
        {
            string token = await CreateTokenAsync
            (
                [
                    new Claim(ClaimTypes.Name, user),
                    ..roles.SetFlags().Select(static role => new Claim(ClaimTypes.Role, role.ToString()))
                ]
            );

            logger.LogInformation("Token created for user: {user}", user);

            return token;
        }

        public async Task<string> RefreshTokenAsync(string token)
        {
            TokenValidationResult result = await ValidateTokenAsync(token);
            if (!result.IsValid)
                throw new ArgumentException("The provided token is not valid", nameof(token));

            JwtSecurityToken jwt = (JwtSecurityToken) result.SecurityToken;

            token = await CreateTokenAsync(jwt.Claims);

            logger.LogInformation("Token created for user: {user}", jwt.Claims.Single(static c => c.Type == ClaimTypes.Name).Value);

            return token;
        }

        public async Task<TokenValidationResult> ValidateTokenAsync(string token)
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

            if (result.IsValid)
                logger.LogInformation("Token validation completed for user: {user}", result.Claims[ClaimTypes.Name]);
            else
                logger.LogInformation("Token validation failed: {reason}", result.Exception?.ToString());

            return result;
        }
    }
}
