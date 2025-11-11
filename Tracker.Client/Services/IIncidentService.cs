using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tracker.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tracker.Client.Services
{
    public interface IIncidentService
    {
        Task<IncidentDto?> GetIncidentByIdAsync(Guid id);
        Task<PagedResult<IncidentDto>> GetIncidentsAsync(int page = 1, int pageSize = 10, string? searchQuery = null, string? status = null);
        Task<IncidentDto?> CreateIncidentAsync(IncidentDto incident);
        Task<IncidentDto?> UpdateIncidentAsync(IncidentDto incident);
        Task<bool> UpdateIncidentStatusAsync(Guid incidentId, string status, string? comment = null);
        Task<bool> DeleteIncidentAsync(Guid id);
        Task<PagedResult<TimelineEntryDto>> GetTimelineEntriesAsync(Guid incidentId, int page = 1, int pageSize = 10);
        Task<IEnumerable<string>> GetIncidentStatusesAsync();
        Task<IEnumerable<string>> GetIncidentPrioritiesAsync();
    }
}
