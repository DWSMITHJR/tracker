using System.ComponentModel.DataAnnotations;

namespace Tracker.Shared.Auth;

/// <summary>
/// Represents a user registration request
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Gets or sets the user's first name
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    public required string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    public required string LastName { get; set; }

    /// <summary>
    /// Gets or sets the user's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the user's password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public required string Password { get; set; }

    /// <summary>
    /// Gets or sets the password confirmation (must match Password)
    /// </summary>
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public required string ConfirmPassword { get; set; }

    /// <summary>
    /// Gets or sets the name of the organization (optional for new organizations)
    /// </summary>
    public string? OrganizationName { get; set; }
    
    /// <summary>
    /// Gets or sets the enrollment code for joining an existing organization (optional)
    /// </summary>
    public string? EnrollmentCode { get; set; }
}

/// <summary>
/// Represents a user login request
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Gets or sets the user's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the user's password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public required string Password { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the login should persist across browser sessions
    /// </summary>
    public bool RememberMe { get; set; }
}

/// <summary>
/// Represents the result of a successful login operation
/// </summary>
public class LoginResult
{
    /// <summary>
    /// Gets or sets the JWT bearer token for authentication
    /// </summary>
    public required string Token { get; set; }
    
    /// <summary>
    /// Gets or sets the refresh token for obtaining new access tokens
    /// </summary>
    public required string RefreshToken { get; set; }
    
    /// <summary>
    /// Gets or sets the token expiration date and time
    /// </summary>
    public DateTime Expiration { get; set; }
    
    /// <summary>
    /// Gets or sets the user's unique identifier
    /// </summary>
    public required string UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the user's email address
    /// </summary>
    public required string Email { get; set; }
    
    /// <summary>
    /// Gets or sets the user's first name
    /// </summary>
    public required string FirstName { get; set; }
    
    /// <summary>
    /// Gets or sets the user's last name
    /// </summary>
    public required string LastName { get; set; }
    
    /// <summary>
    /// Gets or sets the user's role
    /// </summary>
    public required string Role { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the organization the user belongs to
    /// </summary>
    public required string OrganizationId { get; set; }
}

/// <summary>
/// Represents a request to refresh an authentication token
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Gets or sets the expired JWT token
    /// </summary>
    public required string Token { get; set; }
    
    /// <summary>
    /// Gets or sets the refresh token
    /// </summary>
    public required string RefreshToken { get; set; }
}

/// <summary>
/// Represents a request to initiate password reset
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>
    /// Gets or sets the email address for password reset
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public required string Email { get; set; }
}

/// <summary>
/// Represents a request to reset a user's password
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// Gets or sets the user's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public required string Email { get; set; }

    /// <summary>
    /// Gets or sets the new password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public required string Password { get; set; }

    /// <summary>
    /// Gets or sets the password confirmation (must match Password)
    /// </summary>
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public required string ConfirmPassword { get; set; }

    /// <summary>
    /// Gets or sets the password reset token
    /// </summary>
    [Required(ErrorMessage = "Token is required")]
    public required string Token { get; set; }
}

/// <summary>
/// Represents a request to change a user's password
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Gets or sets the user's current password
    /// </summary>
    [Required(ErrorMessage = "Current password is required")]
    [DataType(DataType.Password)]
    public required string CurrentPassword { get; set; }

    /// <summary>
    /// Gets or sets the new password
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public required string NewPassword { get; set; }

    /// <summary>
    /// Gets or sets the new password confirmation (must match NewPassword)
    /// </summary>
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public required string ConfirmNewPassword { get; set; }
}

/// <summary>
/// Represents a user profile with account and personal information
/// </summary>
public class UserProfile
{
    /// <summary>
    /// Gets or sets the unique identifier for the user
    /// </summary>
    public required string Id { get; set; }
    
    /// <summary>
    /// Gets or sets the user's first name
    /// </summary>
    public required string FirstName { get; set; }
    
    /// <summary>
    /// Gets or sets the user's last name
    /// </summary>
    public required string LastName { get; set; }
    
    /// <summary>
    /// Gets or sets the user's email address
    /// </summary>
    public required string Email { get; set; }
    
    /// <summary>
    /// Gets or sets the user's phone number
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Gets or sets the user's role
    /// </summary>
    public required string Role { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the organization the user belongs to
    /// </summary>
    public string? OrganizationId { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the organization the user belongs to
    /// </summary>
    public string? OrganizationName { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the user's email has been confirmed
    /// </summary>
    public bool EmailConfirmed { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether two-factor authentication is enabled for the user
    /// </summary>
    public bool TwoFactorEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the user can be locked out
    /// </summary>
    public bool LockoutEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the user's lockout ends, if applicable
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; set; }
    
    /// <summary>
    /// Gets or sets the number of failed login attempts for the user
    /// </summary>
    public int AccessFailedCount { get; set; }
}
