using System;
using System.ComponentModel.DataAnnotations;

namespace Tracker.Infrastructure.Models
{
    public class Contact : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(100)]
        public string? Position { get; set; }

        public bool IsPrimary { get; set; }

        [MaxLength(20)]
        public string? PreferredContactMethod { get; set; } = "email";

        // Organization relationship
        [Required]
        public Guid OrganizationId { get; set; }
        public virtual Organization Organization { get; set; } = null!;
    }
}
