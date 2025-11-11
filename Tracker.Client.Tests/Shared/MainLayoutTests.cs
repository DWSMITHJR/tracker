using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Tracker.Client.Shared;
using Tracker.Client.TestContext;
using Xunit;

namespace Tracker.Client.Tests.Shared;

public class MainLayoutTests : TestContext
{
    private readonly TestNavigationManager _navigationManager;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IAppSettingsService> _appSettingsMock;
    private readonly Mock<IToastService> _toastServiceMock;

    public MainLayoutTests()
    {
        ConfigureTestContext();
        
        _authServiceMock = Services.GetRequiredService<Mock<IAuthService>>();
        _navigationManager = (TestNavigationManager)Services.GetRequiredService<NavigationManager>();
        
        // Setup mocks for services used in MainLayout
        _appSettingsMock = new Mock<IAppSettingsService>();
        _toastServiceMock = new Mock<IToastService>();
        
        Services.AddSingleton(_appSettingsMock.Object);
        Services.AddSingleton(_toastServiceMock.Object);
        
        // Setup default auth state (unauthenticated)
        SetUnauthenticatedUser();
    }

    [Fact]
    public void MainLayout_RendersCorrectly_ForUnauthenticatedUser()
    {
        // Arrange
        SetUnauthenticatedUser();
        
        // Act
        var cut = RenderComponent<MainLayout>();
        
        // Assert
        Assert.Contains("IndiTracker", cut.Markup);
        Assert.DoesNotContain("Logout", cut.Markup);
        Assert.DoesNotContain("Admin", cut.Markup);
    }

    [Fact]
    public void MainLayout_ShowsNavigationLinks_ForAuthenticatedUser()
    {
        // Arrange
        SetAuthenticatedUser("test-user", "User");
        
        // Act
        var cut = RenderComponent<MainLayout>();
        
        // Assert
        Assert.Contains("Dashboard", cut.Markup);
        Assert.Contains("Incidents", cut.Markup);
        Assert.Contains("Individuals", cut.Markup);
        Assert.Contains("Contacts", cut.Markup);
    }

    [Fact]
    public void MainLayout_ShowsAdminLink_ForAdminUser()
    {
        // Arrange
        SetAuthenticatedUser("admin-user", "Admin");
        
        // Act
        var cut = RenderComponent<MainLayout>();
        
        // Assert
        Assert.Contains("Admin", cut.Markup);
    }

    [Fact]
    public void MobileMenu_Toggles_WhenHamburgerClicked()
    {
        // Arrange
        SetAuthenticatedUser("test-user", "User");
        var cut = RenderComponent<MainLayout>();
        
        // Initial state - menu should be closed
        Assert.DoesNotContain("Mobile menu", cut.Markup);
        
        // Act - Open menu
        var hamburgerButton = cut.Find("button[aria-controls='mobile-menu']");
        hamburgerButton.Click();
        
        // Assert - Menu should be open
        Assert.Contains("Mobile menu", cut.Markup);
        
        // Act - Close menu
        hamburgerButton.Click();
        
        // Assert - Menu should be closed
        Assert.DoesNotContain("Mobile menu", cut.Markup);
    }

    [Fact]
    public void ActiveLink_GetsHighlighted()
    {
        // Arrange
        SetAuthenticatedUser("test-user", "User");
        _navigationManager.NavigateTo("/incidents");
        
        // Act
        var cut = RenderComponent<MainLayout>();
        
        // Assert - Incidents link should be highlighted
        var incidentsLink = cut.Find("a[href='/incidents']");
        Assert.Contains("bg-indigo-700", incidentsLink.ClassName);
    }

    [Fact]
    public async Task Logout_Button_Works_Correctly()
    {
        // Arrange
        SetAuthenticatedUser("test-user", "User");
        _authServiceMock.Setup(x => x.LogoutAsync()).Returns(Task.CompletedTask);
        
        var cut = RenderComponent<MainLayout>();
        
        // Act - Open user menu and click logout
        var userMenuButton = cut.Find("button[id='user-menu-button']");
        userMenuButton.Click();
        
        var logoutButton = cut.Find("button:contains('Sign out')");
        await logoutButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        
        // Assert
        _authServiceMock.Verify(x => x.LogoutAsync(), Times.Once);
        Assert.Equal("/login", _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/"));
    }

    private void SetAuthenticatedUser(string userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        };
        
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        
        var authState = new AuthenticationState(user);
        var authStateProvider = Services.GetRequiredService<AuthenticationStateProvider>() as TestAuthStateProvider;
        authStateProvider?.SetAuthenticationState(Task.FromResult(authState));
    }

    private void SetUnauthenticatedUser()
    {
        var authState = new AuthenticationState(new ClaimsPrincipal());
        var authStateProvider = Services.GetRequiredService<AuthenticationStateProvider>() as TestAuthStateProvider;
        authStateProvider?.SetAuthenticationState(Task.FromResult(authState));
    }
}
