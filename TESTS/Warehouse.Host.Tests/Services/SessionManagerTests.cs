/********************************************************************************
* SessionManagerTests.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

using Moq;
using NUnit.Framework;


namespace Warehouse.Host.Services.Tests
{
    using Core.Abstractions;

    [TestFixture]
    internal sealed class SessionManagerTests
    {
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor = null!;
        private Mock<TimeProvider> _mockTimeProvider = null!;
        private IConfiguration _configuration = null!;
        private HttpContext _context = null!;


        [SetUp]
        public void SetupTest()
        {
            _context = new DefaultHttpContext();

            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
            _mockHttpContextAccessor
                .SetupGet(a => a.HttpContext)
                .Returns(_context);

            _mockTimeProvider = new Mock<TimeProvider>(MockBehavior.Strict);

            _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:SessionCookieName"] = "session-cookie",
                ["Auth:SlidingExpiration"] = "true",
                ["Auth:SessionExpirationMinutes"] = "1440",

            }).Build();

        }

        [Test]
        public void GetToken_ShouldReadTheTokenFromCookie()
        {
            ISessionManager sessionManager = new HttpSessionManager
            (
                _mockHttpContextAccessor.Object,
                _configuration,
                _mockTimeProvider.Object
            );

            _context.Request.Headers.Cookie = "session-cookie=token";

            Assert.That(sessionManager.Token, Is.EqualTo("token"));
        }

        [Test]
        public void GetToken_ShouldReturnNullIfThereIsNoSessionCookieProvided()
        {
            ISessionManager sessionManager = new HttpSessionManager
            (
                _mockHttpContextAccessor.Object,
                _configuration,
                _mockTimeProvider.Object
            );

            Assert.That(sessionManager.Token, Is.Null);
        }

        [Test]
        public void SetToken_ShouldDeleteTheSessionCookie()
        {
            ISessionManager sessionManager = new HttpSessionManager
            (
                _mockHttpContextAccessor.Object,
                _configuration,
                _mockTimeProvider.Object
            );

            _context.Request.Headers.Cookie = "session-cookie=token";

            sessionManager.Token = null;

            Assert.That(_context.Response.Headers.SetCookie, Is.EqualTo("session-cookie=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/"));
        }

        [Test]
        public void SetToken_ShouldSetTheSessionCookie()
        {
            DateTime now = new(year: 1986, month: 10, day: 26);

            _mockTimeProvider
                .Setup(t => t.GetUtcNow())
                .Returns(now);

            ISessionManager sessionManager = new HttpSessionManager
            (
                _mockHttpContextAccessor.Object,
                _configuration,
                _mockTimeProvider.Object
            );

            sessionManager.Token = "token";

            Assert.That(_context.Response.Headers.SetCookie, Is.EqualTo("session-cookie=token; expires=Sun, 26 Oct 1986 22:00:00 GMT; path=/; secure; samesite=strict; httponly"));
        }
    }
}
