using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tracker.API;
using Tracker.Shared.Auth;
using Tracker.Infrastructure.Data;
using Tracker.Infrastructure.Models;
using Xunit;
using Xunit.Abstractions;

namespace Tracker.AuthTests;

public class AuthEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;
    private readonly IServiceScope _scope;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    
    private const string TestUserEmail = "test@example.com";
    private const string TestUserPassword = "Test@123";
    private const string TestUserRole = "Admin";

    public AuthEndpointTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _output = output;
        
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Configure in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("AuthTestDb");
                });
                
                // Configure test JWT settings
                services.Configure<JwtSettings>(options =>
                {
                    options.Secret = "test_secret_key_that_is_at_least_32_chars_long";
                    options.Issuer = "test_issuer";
                    options.Audience = "test_audience";
                    options.ExpirationInMinutes = 60; // 1 hour
                    options.RefreshTokenExpirationInDays = 1; // 1 day
                });
            });
        });
        
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        
        _scope = _factory.Services.CreateScope();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    }
    
    public async Task InitializeAsync()
    {
        _output.WriteLine("Initializing test data...");
        
        // Create test role if it doesn't exist
        if (!await _roleManager.RoleExistsAsync(TestUserRole))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>(TestUserRole));
        }
        
        // Create test user if it doesn't exist
        var user = await _userManager.FindByEmailAsync(TestUserEmail);
        if (user == null)
        {
            user = new User
            {
                UserName = TestUserEmail,
                Email = TestUserEmail,
                EmailConfirmed = true,
                IsActive = true,
                FirstName = "Test",
                LastName = "User"
            };
            
            var result = await _userManager.CreateAsync(user, TestUserPassword);
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors)}");
            }
            
            await _userManager.AddToRoleAsync(user, TestUserRole);
            _output.WriteLine($"Created test user with ID: {user.Id}");
        }
    }
    
    public async Task DisposeAsync()
    {
        _output.WriteLine("Cleaning up test data...");
        
        // Clean up test user
        var user = await _userManager.FindByEmailAsync(TestUserEmail);
        if (user != null)
        {
            await _userManager.DeleteAsync(user);
        }
        
        // Clean up test role
        var role = await _roleManager.FindByNameAsync(TestUserRole);
        if (role != null)
        {
            await _roleManager.DeleteAsync(role);
        }
        
        _scope.Dispose();
        _client.Dispose();
    }
    
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccessAndTokens()
    {
        // Arrange
        var loginData = new 
        {
            email = TestUserEmail,
            password = TestUserPassword
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginData);
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Login response: {content}");
        
        var json = JsonDocument.Parse(content).RootElement;
        Assert.True(json.TryGetProperty("token", out _), "Response should contain a token");
        Assert.True(json.TryGetProperty("refreshToken", out _), "Response should contain a refresh token");
    }
    
    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginData = new 
        {
            email = TestUserEmail,
            password = "wrong_password"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginData);
        
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
