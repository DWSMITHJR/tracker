using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tracker.API.Services;
using Tracker.Shared.Auth;

namespace Tracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

    // Local DTO for revoke since it's not present in shared models
    public class RevokeTokenRequest
    {
        public required string Token { get; set; }
    }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterAsync(
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName,
                    "Client");

                if (!result.Success)
                {
                    return BadRequest(new { Errors = result.Errors });
                }

                return Ok(new
                {
                    result.Token,
                    result.RefreshToken,
                    result.UserId,
                    result.Email,
                    result.FirstName,
                    result.LastName,
                    result.Role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new { Errors = new[] { "An error occurred while processing your request." } });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request, 
            [FromQuery] bool debug = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Log debug information if requested
                if (debug && _authService is AuthService authService)
                {
                    _logger.LogInformation("=== AUTH SERVICE DEBUG INFO ===");
                    _logger.LogInformation($"JWT Secret configured: {!string.IsNullOrEmpty(authService.GetJwtSecretForDebug())}");
                    _logger.LogInformation($"JWT Issuer: {authService.GetJwtIssuerForDebug()}");
                    _logger.LogInformation($"JWT Audience: {authService.GetJwtAudienceForDebug()}");
                }

                var result = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);

                if (!result.Success)
                {
                    _logger.LogWarning("Login failed for user {Email}. Errors: {Errors}", 
                        request.Email, string.Join(", ", result.Errors ?? new[] { "Unknown error" }));
                    return Unauthorized(new { Errors = result.Errors });
                }

                _logger.LogInformation("User {Email} logged in successfully", request.Email);
                return Ok(new
                {
                    result.Token,
                    result.RefreshToken,
                    result.UserId,
                    result.Email,
                    result.FirstName,
                    result.LastName,
                    result.Role
                });
            }
            catch (Exception ex)
            {
                // Log the full exception details including inner exceptions
                _logger.LogError(ex, "Error during user login for email: {Email}. Error: {Message}\nStack Trace: {StackTrace}", 
                    request.Email, 
                    ex.Message, 
                    ex.StackTrace);

                // Include more detailed error information in development environment
                var errorMessage = "An error occurred while processing your request.";
                #if DEBUG
                errorMessage = $"An error occurred: {ex.Message}\nStack Trace: {ex.StackTrace}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nInner Exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
                }
                #endif

                return StatusCode(500, new { 
                    Errors = new[] { errorMessage },
                    ExceptionType = ex.GetType().FullName,
                    ExceptionMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    InnerException = ex.InnerException != null ? new {
                        Type = ex.InnerException.GetType().FullName,
                        Message = ex.InnerException.Message,
                        StackTrace = ex.InnerException.StackTrace
                    } : null
                });
            }
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RefreshTokenAsync(request.Token, request.RefreshToken);

                if (!result.Success)
                {
                    return BadRequest(new { Errors = result.Errors });
                }

                return Ok(new
                {
                    result.Token,
                    result.RefreshToken,
                    result.UserId,
                    result.Email,
                    result.FirstName,
                    result.LastName,
                    result.Role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { Errors = new[] { "An error occurred while refreshing token." } });
            }
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RevokeTokenAsync(request.Token);
                
                if (!result)
                {
                    return BadRequest(new { Errors = new[] { "Invalid token." } });
                }

                return Ok(new { Message = "Token revoked successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                return StatusCode(500, new { Errors = new[] { "An error occurred while revoking token." } });
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var token = await _authService.GeneratePasswordResetTokenAsync(request.Email);
                
                if (string.IsNullOrEmpty(token))
                {
                    // Don't reveal that the user doesn't exist
                    return Ok(new { Message = "If your email is registered, you will receive a password reset link." });
                }

                // In a real application, you would send an email with the reset link
                // For now, we'll just return the token (in production, never return the token in the response)
                return Ok(new { 
                    Message = "If your email is registered, you will receive a password reset link.",
                    Token = token // Remove this in production
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password reset token");
                return StatusCode(500, new { Errors = new[] { "An error occurred while processing your request." } });
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { Errors = new[] { "Email is required for password reset." } });
                }

                var result = await _authService.ResetPasswordAsync(request.Email, request.Token, request.Password);
                
                if (!result)
                {
                    return BadRequest(new { Errors = new[] { "Invalid token or email." } });
                }

                return Ok(new { Message = "Password has been reset successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return StatusCode(500, new { Errors = new[] { "An error occurred while resetting your password." } });
            }
        }
    }
}
