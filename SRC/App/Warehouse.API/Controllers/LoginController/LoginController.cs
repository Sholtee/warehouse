/********************************************************************************
* LoginController.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Warehouse.API.Controllers
{
    using Core.Abstractions;
    using Core.Attributes;
    using Core.Exceptions;
    using DAL;

    /// <summary>
    /// Login endpoints.
    /// </summary>
    [ApiController, Route("api/v1"), EnableRateLimiting("fixed")]
    public sealed class LoginController
    (
        IUserRepository userRepository,
        ITokenManager tokenManager,
        ISessionManager session,
        IPasswordHasher<string> passwordHasher,
        ILogger<LoginController> logger
    ) : ControllerBase
    {
        private const string SCHEME = "Basic";

        private UnauthorizedException Unauthorized(string reason)
        {
            logger.LogInformation("Authentication failed: {reason}", reason);
            return new UnauthorizedException
            {
                Authenticate = SCHEME
            };
        }

        /// <summary>
        /// Uses Basic Auth to create a new login session
        /// </summary>
        /// <response code="204">Login was successful, the session cookie is provided via the Set-Cookie header</response>
        /// <response code="401">The client is unauthorized to execute the operation.</response>
        [HttpGet("login"), ResponseCode(204)]
        [ApiExplorerBasicAuthorization, ProducesResponseType(204), ProducesResponseType(401)] // ProducesResponseType is required to prevent Swagger from enumerating HTPP 200 as well
        public async Task Login()
        {
            const string HEADER_PREFIX = $"{SCHEME} ";

            if (!Request.Headers.TryGetValue("Authorization", out StringValues value))
            {
                throw Unauthorized("No Authorization header provided");
            }

            string authorizationHeader = value.ToString();

            if (!authorizationHeader.StartsWith(HEADER_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                throw Unauthorized("Invalid Authorization header provided");
            }

            //
            // B64 string is always longer than the original
            //

            Span<byte> rawData = new byte[(authorizationHeader.Length - HEADER_PREFIX.Length) * sizeof(char)];

            if (!Convert.TryFromBase64String(authorizationHeader[HEADER_PREFIX.Length..], rawData, out int bytesWritten))
            {
                throw Unauthorized("Invalid Base64 string");
            }

            if (Encoding.UTF8.GetString(rawData[..bytesWritten]).Split(':', 2) is not [string clientId, string clientSecret])
            {
                throw Unauthorized("Invalid Authorization header format");
            }

            //
            // Verify the credentials
            //

            User? user = await userRepository.QueryUser(clientId);
            if (user is null || passwordHasher.VerifyHashedPassword(user.ClientId, user.ClientSecretHash, clientSecret) != PasswordVerificationResult.Success)
            {
                throw Unauthorized("Invalid credentials");
            }

            //
            // Set up the session cookie
            //

            session.Token = await tokenManager.CreateTokenAsync(clientId, user.Roles);

            //
            // TODO: redirect the client to its original destination
            //
        }

        /// <summary>
        /// Logs out the client
        /// </summary>
        /// <response code="204">The operation completed successfully</response>
        [HttpGet("logout"), ResponseCode(204)]
        [ApiExplorerSessionCookieAuthorization, ProducesResponseType(204)]
        public async Task Logout()
        {
            if (!string.IsNullOrEmpty(session.Token))
            {
                await tokenManager.RevokeTokenAsync(session.Token);
                session.Token = null;
            }
        }
    }
}
