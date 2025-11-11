using System;
using System.Collections.Generic;
using System.Linq;
using Tracker.Client.Models;
using Tracker.Shared.Models;

// Aliases for shared models to avoid ambiguity
using SharedIncident = Tracker.Shared.Models.IncidentDto;
using SharedIncidentIndividualDto = Tracker.Shared.Models.IncidentIndividualDto;

// Aliases for client models for clarity
using ClientIncident = Tracker.Client.Models.IncidentDto;
using ClientIncidentIndividualDto = Tracker.Client.Models.IncidentIndividualDto;

namespace Tracker.Client.Extensions
{
    public static class IncidentDtoExtensions
    {
        public static ClientIncident ToClientIncident(this SharedIncident sharedDto)
        {
            if (sharedDto == null)
                throw new ArgumentNullException(nameof(sharedDto));

            var clientIncident = new ClientIncident
            {
                Id = sharedDto.Id.ToString(),
                Title = sharedDto.Title ?? string.Empty,
                Description = sharedDto.Description ?? string.Empty,
                Status = sharedDto.Status ?? "Open",
                Type = sharedDto.Type ?? string.Empty,
                Priority = sharedDto.Priority ?? "Medium",
                ReportedDate = sharedDto.ReportedDate,
                ResolvedDate = sharedDto.ResolvedDate,
                AssignedToId = sharedDto.AssignedToId?.ToString() ?? string.Empty,
                AssignedToName = sharedDto.AssignedToName ?? string.Empty,
                AssignedTo = sharedDto.AssignedToName ?? string.Empty, // For backward compatibility
                ReportedById = sharedDto.ReportedById?.ToString() ?? string.Empty,
                ReportedByName = sharedDto.ReportedByName ?? string.Empty,
                ReportedBy = sharedDto.ReportedByName ?? string.Empty, // For backward compatibility
                Location = sharedDto.Location ?? string.Empty,
                Resolution = sharedDto.Resolution ?? string.Empty,
                IsActive = sharedDto.IsActive,
                CreatedAt = sharedDto.CreatedAt,
                UpdatedAt = sharedDto.UpdatedAt,
                ResolvedAt = sharedDto.ResolvedDate, // For backward compatibility
                OrganizationId = sharedDto.OrganizationId?.ToString() ?? string.Empty,
                InvolvedIndividuals = new List<ClientIncidentIndividualDto>()
            };

            // Map involved individuals if they exist
            if (sharedDto.InvolvedIndividuals != null)
            {
                foreach (var individual in sharedDto.InvolvedIndividuals)
                {
                    clientIncident.InvolvedIndividuals.Add(new ClientIncidentIndividualDto
                    {
                        IndividualId = individual.IndividualId.ToString(),
                        Name = individual.IndividualName ?? string.Empty,
                        Role = individual.Role ?? string.Empty,
                        IsPrimary = false // Default value since it's not in the shared model
                    });
                }
            }

            return clientIncident;
        }

        public static SharedIncident ToSharedDto(this ClientIncident clientDto)
        {
            if (clientDto == null)
                throw new ArgumentNullException(nameof(clientDto));

            var sharedIncident = new SharedIncident
            {
                Id = string.IsNullOrEmpty(clientDto.Id) ? Guid.NewGuid() : Guid.Parse(clientDto.Id),
                Title = clientDto.Title,
                Description = clientDto.Description,
                Status = clientDto.Status,
                Type = clientDto.Type,
                Priority = clientDto.Priority,
                ReportedDate = clientDto.ReportedDate,
                ResolvedDate = clientDto.ResolvedDate,
                AssignedToId = string.IsNullOrEmpty(clientDto.AssignedToId) ? null : Guid.Parse(clientDto.AssignedToId),
                AssignedToName = clientDto.AssignedToName,
                ReportedById = string.IsNullOrEmpty(clientDto.ReportedById) ? null : Guid.Parse(clientDto.ReportedById),
                ReportedByName = clientDto.ReportedByName,
                Location = clientDto.Location,
                Resolution = clientDto.Resolution,
                IsActive = clientDto.IsActive,
                CreatedAt = clientDto.CreatedAt,
                UpdatedAt = clientDto.UpdatedAt,
                InvolvedIndividuals = new List<SharedIncidentIndividualDto>()
            };

            // Add organization ID if it exists
            if (!string.IsNullOrEmpty(clientDto.OrganizationId) && Guid.TryParse(clientDto.OrganizationId, out var orgId))
            {
                sharedIncident.OrganizationId = orgId;
            }

            // Map involved individuals if they exist
            if (clientDto.InvolvedIndividuals != null)
            {
                foreach (var individual in clientDto.InvolvedIndividuals)
                {
                    if (string.IsNullOrEmpty(individual.IndividualId)) continue;

                    sharedIncident.InvolvedIndividuals.Add(new SharedIncidentIndividualDto
                    {
                        IndividualId = Guid.Parse(individual.IndividualId),
                        IndividualName = individual.Name,
                        Role = individual.Role,
                        Notes = null // Notes is not in the client model
                    });
                }
            }

            return sharedIncident;
        }
    }
}
