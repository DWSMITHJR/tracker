using System;
using Tracker.Client.Models;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public static class DtoMappings
    {
        public static Tracker.Client.Models.IncidentDto? ToClientDto(this Tracker.Shared.Models.IncidentDto? sharedDto)
        {
            if (sharedDto == null) return null;

            return new Tracker.Client.Models.IncidentDto
            {
                Id = sharedDto.Id.ToString(),
                IncidentNumber = sharedDto.Id.ToString("N").Substring(0, 8).ToUpper(),
                Title = sharedDto.Title,
                Description = sharedDto.Description,
                Status = sharedDto.Status,
                Priority = sharedDto.Priority,
                AssignedTo = sharedDto.AssignedToName,
                ReportedBy = sharedDto.ReportedByName,
                CreatedAt = sharedDto.CreatedAt,
                UpdatedAt = sharedDto.UpdatedAt,
                ResolvedAt = sharedDto.ResolvedDate,
                Resolution = sharedDto.Resolution,
                OrganizationId = sharedDto.OrganizationId?.ToString(),
                // Organization name is not available in the shared DTO, so we'll use an empty string
                Tags = sharedDto.Tags?.ToArray() ?? Array.Empty<string>()
            };
        }

        public static Tracker.Shared.Models.IncidentDto? ToSharedDto(this Tracker.Client.Models.IncidentDto? clientDto)
        {
            if (clientDto == null) return null;

            Guid.TryParse(clientDto.Id, out var id);
            Guid.TryParse(clientDto.OrganizationId, out var orgId);

            return new Tracker.Shared.Models.IncidentDto
            {
                Id = id != Guid.Empty ? id : Guid.NewGuid(),
                Title = clientDto.Title,
                Description = clientDto.Description,
                Status = clientDto.Status,
                Priority = clientDto.Priority,
                AssignedToName = clientDto.AssignedTo,
                ReportedByName = clientDto.ReportedBy,
                CreatedAt = clientDto.CreatedAt,
                UpdatedAt = clientDto.UpdatedAt,
                ResolvedDate = clientDto.ResolvedAt,
                Resolution = clientDto.Resolution,
                IsActive = true,
                Tags = clientDto.Tags?.ToList() ?? new List<string>()
            };
        }
    }
}
