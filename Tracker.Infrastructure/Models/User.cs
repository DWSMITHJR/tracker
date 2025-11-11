using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Tracker.Infrastructure.Models
{
    public class User : IdentityUser<Guid>
    {
        // Properties from BaseEntity that aren't in IdentityUser
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // User properties
        [Required]
        [MaxLength(100)]
        public required string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public required string LastName { get; set; }

        // Override base class properties to add attributes
        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public override string? Email { get; set; }

        // Make PhoneNumber optional
        [Phone]
        [MaxLength(20)]
        public override string? PhoneNumber { get; set; }

        // Override base class PasswordHash to match the base class implementation
        [PersonalData]
        public override string? PasswordHash { get; set; }

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Client";

        public bool IsActive { get; set; } = true;
        
        // Override base class properties
        public override int AccessFailedCount { get; set; }
        public override DateTimeOffset? LockoutEnd { get; set; }
        
        public DateTime? LastLogin { get; set; }
        // Navigation property for refresh tokens
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        
        // Legacy properties - keeping for backward compatibility
        [Obsolete("Use RefreshTokens navigation property instead")]
        public string? RefreshToken { get; set; }
        
        [Obsolete("Use RefreshTokens navigation property instead")]
        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Navigation properties
        public virtual ICollection<Organization> Organizations { get; set; } = new List<Organization>();
        public virtual ICollection<Incident> AssignedIncidents { get; set; } = new List<Incident>();
        
        // Primary organization for client users
        public Guid? OrganizationId { get; set; }
        public virtual Organization? Organization { get; set; }
    }
}
