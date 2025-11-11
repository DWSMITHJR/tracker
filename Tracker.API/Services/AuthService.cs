using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tracker.Infrastructure.Data;
using Tracker.Infrastructure.Models;
using Tracker.Shared.Auth;

namespace Tracker.API.Services
{
    /// <summary>
    /// Service responsible for handling authentication and authorization operations.
    /// </summary>
    /// <summary>
    /// Service responsible for handling authentication and authorization operations.
    /// </summary>
    public class AuthService : IAuthService
    {
        private const string TokenAlgorithm = SecurityAlgorithms.HmacSha256;
        private const string DefaultUserRole = "User";
        private const string LocalhostIp = "127.0.0.1";
        private const int MaxFailedLoginAttempts = 5;
        private const int LoginLockoutMinutes = 15;
        private const string EmailRegexPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        
        private readonly UserManager<User> _userManager;
        private readonly IOptions<JwtSettings> _jwtSettingsOptions;
        private readonly JwtSettings _jwtSettings;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly IMemoryCache _memoryCache;

        public AuthService(
            UserManager<User> userManager,
            IOptions<JwtSettings> jwtSettings,
            ApplicationDbContext context,
            ILogger<AuthService> logger,
            IHttpContextAccessor? httpContextAccessor = null,
            IMemoryCache? memoryCache = null)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _jwtSettingsOptions = jwtSettings ?? throw new ArgumentNullException(nameof(jwtSettings));
            _jwtSettings = _jwtSettingsOptions.Value ?? throw new InvalidOperationException("JWT settings are not configured");
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor;
            _memoryCache = memoryCache ?? new MemoryCache(new MemoryCacheOptions());
            
            // Validate JWT settings
            if (string.IsNullOrWhiteSpace(_jwtSettings.Secret))
                throw new InvalidOperationException("JWT Secret is not configured");
                
            if (string.IsNullOrWhiteSpace(_jwtSettings.Issuer))
                throw new InvalidOperationException("JWT Issuer is not configured");
                
            if (string.IsNullOrWhiteSpace(_jwtSettings.Audience))
                throw new InvalidOperationException("JWT Audience is not configured");
        }

        // Debug methods to check JWT settings
        public string? GetJwtSecretForDebug() => _jwtSettings.Secret;
        public string? GetJwtIssuerForDebug() => _jwtSettings.Issuer;
        public string? GetJwtAudienceForDebug() => _jwtSettings.Audience;

