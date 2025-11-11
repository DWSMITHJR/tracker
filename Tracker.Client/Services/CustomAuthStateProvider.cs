using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Tracker.Client.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;
    public event Func<Task>? TokenRefreshRequested;

    public CustomAuthStateProvider(
        ILocalStorageService localStorage,
        HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var savedToken = await _localStorage.GetItemAsync<string>("authToken");

        if (string.IsNullOrWhiteSpace(savedToken))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        try
        {
            // Check if the token is expired
            var tokenExpiration = GetTokenExpiration(savedToken);
            if (tokenExpiration < DateTime.UtcNow)
            {
                // Token is expired, request a refresh via event
                if (TokenRefreshRequested != null)
                {
                    await TokenRefreshRequested.Invoke();
                    // Get the new token after refresh
                    savedToken = await _localStorage.GetItemAsync<string>("authToken");
                    
                    // If still expired after refresh, return unauthorized
                    if (string.IsNullOrEmpty(savedToken) || GetTokenExpiration(savedToken) < DateTime.UtcNow)
                    {
                        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                    }
                }
                else
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
            }

            // Set the authorization header for subsequent requests
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", savedToken);

            var claims = string.IsNullOrEmpty(savedToken) 
                ? Enumerable.Empty<Claim>() 
                : ParseClaimsFromJwt(savedToken);
                
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            
            return new AuthenticationState(user);
        }
        catch
        {
            // If there's an error, log the user out
            await Logout();
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        _httpClient.DefaultRequestHeaders.Authorization = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void MarkUserAsAuthenticated(string token)
    {
        var claims = ParseClaimsFromJwt(token);
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
        var authState = Task.FromResult(new AuthenticationState(user));
        NotifyAuthenticationStateChanged(authState);
    }

    public void MarkUserAsLoggedOut()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        if (string.IsNullOrEmpty(jwt))
            return Enumerable.Empty<Claim>();
            
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            return Enumerable.Empty<Claim>();
            
        try
        {
            var payload = parts[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
            
            return keyValuePairs?.Select(kvp => new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty)) 
                ?? Enumerable.Empty<Claim>();
        }
        catch
        {
            return Enumerable.Empty<Claim>();
        }
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }

    private static DateTime GetTokenExpiration(string token)
    {
        if (string.IsNullOrEmpty(token))
            return DateTime.MinValue;
            
        var parts = token.Split('.');
        if (parts.Length != 3)
            return DateTime.MinValue;
            
        try
        {
            var payload = parts[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);
            
            if (keyValuePairs != null && 
                keyValuePairs.TryGetValue("exp", out var expValue) && 
                expValue.TryGetInt64(out var expUnixTime))
            {
                return DateTimeOffset.FromUnixTimeSeconds(expUnixTime).UtcDateTime;
            }
        }
        catch
        {
            // If there's an error, return MinValue to indicate an invalid/expired token
        }
        
        return DateTime.MinValue;
    }
}
