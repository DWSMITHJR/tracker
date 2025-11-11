using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tracker.Shared.Models;

namespace Tracker.Client.Models
{
    public class IncidentDto
    {
        public string Id { get; set; } = string.Empty;
        public string IncidentNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "Open";
        public string? Type { get; set; }
        public string? Priority { get; set; }
        public DateTime? ReportedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public string? AssignedTo { get; set; } // For backward compatibility with existing code
        public string? ReportedById { get; set; }
        public string? ReportedByName { get; set; }
        public string? ReportedBy { get; set; } // For backward compatibility with existing code
        public string? Location { get; set; }
        public string? Resolution { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; } // For backward compatibility with existing code
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string? OrganizationId { get; set; } // For backward compatibility with existing code
        public List<IncidentIndividualDto> InvolvedIndividuals { get; set; } = new();

        public Tracker.Shared.Models.IncidentDto ToSharedDto()
        {
            return new Tracker.Shared.Models.IncidentDto
            {
                Id = string.IsNullOrEmpty(Id) ? Guid.NewGuid() : Guid.Parse(Id),
                Title = Title,
                Description = Description,
                Status = Status,
                Type = Type,
                Priority = Priority,
                ReportedDate = ReportedDate,
                ResolvedDate = ResolvedDate,
                AssignedToId = string.IsNullOrEmpty(AssignedToId) ? null : Guid.Parse(AssignedToId),
                AssignedToName = AssignedToName,
                ReportedById = string.IsNullOrEmpty(ReportedById) ? null : Guid.Parse(ReportedById),
                ReportedByName = ReportedByName,
                Location = Location,
                Resolution = Resolution,
                IsActive = IsActive,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                OrganizationId = string.IsNullOrEmpty(OrganizationId) ? null : Guid.Parse(OrganizationId),
                InvolvedIndividuals = InvolvedIndividuals.Select(i => new Tracker.Shared.Models.IncidentIndividualDto
                {
                    IndividualId = string.IsNullOrEmpty(i.IndividualId) ? Guid.Empty : Guid.Parse(i.IndividualId),
                    IndividualName = i.Name,
                    Role = i.Role,
                    Notes = string.Empty // No direct mapping in client DTO
                }).ToList()
            };
        }

        public static IncidentDto FromSharedDto(Tracker.Shared.Models.IncidentDto dto)
        {
            return new IncidentDto
            {
                Id = dto.Id.ToString(),
                IncidentNumber = dto.Id.ToString("N").Substring(0, 8).ToUpper(),
                Title = dto.Title,
                Description = dto.Description,
                Status = dto.Status,
                Type = dto.Type,
                Priority = dto.Priority,
                ReportedDate = dto.ReportedDate,
                ResolvedDate = dto.ResolvedDate,
                AssignedToId = dto.AssignedToId?.ToString(),
                AssignedToName = dto.AssignedToName,
                ReportedById = dto.ReportedById?.ToString(),
                ReportedByName = dto.ReportedByName,
                Location = dto.Location,
                Resolution = dto.Resolution,
                IsActive = dto.IsActive,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                OrganizationId = dto.OrganizationId?.ToString(),
                InvolvedIndividuals = dto.InvolvedIndividuals.Select(i => new IncidentIndividualDto
                {
                    Id = i.IndividualId.ToString(),
                    IndividualId = i.IndividualId.ToString(),
                    Name = i.IndividualName,
                    Role = i.Role ?? string.Empty,
                    IsPrimary = false // No direct mapping in shared DTO
                }).ToList()
            };
        }
    }
}