        /// <summary>
        /// Registers a new user with the specified details.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="password">User's password</param>
        /// <param name="firstName">User's first name</param>
        /// <param name="lastName">User's last name</param>
        /// <param name="role">User's role</param>
        /// <returns>AuthResult indicating success or failure</returns>
        public async Task<AuthResult> RegisterAsync(string email, string password, string firstName, string lastName, string role)
        {
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                return new AuthResult { Success = false, Errors = new[] { "Invalid email address" } };
                
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return new AuthResult { Success = false, Errors = new[] { "Password must be at least 8 characters long" } };
                
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                return new AuthResult { Success = false, Errors = new[] { "First name and last name are required" } };
                
            if (string.IsNullOrWhiteSpace(role))
                role = DefaultUserRole;

            _logger.LogInformation("Registering new user with email: {Email}", email);

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = new[] { "User with this email already exists" }
                };
            }

            // Create new user
            var newUser = new User
            {
                Email = email,
                UserName = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true // You might want to implement email confirmation
            };

            var createUserResult = await _userManager.CreateAsync(newUser, password);
            if (!createUserResult.Succeeded)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = createUserResult.Errors.Select(e => e.Description)
                };
            }

            // Add user to role
            var addToRoleResult = await _userManager.AddToRoleAsync(newUser, role);
            if (!addToRoleResult.Succeeded)
            {
                // If adding to role fails, delete the user to maintain consistency
                await _userManager.DeleteAsync(newUser);
                return new AuthResult
                {
                    Success = false,
                    Errors = addToRoleResult.Errors.Select(e => e.Description)
                };
            }

            // Generate tokens
            var token = await GenerateJwtTokenAsync(newUser);
            var refreshToken = GenerateRefreshToken();
            
            // Save refresh token
            await SaveRefreshTokenAsync(newUser.Id, refreshToken);

            return new AuthResult
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                UserId = newUser.Id.ToString(),
                Email = newUser.Email,
                FirstName = newUser.FirstName,
                LastName = newUser.LastName,
                Role = role
            };
        }

        /// <summary>
        /// Authenticates a user and generates JWT and refresh tokens.
        /// </summary>
        /// <param name="email">User's email</param>
        /// <param name="password">User's password</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>AuthResult with tokens if successful</returns>
        public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Login attempt started for: {Email}", email);
            
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                _logger.LogWarning("Invalid email format: {Email}", email);
                return new AuthResult { Success = false, Errors = new[] { "Invalid email or password" } };
            }
                
            if (string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Empty password for email: {Email}", email);
                return new AuthResult { Success = false, Errors = new[] { "Password is required" } };
            }

            var ipAddress = GetClientIpAddress();
            var cacheKey = $"login_attempts_{ipAddress}";
            _logger.LogDebug("IP Address: {IpAddress}, CacheKey: {CacheKey}", ipAddress, cacheKey);
            
            // Check for too many failed attempts
            try 
            {
                if (_memoryCache.TryGetValue(cacheKey, out int attempts) && attempts >= MaxFailedLoginAttempts)
                {
                    _logger.LogWarning("Too many login attempts from IP: {IpAddress}, Attempts: {Attempts}", ipAddress, attempts);
                    return new AuthResult 
                    { 
                        Success = false, 
                        Errors = new[] { $"Too many failed attempts. Please try again in {LoginLockoutMinutes} minutes." } 
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking login attempts for IP: {IpAddress}", ipAddress);
                throw;
            }

            _logger.LogInformation("Looking up user in database: {Email}", email);
        
            User? user = null;
            try
            {
                user = await _userManager.FindByEmailAsync(email);
                _logger.LogDebug("Database query completed for user: {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error when finding user: {Email}", email);
                throw;
            }
            if (user == null)
            {
                await TrackFailedLoginAttempt(cacheKey);
                _logger.LogWarning("Login failed: User {Email} not found", email);
                return new AuthResult { Success = false, Errors = new[] { "Invalid email or password" } };
            }

            // Check if account is locked out
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                var remainingTime = user.LockoutEnd.Value - DateTimeOffset.UtcNow;
                return new AuthResult 
                { 
                    Success = false, 
                    Errors = new[] { $"Account is locked out. Please try again in {remainingTime.Minutes} minutes." } 
                };
            }

            bool isPasswordValid;
            try
            {
                _logger.LogDebug("Validating password for user: {Email}", email);
                isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
                _logger.LogDebug("Password validation completed for user: {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating password for user: {Email}", email);
                throw;
            }
        
            if (!isPasswordValid)
            {    
                await _userManager.AccessFailedAsync(user);
                await TrackFailedLoginAttempt(cacheKey);
                
                var remainingAttempts = MaxFailedLoginAttempts - (user.AccessFailedCount + 1);
                _logger.LogWarning("Invalid password for user: {Email}. Remaining attempts: {RemainingAttempts}", 
                    email, remainingAttempts);
                    
                return new AuthResult 
                { 
                    Success = false, 
                    Errors = new[] { $"Invalid email or password. {remainingAttempts} attempts remaining." } 
                };
            }
            
            // Reset failed attempts on successful login
            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.SetLockoutEndDateAsync(user, null);
            _memoryCache.Remove(cacheKey);

            // Generate tokens
            _logger.LogDebug("Generating JWT token for user: {Email}", email);
            var token = await GenerateJwtTokenAsync(user);
            _logger.LogDebug("JWT token generated for user: {Email}", email);
        
            var refreshToken = GenerateRefreshToken();
            _logger.LogDebug("Refresh token generated for user: {Email}", email);
        
            // Save refresh token
            try
            {
                _logger.LogDebug("Saving refresh token for user: {Email}", email);
                await SaveRefreshTokenAsync(user.Id, refreshToken);
                _logger.LogDebug("Refresh token saved for user: {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving refresh token for user: {Email}", email);
                throw;
            }

            _logger.LogDebug("Getting roles for user: {Email}", email);
            var roles = await _userManager.GetRolesAsync(user);
            _logger.LogDebug("Retrieved {RoleCount} roles for user: {Email}", roles.Count, email);
            var userRole = roles.FirstOrDefault() ?? "User";

            return new AuthResult
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                UserId = user.Id.ToString(),
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Role = userRole
            };
        }

        public async Task Logout()
        {
            _logger.LogInformation("[AuthService] Logout called");
            // In a real implementation, you might want to invalidate the current token
            // or perform other cleanup tasks
            await Task.CompletedTask;
        }

        public async Task<AuthResult> RefreshTokenAsync(string token, string refreshToken)
        {
            _logger.LogInformation("[AuthService] Refreshing token");

            var principal = GetPrincipalFromExpiredToken(token);
            if (principal?.Identity?.Name == null)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = new[] { "Invalid token" }
                };
            }

            var user = await _userManager.FindByEmailAsync(principal.Identity.Name);
            if (user == null)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = new[] { "User not found" }
                };
            }

            // Verify refresh token
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == user.Id && rt.Token == refreshToken && !rt.IsRevoked);

            if (storedToken == null || storedToken.Expires < DateTime.UtcNow)
            {
                return new AuthResult
                {
                    Success = false,
                    Errors = new[] { "Invalid refresh token" }
                };
            }

            // Generate new tokens
            var newToken = await GenerateJwtTokenAsync(user);
            var newRefreshToken = GenerateRefreshToken();

            // Mark old refresh token as revoked
            storedToken.Revoked = DateTime.UtcNow;
            storedToken.ReasonRevoked = "Replaced by new token";
            _context.RefreshTokens.Update(storedToken);

            // Save the new refresh token
            await SaveRefreshTokenAsync(user.Id, newRefreshToken);
            await _context.SaveChangesAsync();

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "User";

            return new AuthResult
            {
                Success = true,
                Token = newToken,
                RefreshToken = newRefreshToken,
                UserId = user.Id.ToString(),
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Role = userRole
            };
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            _logger.LogInformation("[AuthService] Revoking token");

            var principal = GetPrincipalFromExpiredToken(token);
            if (principal?.Identity?.Name == null)
            {
                return false;
            }

            var user = await _userManager.FindByEmailAsync(principal.Identity.Name);
            if (user == null)
            {
                return false;
            }

            // Revoke all refresh tokens for this user
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToListAsync();

            foreach (var refreshToken in refreshTokens)
            {
                refreshToken.Revoked = DateTime.UtcNow;
                refreshToken.ReasonRevoked = "Revoked by user";
            }

            _context.RefreshTokens.UpdateRange(refreshTokens);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<string> GeneratePasswordResetTokenAsync(string email)
        {
            _logger.LogInformation($"[AuthService] Generating password reset token for: {email}");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                return string.Empty;
            }

            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            _logger.LogInformation($"[AuthService] Resetting password for: {email}");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                return true;
            }

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded;
        }

        /// <summary>
        /// Generates a JWT token for the specified user
        /// </summary>
        private async Task<string> GenerateJwtTokenAsync(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret ?? throw new InvalidOperationException("JWT Secret is not configured"));
            
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Email ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Add roles as claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    TokenAlgorithm)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Generates a cryptographically secure random refresh token.
        /// </summary>
        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        /// <summary>
        /// Gets the principal from an expired JWT token
        /// </summary>
        /// <summary>
        /// Extracts the principal from an expired JWT token.
        /// </summary>
        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Token is null or whitespace");
                return null;
            }

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.Secret ?? throw new InvalidOperationException("JWT Secret is not configured"))),
                ValidateLifetime = false, // We want to allow expired tokens for refresh
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                ClockSkew = TimeSpan.Zero // No tolerance for the expiration date
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                
                if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                    !jwtSecurityToken.Header.Alg.Equals(TokenAlgorithm, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("Invalid token - invalid algorithm");
                    return null;
                }

                return principal;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogError(ex, "Security token validation failed");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error validating token");
                return null;
            }
        }

        /// <summary>
        /// Saves a refresh token to the database
        /// </summary>
        /// <summary>
        /// Saves a refresh token to the database.
        /// </summary>
        private async Task SaveRefreshTokenAsync(Guid userId, string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(refreshToken));
                
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            var ipAddress = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? LocalhostIp;
            
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = refreshToken,
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();
        }
        #region Helper Methods
        
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
                
            try 
            {
                return Regex.IsMatch(email, EmailRegexPattern, 
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
        
        private string? GetClientIpAddress()
        {
            try
            {
                return _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            }
            catch
            {
                return LocalhostIp;
            }
        }
        
        private async Task TrackFailedLoginAttempt(string cacheKey)
        {
            var options = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(LoginLockoutMinutes));
                
            var attempts = _memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.SetOptions(options);
                return 0;
            });
            
            _memoryCache.Set(cacheKey, attempts + 1, options);
            await Task.CompletedTask;
        }
        
        #endregion
    }
}
