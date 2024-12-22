/********************************************************************************
* IJwtService.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Tokens;

namespace Warehouse.Core.Abstractions
{
    using Auth;

    /// <summary>
    /// Service to create and validate JSON Web Tokens
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Creates a new JWT
        /// </summary>
        Task<string> CreateTokenAsync(string user, Roles roles, DateTimeOffset expires);

        /// <summary>
        /// Validates the given JWT
        /// </summary>
        Task<TokenValidationResult> ValidateTokenAsync(string token);
    }
}
