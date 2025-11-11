using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tracker.Infrastructure.Models
{
    public class Incident : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string? Description { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Open";

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = "Medium";

        // Organization relationship
        [Required]
        public Guid OrganizationId { get; set; }
        public virtual Organization Organization { get; set; } = null!;

        // Reported by relationship
        [Required]
        public Guid ReportedById { get; set; }
        public virtual Individual ReportedBy { get; set; } = null!;

        // Assigned to relationship
        public Guid? AssignedToId { get; set; }
        public virtual User? AssignedTo { get; set; }

        // Navigation properties
        public virtual ICollection<IncidentIndividual> InvolvedIndividuals { get; set; } = new List<IncidentIndividual>();
        public virtual ICollection<IncidentTimeline> Timeline { get; set; } = new List<IncidentTimeline>();
        public virtual ICollection<IncidentAttachment> Attachments { get; set; } = new List<IncidentAttachment>();
    }
}
