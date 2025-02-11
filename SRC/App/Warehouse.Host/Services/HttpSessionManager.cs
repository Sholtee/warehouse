/********************************************************************************
* HttpSessionManager.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Warehouse.Host.Services
{
    using Core.Abstractions;
    using Core.Extensions;

    internal sealed class HttpSessionManager(IHttpContextAccessor httpContext, IConfiguration configuration, TimeProvider timeProvider) : ISessionManager
    {
        private readonly string _sessionCookieName = configuration.GetRequiredValue<string>("Auth:SessionCookieName");

        private readonly bool _slidingExpiration = configuration.GetValue("Auth:SlidingExpiration", true);

        private readonly int  _sessionExpirationMinutes = configuration.GetValue("Auth:SessionExpirationMinutes", 1440);

        public string? Token
        {
            get => httpContext
                .HttpContext!
                .Request
                .Cookies
                .TryGetValue(_sessionCookieName, out string? token) ? token : null;
            set
            {
                HttpContext context = httpContext.HttpContext!;

                if (string.IsNullOrEmpty(value))
                {
                    if (context.Request.Cookies[_sessionCookieName] is not null)
                    {
                        context.Response.Cookies.Delete(_sessionCookieName);
                    }
                }
                else
                {
                    context.Response.Cookies.Append
                    (
                        _sessionCookieName,
                        value,
                        new CookieOptions
                        {
                            Expires = timeProvider.GetUtcNow().AddMinutes(_sessionExpirationMinutes),
                            Path = "/",
                            HttpOnly = true,
                            SameSite = SameSiteMode.Strict,
                            Secure = true
                        }
                    );
                }
            }
        }

        public bool SlidingExpiration => _slidingExpiration;
    }
}
