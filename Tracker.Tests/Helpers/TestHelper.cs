using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Tracker.Tests.Helpers
{
    public static class TestHelper
    {
        public static ControllerContext CreateTestControllerContext(Guid userId, string email, string role, Guid? organizationId = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Role, role)
            };

            if (organizationId.HasValue)
            {
                claims.Add(new Claim("organizationId", organizationId.Value.ToString()));
                claims.Add(new Claim("organization", organizationId.Value.ToString()));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            httpContext.RequestServices = serviceProvider;

            return new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        public static void SetupTestUser(ControllerBase controller, Guid userId, string email, string role, Guid? organizationId = null)
        {
            controller.ControllerContext = CreateTestControllerContext(userId, email, role, organizationId);
        }
    }
}
