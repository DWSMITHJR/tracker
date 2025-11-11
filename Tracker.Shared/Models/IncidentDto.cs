using System;
using System.Collections.Generic;

namespace Tracker.Shared.Models
{
    public class IncidentDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "Open";
        public string? Type { get; set; }
        public string? Priority { get; set; }
        public DateTime? ReportedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public Guid? ReportedById { get; set; }
        public string? ReportedByName { get; set; }
        public Guid? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public string? Location { get; set; }
        public string? Resolution { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public Guid? OrganizationId { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<IncidentIndividualDto> InvolvedIndividuals { get; set; } = new();
    }

    public class IncidentIndividualDto
    {
        public Guid IndividualId { get; set; }
        public string IndividualName { get; set; } = string.Empty;
        public string? Role { get; set; }
        public string? Notes { get; set; }
    }
}
