using System;
using System.ComponentModel.DataAnnotations;

namespace Tracker.Shared.Models
{
    public class CreateIncidentDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(10000, ErrorMessage = "Description is too long")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = "Open";

        public string? Type { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        public string Priority { get; set; } = "Medium";

        public DateTime? ReportedDate { get; set; } = DateTime.UtcNow;
        public Guid? ReportedById { get; set; }
        public string? ReportedByName { get; set; }
        public Guid? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public string? Location { get; set; }
        public string? Resolution { get; set; }
        public bool IsActive { get; set; } = true;
        public List<IncidentIndividualDto> InvolvedIndividuals { get; set; } = new();
    }
}
