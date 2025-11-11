using System;
using System.ComponentModel.DataAnnotations;

namespace Tracker.Client.Models
{
    public class CreateTimelineEntryDto
    {
        [Required]
        public string IncidentId { get; set; } = string.Empty;
        
        [Required]
        public string Event { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public string? UpdatedById { get; set; }
        public string? UpdatedByName { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
