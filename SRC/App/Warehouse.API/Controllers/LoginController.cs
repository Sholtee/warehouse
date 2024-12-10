using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

namespace Warehouse.API.Controllers
{
    /// <summary>
    /// Login endpoints.
    /// </summary>
    [ApiController, Route("api/v1")]
    public sealed class LoginController(IMemoryCache cache, IConfiguration configuration, IAmazonSecretsManager secretsManager, ILogger<LoginController> logger) : ControllerBase
    {
        internal UnauthorizedResult Unauthorized(string reason)
        {
            logger.LogInformation(reason);
            Response.Headers.Append("WWW-Authenticate", "Basic");
            return Unauthorized();
        }

        internal async Task<string> CreateJWT(string user, IEnumerable<string> roles, string domain, DateTime expires)
        {
            //
            // Get the secret key to create a new JWT
            //

            string secretKey = (await cache.GetOrCreateAsync("jwt-secret-key", async entry =>
            {
                entry.AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes
                (
                    configuration.GetValue("LoginController:CacheExpirationMinutes", 30)
                );

                GetSecretValueResponse resp = await secretsManager.GetSecretValueAsync(new GetSecretValueRequest
                {
                    SecretId = $"{configuration.GetValue("Prefix", "local")}-jwt-secret-key"
                });

                return resp.SecretString;
            }))!;

            JwtSecurityToken token = new
            (
                issuer: domain,
                audience: domain,
                claims:
                [
                    new Claim(ClaimTypes.Name, user),
                    ..roles.Select(static role => new Claim(ClaimTypes.Role, role))
                ],
                expires: expires,
                signingCredentials: new SigningCredentials
                (
                    new SymmetricSecurityKey
                    (
                        Encoding.UTF8.GetBytes(secretKey)
                    ),
                    SecurityAlgorithms.HmacSha256
                )
            );

            logger.LogInformation("Token created for user: {user}", user);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Uses Basic Auth to create a new login session
        /// </summary>
        [HttpGet("login")]
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
            // TODO: implement once we'll have DB
            //

            if (clientId != "admin" || clientSecret != "test")
            {
                return Unauthorized("Invalid credentials");
            }

            //
            // Set the session cookie
            //

            string domain = configuration["LoginController:AppDomain"] ?? throw new InvalidOperationException("Domain must be specified");

            DateTime expires =  DateTime.Now.AddMinutes
            (
                configuration.GetValue("LoginController:TokenExpirationMinutes", 1440)
            );

            Response.Cookies.Append
            (
                "warehouse-session",
                await CreateJWT(clientId, ["Admin"], domain, expires),
                new CookieOptions
                {
                    Expires = expires,
                    Domain = domain,
                    Path = "/",
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict
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
        [HttpGet("logout")]
        public IActionResult Logout()
        {
            if (Request.Cookies["warehouse-session"] is not null)
            {
                Response.Cookies.Delete("warehouse-session");
            }

            return NoContent();
        }
    }
}
