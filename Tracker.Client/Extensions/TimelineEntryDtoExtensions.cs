using System;
using Tracker.Client.Models;
using Tracker.Shared.Models;

// Aliases for shared models to avoid ambiguity
using SharedTimelineEntry = Tracker.Shared.Models.TimelineEntryDto;

// Aliases for client models for clarity
using ClientTimelineEntry = Tracker.Client.Models.TimelineEntryDto;

namespace Tracker.Client.Extensions
{
    public static class TimelineEntryDtoExtensions
    {
        public static ClientTimelineEntry ToClientTimelineEntry(this SharedTimelineEntry sharedDto)
        {
            if (sharedDto == null)
                throw new ArgumentNullException(nameof(sharedDto));

            return new ClientTimelineEntry
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

        public static SharedTimelineEntry ToSharedDto(this ClientTimelineEntry clientDto)
        {
            if (clientDto == null)
                throw new ArgumentNullException(nameof(clientDto));

            return new SharedTimelineEntry
            {
                Id = clientDto.Id,
                IncidentId = clientDto.IncidentId,
                Timestamp = clientDto.Timestamp,
                Event = clientDto.Event,
                Description = clientDto.Description,
                UpdatedById = clientDto.UpdatedById,
                UpdatedByName = clientDto.UpdatedByName
            };
        }
    }
}
