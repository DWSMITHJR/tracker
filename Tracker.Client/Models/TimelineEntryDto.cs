using System;
using System.Collections.Generic;

namespace Tracker.Client.Models
{
    public class TimelineEntryDto
    {
        public string Id { get; set; } = string.Empty;
        public string IncidentId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Event { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UpdatedById { get; set; } = string.Empty;
        public string UpdatedByName { get; set; } = string.Empty;

        public static TimelineEntryDto FromSharedDto(Tracker.Shared.Models.TimelineEntryDto sharedDto)
        {
            if (sharedDto == null)
                throw new ArgumentNullException(nameof(sharedDto));

            return new TimelineEntryDto
            {
                Id = sharedDto.Id ?? string.Empty,
                IncidentId = sharedDto.IncidentId ?? string.Empty,
                Timestamp = sharedDto.Timestamp,
                Event = sharedDto.Event ?? string.Empty,
                Description = sharedDto.Description ?? string.Empty,
                UpdatedById = sharedDto.UpdatedById ?? string.Empty,
                UpdatedByName = sharedDto.UpdatedByName ?? string.Empty
            };
        }

        public Tracker.Shared.Models.TimelineEntryDto ToSharedDto()
        {
            return new Tracker.Shared.Models.TimelineEntryDto
            {
                Id = Id,
                IncidentId = IncidentId,
                Timestamp = Timestamp,
                Event = Event,
                Description = Description,
                UpdatedById = UpdatedById,
                UpdatedByName = UpdatedByName
            };
        }
    }
}
