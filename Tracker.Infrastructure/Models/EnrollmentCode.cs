using System;
using System.ComponentModel.DataAnnotations;

namespace Tracker.Infrastructure.Models
{
    public class EnrollmentCode : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        // Organization relationship
        [Required]
        public Guid OrganizationId { get; set; }
        public virtual Organization Organization { get; set; } = null!;

        [Required]
        public DateTime BeginDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime EndDate { get; set; }

        public new bool IsActive { get; set; } = true;
        public bool Used { get; set; }
        public DateTime? UsedAt { get; set; }

        // Used by relationship
        public Guid? UsedById { get; set; }
        public virtual User? UsedBy { get; set; }
    }
}
