using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tracker.API.Controllers;
using Tracker.API.Services;
using Tracker.Shared.Auth;
using Tracker.Tests.Helpers;
using Xunit;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Tracker.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_mockAuthService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Register_WithValidModel_ReturnsOkResult()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            FirstName = "Test",
            LastName = "User"
        };

        _mockAuthService.Setup(x => x.RegisterAsync(
            request.Email, 
            request.Password, 
            request.FirstName, 
            request.LastName, 
            It.IsAny<string>()))
            .ReturnsAsync(new AuthResult { Success = true });

        // Act
        var result = await _controller.Register(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Register_WithInvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "", // Invalid email to trigger validation
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            FirstName = "Test",
            LastName = "User"
        };
        _controller.ModelState.AddModelError("Email", "Email is required");

        // Act
        var result = await _controller.Register(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test@123"
        };

        var expectedAuthResult = new AuthResult 
        { 
            Success = true, 
            Token = "test-token",
            RefreshToken = "refresh-token",
            UserId = "test-user-id",
            Email = request.Email,
            FirstName = "Test",
            LastName = "User",
            Role = "User"
        };

        _mockAuthService.Setup(x => x.LoginAsync(
            request.Email, 
            request.Password, 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAuthResult);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
        
        // Use reflection to safely access properties
        var responseType = response.GetType();
        var tokenProperty = responseType.GetProperty("Token");
        var refreshTokenProperty = responseType.GetProperty("RefreshToken");
        
        Assert.NotNull(tokenProperty);
        Assert.NotNull(refreshTokenProperty);
        
        var token = tokenProperty.GetValue(response) as string;
        var refreshToken = refreshTokenProperty.GetValue(response) as string;
        
        Assert.NotNull(token);
        Assert.NotNull(refreshToken);
        Assert.Equal(expectedAuthResult.Token, token);
        Assert.Equal(expectedAuthResult.RefreshToken, refreshToken);
    }
}
