using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Warehouse.API.Controllers
{
    using Infrastructure.Auth;
    using Infrastructure.Extensions;

    /// <summary>
    /// Login endpoints.
    /// </summary>
    [ApiController, Route("api/v1")]
    public sealed class LoginController(IConfiguration configuration, IJwtService jwtService, ILogger<LoginController> logger) : ControllerBase
    {
        private UnauthorizedResult Unauthorized(string reason)
        {
            logger.LogInformation(reason);
            Response.Headers.Append("WWW-Authenticate", "Basic");
            return Unauthorized();
        }

        private sealed class GroupRoles
        {
            public required List<string> Roles { get; init; }
            public List<string>? Includes { get; init; }
        }

        private HashSet<string> GetAvailableRoles(List<string> groups)
        {
            HashSet<string> roles = [];

            Dictionary<string, GroupRoles> groupRoles = [];
            configuration.GetRequiredSection("Auth:GroupRoles").Bind(groupRoles);
            groups.ForEach(ExtendRoles);

            return roles;

            void ExtendRoles(string group)
            {
                if (groupRoles.TryGetValue(group, out GroupRoles? gr))
                {
                    roles.UnionWith(gr.Roles);
                    gr.Includes?.ForEach(ExtendRoles);
                }
            }
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

            DateTime expires = DateTime.Now.AddMinutes
            (
                configuration.GetValue("Auth:SessionExpirationMinutes", 1440)
            );

            Response.Cookies.Append
            (
                configuration.GetRequiredValue<string>("Auth:SessionCookieName"),
                await jwtService.CreateToken(clientId, ["Admin"], expires),
                new CookieOptions
                {
                    Expires = expires,
                    Domain = configuration.GetRequiredValue<string>("AppDomain"),
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
            string sessionCookie = configuration.GetRequiredValue<string>("Auth:SessionCookieName");

            if (Request.Cookies[sessionCookie] is not null)
            {
                Response.Cookies.Delete(sessionCookie);
            }

            return NoContent();
        }
    }
}
