using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using Tracker.Client.Pages;
using Tracker.Client.Services;
using Tracker.Client.TestContext;
using Tracker.Shared.Auth;
using Xunit;

namespace Tracker.Client.Tests.Pages;

public class LoginTests : TestContext
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly TestNavigationManager _navigationManager;
    private readonly Mock<IJSRuntime> _jsRuntimeMock;

    public LoginTests()
    {
        ConfigureTestContext();
        
        _authServiceMock = Services.GetRequiredService<Mock<IAuthService>>();
        _navigationManager = (TestNavigationManager)Services.GetRequiredService<NavigationManager>();
        _jsRuntimeMock = Services.GetRequiredService<Mock<IJSRuntime>>();
    }

    [Fact]
    public void LoginPage_RendersCorrectly()
    {
        // Act
        var cut = RenderComponent<Login>();

        // Assert
        Assert.Contains("Sign in to your account", cut.Markup);
        Assert.Contains("Email address", cut.Markup);
        Assert.Contains("Password", cut.Markup);
        Assert.Contains("Remember me", cut.Markup);
        Assert.Contains("Sign in", cut.Markup);
    }

    [Fact]
    public void Login_WithInvalidInput_ShowsValidationErrors()
    {
        // Arrange
        var cut = RenderComponent<Login>();
        
        // Act - Try to submit without entering any data
        var form = cut.Find("form");
        form.Submit();

        // Assert
        Assert.Contains("The Email field is required.", cut.Markup);
        Assert.Contains("The Password field is required.", cut.Markup);
    }

    [Fact]
    public void Login_WithInvalidEmailFormat_ShowsValidationError()
    {
        // Arrange
        var cut = RenderComponent<Login>();
        
        // Act - Enter invalid email format
        var emailInput = cut.Find("#email-address");
        emailInput.Change("invalid-email");
        
        var form = cut.Find("form");
        form.Submit();

        // Assert
        Assert.Contains("The Email field is not a valid e-mail address.", cut.Markup);
    }

    [Fact]
    public async Task Login_WithValidCredentials_NavigatesToHome()
    {
        // Arrange
        _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(new AuthResponse { Success = true });
            
        var cut = RenderComponent<Login>();
        
        // Act - Enter valid credentials
        cut.Find("#email-address").Change("test@example.com");
        cut.Find("#password").Change("Test@123");
        cut.Find("form").Submit();

        // Assert
        _authServiceMock.Verify(x => x.LoginAsync(It.Is<LoginRequest>(r => 
            r.Email == "test@example.com" && r.Password == "Test@123")), Times.Once);
            
        // Wait for navigation
        await Task.Delay(100); // Small delay to allow navigation to complete
        Assert.Equal("http://localhost/", _navigationManager.Uri);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShowsErrorMessage()
    {
        // Arrange
        var errorMessage = "Invalid credentials";
        _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(new AuthResponse 
            { 
                Success = false, 
                Error = errorMessage 
            });
            
        var cut = RenderComponent<Login>();
        
        // Act - Submit with invalid credentials
        cut.Find("#email-address").Change("test@example.com");
        cut.Find("#password").Change("wrong-password");
        cut.Find("form").Submit();

        // Assert
        await Task.Delay(100); // Small delay to allow state update
        cut.Render();
        
        Assert.Contains(errorMessage, cut.Markup);
    }

    [Fact]
    public void Login_WithReturnUrl_PreservesItInFormAction()
    {
        // Arrange
        var returnUrl = "/protected-route";
        var navManager = Services.GetRequiredService<NavigationManager>();
        navManager.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        
        // Act
        var cut = RenderComponent<Login>();
        
        // Assert
        var form = cut.Find("form");
        Assert.Equal("/login?returnUrl=/protected-route", form.GetAttribute("action"));
    }
}
