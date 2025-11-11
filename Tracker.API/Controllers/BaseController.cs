using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Tracker.Infrastructure.Data;
using Tracker.Infrastructure.Models;

namespace Tracker.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController<T> : ControllerBase where T : BaseEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly ILogger<BaseController<T>> _logger;

        protected BaseController(ApplicationDbContext context, ILogger<BaseController<T>> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current user's ID from claims, or throws if not authenticated
        /// </summary>
        protected string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
            throw new UnauthorizedAccessException("User is not authenticated");
            
        /// <summary>
        /// Gets the current user's email from claims, or null if not available
        /// </summary>
        protected string? CurrentUserEmail => User.FindFirst(ClaimTypes.Email)?.Value;
        
        /// <summary>
        /// Gets the current user's role from claims, or null if not available
        /// </summary>
        protected string? CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value;

        protected async Task<bool> IsAuthorized(Guid organizationId)
        {
            if (string.IsNullOrEmpty(CurrentUserId) || !Guid.TryParse(CurrentUserId, out var userId))
            {
                return false;
            }

            // Admin users have access to all organizations
            if (CurrentUserRole == "Admin")
            {
                return true;
            }

            // Check if the user is a member of the organization
            var user = await _context.Users
                .Include(u => u.Organizations)
                .FirstOrDefaultAsync(u => u.Id == userId);
                
            if (user?.Organizations == null)
            {
                return false;
            }
                
            return user.Organizations.Any(o => o?.Id == organizationId);
        }

        /// <summary>
        /// Handles errors and returns an appropriate IActionResult
        /// </summary>
        /// <param name="message">The error message to return</param>
        /// <param name="ex">Optional exception that caused the error</param>
        /// <param name="statusCode">HTTP status code to return (default: 500)</param>
        /// <returns>An IActionResult with the error details</returns>
        protected IActionResult HandleError(string message, Exception? ex = null, int statusCode = 500)
        {
            _logger.LogError(ex, message);
            return StatusCode(statusCode, new { 
                Message = message, 
                Error = ex?.Message ?? "No additional error details available" 
            });
        }
    }
}
