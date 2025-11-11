using Microsoft.AspNetCore.Identity;

namespace Tracker.Infrastructure.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        
        // Add any additional properties for your user here
        // Example:
        // public string? ProfilePictureUrl { get; set; }
        // public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // public bool IsActive { get; set; } = true;
    }
}
