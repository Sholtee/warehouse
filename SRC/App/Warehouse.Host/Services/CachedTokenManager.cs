/********************************************************************************
* CachedTokenManager.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Warehouse.Host.Services
{
    using Core.Abstractions;
    using Core.Auth;
    using Core.Extensions;

    internal sealed class CachedTokenManager(IDistributedCache cache, IConfiguration configuration, ILogger<CachedTokenManager> logger, TimeProvider timeProvider) : ITokenManager
    {
        private readonly int _sessionExpirationMinutes = configuration.GetRequiredValue<int>("Auth:SessionExpirationMinutes");
        private readonly bool _slidingExpiration = configuration.GetValue("Auth:SlidingExpiration", false);

        private static string Prefix(string key) => $"TOKEN::{key}";

        public async Task<string> CreateTokenAsync(string user, Roles roles)
        {
            string key = Guid.NewGuid().ToString();

            DistributedCacheEntryOptions entryOptions = _slidingExpiration
                ? new() { SlidingExpiration = TimeSpan.FromMinutes(_sessionExpirationMinutes) }
                : new() { AbsoluteExpiration = timeProvider.GetUtcNow().AddMinutes(_sessionExpirationMinutes) };

            using MemoryStream stm = new();
            using BinaryWriter writer = new(stm, Encoding.UTF8);

            new ClaimsIdentity
            (
                [
                    new Claim(ClaimTypes.Name, user),
                    ..roles.SetFlags().Select(static role => new Claim(ClaimTypes.Role, role.ToString()))
                ],
                authenticationType: "CUSTOM"
            ).WriteTo(writer);

            stm.Seek(0, SeekOrigin.Begin);

            await cache.SetAsync
            (
                Prefix(key),
                stm.ToArray(),
                entryOptions
            );

            logger.LogInformation("Token created for user: {user}", user);

            return key;
        }

        public Task<string> RefreshTokenAsync(string token) =>
            //
            // We are using sliding expiration so this method is useless
            //

            Task.FromResult(token);

        public async Task<bool> RevokeTokenAsync(string token)
        {
            await cache.RemoveAsync(token);
            return true;
        }

        public async Task<ClaimsIdentity?> GetIdentityAsync(string token)
        {
            byte[]? identity = await cache.GetAsync(Prefix(token));
            if (identity is not null)
            {
                using BinaryReader reader = new(new MemoryStream(identity), Encoding.UTF8, leaveOpen: false);

                ClaimsIdentity deserialized = new(reader);

                logger.LogInformation("Token validation completed for user: {user}", deserialized.Name);
                return deserialized;
            }

            logger.LogInformation("Token validation failed for token: {token}", token);
            return null;
        }
    }
}
