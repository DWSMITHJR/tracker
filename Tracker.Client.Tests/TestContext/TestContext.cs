using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using System.Security.Claims;
using Tracker.Client.Auth;
using Tracker.Client.Services;

namespace Tracker.Client.Tests.TestContext;

public class TestContext : TestContextWrapper
{
    protected TestAuthorizationContext AuthContext { get; private set; } = null!;
    protected Mock<IJSRuntime> JSRuntimeMock { get; private set; } = null!;
    protected Mock<ILogger<AuthService>> AuthLoggerMock { get; private set; } = null!;
    protected Mock<IApiService> ApiServiceMock { get; private set; } = null!;
    protected Mock<INotificationService> NotificationServiceMock { get; private set; } = null!;

    [ModuleInitializer]
    public static void Initialize()
    {
        TestContext = new Bunit.TestContext();
    }

    public static new void Dispose()
    {
        TestContext?.Dispose();
    }

    protected void ConfigureTestContext()
    {
        // Setup mocks
        JSRuntimeMock = new Mock<IJSRuntime>();
        AuthLoggerMock = new Mock<ILogger<AuthService>>();
        ApiServiceMock = new Mock<IApiService>();
        NotificationServiceMock = new Mock<INotificationService>();

        // Register services
        Services.AddSingleton(JSRuntimeMock.Object);
        Services.AddSingleton(AuthLoggerMock.Object);
        Services.AddSingleton(ApiServiceMock.Object);
        Services.AddSingleton(NotificationServiceMock.Object);

        // Setup authentication
        AuthContext = this.AddTestAuthorization();
        
        // Register auth state provider for testing
        Services.AddScoped<AuthenticationStateProvider, TestAuthStateProvider>();
        
        // Register test navigation manager
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var testNavigationManager = new TestNavigationManager("http://localhost/", "http://localhost/");
        Services.AddSingleton<NavigationManager>(testNavigationManager);
    }

    protected void SetAuthorizedUser(string userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        
        AuthContext.SetAuthorized(user);
    }

    protected void SetUnauthorizedUser()
    {
        AuthContext.SetNotAuthorized();
    }

    protected static IRenderedComponent<TComponent> RenderComponent<TComponent>(params ComponentParameter[] parameters)
        where TComponent : IComponent
    {
        return TestContext.RenderComponent<TComponent>(parameters);
    }
}
