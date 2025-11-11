using Tracker.Infrastructure.Models;
using Tracker.Shared.Auth;

namespace Tracker.API.Services
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(string email, string password, string firstName, string lastName, string role);
        Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
        Task Logout();
        Task<AuthResult> RefreshTokenAsync(string token, string refreshToken);
        Task<bool> RevokeTokenAsync(string token);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
        Task<string> GeneratePasswordResetTokenAsync(string email);
        
        // Debug methods to check JWT settings
        string? GetJwtSecretForDebug();
        string? GetJwtIssuerForDebug();
        string? GetJwtAudienceForDebug();
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public IEnumerable<string> Errors { get; set; } = new List<string>();
        
        public AuthResult()
        {
            // Initialize all string properties to empty strings to prevent null reference issues
            Token = string.Empty;
            RefreshToken = string.Empty;
            UserId = string.Empty;
            Email = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Role = string.Empty;
            Errors = new List<string>();
        }
    }
}
