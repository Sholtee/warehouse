/********************************************************************************
* LoggingMiddlewareTests.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Warehouse.Host.Infrastructure.Tests
{
    using Middlewares;

    [TestFixture]
    internal sealed class LoggingMiddlewareTests
    {
        private static bool CheckClient(object client, string user, params string[] roles)
        {
            dynamic c = client;
            Assert.That(c.Id, Is.EqualTo(user));
            Assert.That(c.Roles, Is.EquivalentTo(roles));
            return true;
        }

        [Test]
        public void InvokeAsync_ShouldCreateANewLoggingScopeContainingTheUserData()
        {
            Mock<IIdentity> mockIdentity = new(MockBehavior.Strict);
            mockIdentity
                .SetupGet(i => i.Name)
                .Returns("some user");
            mockIdentity
                .SetupGet(i => i.AuthenticationType)
                .Returns("SomeAuth");

            Mock<IDisposable> mockDisposable = new(MockBehavior.Strict);

            object? val;

            MockSequence seq = new();

            Mock<ILogger<LoggingMiddleware>> mockLogger = new(MockBehavior.Strict);
            mockLogger
                .InSequence(seq)
                .Setup(l => l.BeginScope(It.Is<Dictionary<string, object>>(d => d.TryGetValue("@Client", out val) && CheckClient(val, "some user", "Admin"))))
                .Returns(mockDisposable.Object);
           
            ClaimsPrincipal principal = new(mockIdentity.Object);
            principal.AddIdentity(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin"), new Claim(ClaimTypes.Email, "some@email.com")]));

            HttpContext cntx = new DefaultHttpContext
            {
                User = principal
            };

            Mock<RequestDelegate> mockRequestDelegate = new(MockBehavior.Strict);
            mockRequestDelegate
                .InSequence(seq)
                .Setup(d => d.Invoke(cntx))
                .Returns(Task.CompletedTask);

            mockDisposable
                .InSequence(seq)
                .Setup(d => d.Dispose());

            LoggingMiddleware middleware = new(mockRequestDelegate.Object, mockLogger.Object);
            Assert.DoesNotThrowAsync(() => middleware.InvokeAsync(cntx));

            mockDisposable.Verify(d => d.Dispose(), Times.Once);  // due to the sequence we don't need to verify the rest
        }

        [Test]
        public void InvokeAsync_ShouldCreateANewLoggingScopeContainingTheUserData_AnonCase()
        {
            Mock<IDisposable> mockDisposable = new(MockBehavior.Strict);

            object? val;

            MockSequence seq = new();

            Mock<ILogger<LoggingMiddleware>> mockLogger = new(MockBehavior.Strict);
            mockLogger
                .InSequence(seq)
                .Setup(l => l.BeginScope(It.Is<Dictionary<string, object>>(d => d.TryGetValue("@Client", out val) && CheckClient(val, "Anonymous"))))
                .Returns(mockDisposable.Object);

            HttpContext cntx = new DefaultHttpContext
            {
                User = new ClaimsPrincipal()
            };

            Mock<RequestDelegate> mockRequestDelegate = new(MockBehavior.Strict);
            mockRequestDelegate
                .InSequence(seq)
                .Setup(d => d.Invoke(cntx))
                .Returns(Task.CompletedTask);

            mockDisposable
                .InSequence(seq)
                .Setup(d => d.Dispose());

            LoggingMiddleware middleware = new(mockRequestDelegate.Object, mockLogger.Object);
            Assert.DoesNotThrowAsync(() => middleware.InvokeAsync(cntx));

            mockDisposable.Verify(d => d.Dispose(), Times.Once);
        }
    }
}
