using Tracker.Shared.Auth;

namespace Tracker.Client.Services;

public interface IAuthService
{
    Task<AuthResult> Register(RegisterRequest registerRequest);
    Task<AuthResult> Login(LoginRequest loginRequest);
    Task Logout();
    Task<string?> GetToken();
    Task<string?> GetRefreshToken();
    Task<bool> IsAuthenticated();
    Task<AuthResult> RefreshToken();
}
