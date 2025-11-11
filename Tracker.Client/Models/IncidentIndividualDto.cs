using System;

namespace Tracker.Client.Models
{
    public class IncidentIndividualDto
    {
        public string Id { get; set; } = string.Empty;
        public string IndividualId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }
}
