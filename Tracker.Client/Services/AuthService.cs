using System.Net.Http.Json;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Tracker.Shared.Auth;

namespace Tracker.Client.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly CustomAuthStateProvider _authStateProvider;
    private readonly ILocalStorageService _localStorage;

    public AuthService(
        HttpClient httpClient,
        CustomAuthStateProvider authStateProvider,
        ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
        _localStorage = localStorage;
    }

    public async Task<AuthResult> Register(RegisterRequest registerRequest)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", registerRequest);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            return new AuthResult { Success = false, Error = error?.Message ?? "Registration failed" };
        }

        return new AuthResult { Success = true };
    }

    public async Task<AuthResult> Login(LoginRequest loginRequest)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            return new AuthResult { Success = false, Error = error?.Message ?? "Login failed" };
        }

        var loginResult = await response.Content.ReadFromJsonAsync<LoginResult>();
        
        if (loginResult == null || string.IsNullOrEmpty(loginResult.Token) || string.IsNullOrEmpty(loginResult.RefreshToken))
        {
            return new AuthResult { Success = false, Error = "Invalid login response" };
        }
        
        // Store the tokens
        await _localStorage.SetItemAsync("authToken", loginResult.Token);
        await _localStorage.SetItemAsync("refreshToken", loginResult.RefreshToken);
        
        // Notify the authentication state provider
        ((CustomAuthStateProvider)_authStateProvider).MarkUserAsAuthenticated(loginResult.Token);
        
        return new AuthResult { Success = true };
    }

    public async Task Logout()
    {
        // Remove the tokens from local storage
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        
        // Notify the authentication state provider
        ((CustomAuthStateProvider)_authStateProvider).MarkUserAsLoggedOut();
        
        // Navigate to the login page
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<string?> GetToken()
    {
        return await _localStorage.GetItemAsync<string>("authToken");
    }

    public async Task<string?> GetRefreshToken()
    {
        return await _localStorage.GetItemAsync<string>("refreshToken");
    }

    public async Task<bool> IsAuthenticated()
    {
        var token = await GetToken();
        return !string.IsNullOrEmpty(token);
    }

    public async Task<AuthResult> RefreshToken()
    {
        var token = await GetToken();
        var refreshToken = await GetRefreshToken();
        
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
        {
            return new AuthResult { Success = false, Error = "No valid refresh token found" };
        }
        
        var request = new RefreshTokenRequest
        {
            Token = token,
            RefreshToken = refreshToken
        };
        
        var response = await _httpClient.PostAsJsonAsync("api/auth/refresh-token", request);
        
        if (!response.IsSuccessStatusCode)
        {
            // If refresh fails, log the user out
            await Logout();
            return new AuthResult { Success = false, Error = "Session expired. Please log in again." };
        }
        
        var result = await response.Content.ReadFromJsonAsync<LoginResult>();
        
        if (result == null || string.IsNullOrEmpty(result.Token) || string.IsNullOrEmpty(result.RefreshToken))
        {
            await Logout();
            return new AuthResult { Success = false, Error = "Invalid refresh token response" };
        }
        
        // Store the new tokens
        await _localStorage.SetItemAsync("authToken", result.Token);
        await _localStorage.SetItemAsync("refreshToken", result.RefreshToken);
        
        // Notify the authentication state provider
        ((CustomAuthStateProvider)_authStateProvider).MarkUserAsAuthenticated(result.Token);
        
        return new AuthResult { Success = true };
    }
}

public class ApiError
{
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]> Errors { get; set; } = new();
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}
