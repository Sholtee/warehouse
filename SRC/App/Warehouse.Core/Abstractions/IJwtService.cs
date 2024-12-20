using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Tokens;

namespace Warehouse.Core.Abstractions
{
    /// <summary>
    /// Service to create and validate JSON Web Tokens
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Creates a new JWT
        /// </summary>
        Task<string> CreateTokenAsync(string user, IEnumerable<string> roles, DateTimeOffset expires);

        /// <summary>
        /// Validates the given JWT
        /// </summary>
        Task<TokenValidationResult> ValidateTokenAsync(string token);
    }
}
