using System;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Warehouse.API.Controllers
{
    using Core.Abstractions;
    using Core.Attributes;
    using Core.Extensions;
    using DAL;

    /// <summary>
    /// Login endpoints.
    /// </summary>
    [ApiController, Route("api/v1")]
    public sealed class LoginController
    (
        IUserRepository userRepository,
        IConfiguration configuration,
        IJwtService jwtService,
        IPasswordHasher<string> passwordHasher,
        ILogger<LoginController> logger,
        TimeProvider timeProvider
    ) : ControllerBase
    {
        private UnauthorizedResult Unauthorized(string reason)
        {
            logger.LogInformation("Authentication failed: {reason}", reason);
            Response.Headers.Append("WWW-Authenticate", "Basic");
            return Unauthorized();
        }

        /// <summary>
        /// Uses Basic Auth to create a new login session
        /// </summary>
        /// <response code="204">Login was successful, the session cookie is provided via the Set-Cookie header</response>
        /// <response code="401">The client is unathorized to execute the operation.</response>
        [HttpGet("login")]
        [ApiExplorerBasicAuthorization]
        public async Task<IActionResult> Login()
        {
            const string HEADER_PREFIX = "Basic ";

            if (!Request.Headers.TryGetValue("Authorization", out StringValues value))
            {
                return Unauthorized("No Authorization header provided");
            }

            string authorizationHeader = value.ToString();

            if (!authorizationHeader.StartsWith(HEADER_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized("No Authorization header provided");
            }

            //
            // B64 string is always longer than the original
            //

            Span<byte> rawData = new byte[(authorizationHeader.Length - HEADER_PREFIX.Length) * sizeof(char)];

            if (!Convert.TryFromBase64String(authorizationHeader[HEADER_PREFIX.Length..], rawData, out int bytesWritten))
            {
                return Unauthorized("Invalid Base64 string");
            }

            if (Encoding.UTF8.GetString(rawData.Slice(0, bytesWritten)).Split(':', 2) is not [string clientId, string clientSecret])
            {
                return Unauthorized("Invalid Authorization header format");
            }

            //
            // Verify the credentials
            //

            User? user = await userRepository.QueryUser(clientId);
            if (user is null || passwordHasher.VerifyHashedPassword(user.ClientId, user.ClientSecretHash, clientSecret) != PasswordVerificationResult.Success)
            {
                return Unauthorized("Invalid credentials");
            }

            //
            // Set up the session cookie
            //

            DateTimeOffset expires = timeProvider.GetUtcNow().AddMinutes
            (
                configuration.GetValue("Auth:SessionExpirationMinutes", 1440)
            );

            Response.Cookies.Append
            (
                configuration.GetRequiredValue<string>("Auth:SessionCookieName"),
                await jwtService.CreateTokenAsync(clientId, user.Roles, expires),
                new CookieOptions
                {
                    Expires = expires,
                    Path = "/",
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict,
                    Secure = true
                }
            );

            //
            // TODO: redirect the client to its original destination
            //

            return NoContent();
        }

        /// <summary>
        /// Logs out the client
        /// </summary>
        /// <response code="204">The operation completed successfully</response>
        [HttpGet("logout")]
        [ApiExplorerSessionCookieAuthorization]
        public IActionResult Logout()
        {
            string sessionCookie = configuration.GetRequiredValue<string>("Auth:SessionCookieName");

            if (Request.Cookies[sessionCookie] is not null)
            {
                Response.Cookies.Delete(sessionCookie);
            }

            return NoContent();
        }
    }
}
