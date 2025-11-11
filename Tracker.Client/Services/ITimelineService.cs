using System.Collections.Generic;
using System.Threading.Tasks;
using Tracker.Client.Models;
using Tracker.Shared.Models;

// Aliases for client models for clarity
using ClientTimelineEntry = Tracker.Client.Models.TimelineEntryDto;
using ClientCreateTimelineEntry = Tracker.Client.Models.CreateTimelineEntryDto;

namespace Tracker.Client.Services
{
    public interface ITimelineService
    {
        Task<IEnumerable<ClientTimelineEntry>> GetTimelineForIncidentAsync(string incidentId);
        Task<ClientTimelineEntry> AddTimelineEntryAsync(string incidentId, ClientCreateTimelineEntry entry);
        Task<bool> DeleteTimelineEntryAsync(string incidentId, string entryId);
    }
}
