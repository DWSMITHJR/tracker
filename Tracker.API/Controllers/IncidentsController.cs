using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracker.Infrastructure.Data;
using Tracker.Infrastructure.Models;

namespace Tracker.API.Controllers
{
    public class IncidentsController : BaseController<Incident>
    {
        public IncidentsController(ApplicationDbContext context, ILogger<IncidentsController> logger)
            : base(context, logger)
        {
        }

        // GET: api/organizations/{organizationId}/incidents
        [HttpGet("organizations/{organizationId}")]
        public async Task<IActionResult> GetIncidentsByOrganization(Guid organizationId, [FromQuery] string? status = null)
        {
            try
            {
                // Check if the current user has access to this organization
                if (!await IsAuthorized(organizationId))
                {
                    return Forbid();
                }

                var query = _context.Incidents
                    .Include(i => i.ReportedBy)
                    .Include(i => i.AssignedTo)
                    .Include(i => i.InvolvedIndividuals)
                        .ThenInclude(ii => ii.Individual)
                    .Where(i => i.OrganizationId == organizationId && i.IsActive);

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(i => i.Status == status);
                }

                var incidents = await query.ToListAsync();
                return Ok(incidents);
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while retrieving incidents for organization with ID {organizationId}.", ex);
            }
        }

        // GET: api/incidents/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetIncident(Guid id)
        {
            try
            {
                var incident = await _context.Incidents
                    .Include(i => i.Organization)
                    .Include(i => i.ReportedBy)
                    .Include(i => i.AssignedTo)
                    .Include(i => i.InvolvedIndividuals)
                        .ThenInclude(ii => ii.Individual)
                    .Include(i => i.Timeline)
                        .ThenInclude(t => t.UpdatedBy)
                    .Include(i => i.Attachments)
                        .ThenInclude(a => a.UploadedBy)
                    .FirstOrDefaultAsync(i => i.Id == id && i.IsActive);

                if (incident == null)
                {
                    return NotFound();
                }

                // Check if the current user has access to this incident's organization
                if (!await IsAuthorized(incident.OrganizationId))
                {
                    return Forbid();
                }

                return Ok(incident);
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while retrieving incident with ID {id}.", ex);
            }
        }

        // POST: api/incidents
        [HttpPost]
        public async Task<IActionResult> CreateIncident([FromBody] Incident incident)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if the current user has access to this organization
                if (!await IsAuthorized(incident.OrganizationId))
                {
                    return Forbid();
                }

                // Set the reported by ID to the current user's ID
                if (Guid.TryParse(CurrentUserId, out var currentUserId))
                {
                    incident.ReportedById = currentUserId;
                }
                else
                {
                    return Unauthorized("Invalid user ID in token");
                }

                // Set created timestamp
                incident.CreatedAt = DateTime.UtcNow;

                // Add initial timeline entry
                incident.Timeline.Add(new IncidentTimeline
                {
                    Event = "Incident Created",
                    Description = "The incident was created.",
                    UpdatedById = Guid.Parse(CurrentUserId),
                    Timestamp = DateTime.UtcNow
                });

                _context.Incidents.Add(incident);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetIncident), new { id = incident.Id }, incident);
            }
            catch (Exception ex)
            {
                return HandleError("An error occurred while creating the incident.", ex);
            }
        }

        // PUT: api/incidents/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIncident(Guid id, [FromBody] Incident incident)
        {
            try
            {
                if (id != incident.Id)
                {
                    return BadRequest("ID in the URL does not match the ID in the request body.");
                }

                var existingIncident = await _context.Incidents
                    .Include(i => i.Timeline)
                    .FirstOrDefaultAsync(i => i.Id == id && i.IsActive);

                if (existingIncident == null)
                {
                    return NotFound();
                }

                // Check if the current user has access to this incident's organization
                if (!await IsAuthorized(existingIncident.OrganizationId))
                {
                    return Forbid();
                }

                // Track changes for timeline
                var changes = new List<string>();
                if (existingIncident.Title != incident.Title) changes.Add($"Title changed from '{existingIncident.Title}' to '{incident.Title}'");
                if (existingIncident.Status != incident.Status) changes.Add($"Status changed from '{existingIncident.Status}' to '{incident.Status}'");
                if (existingIncident.Severity != incident.Severity) changes.Add($"Severity changed from '{existingIncident.Severity}' to '{incident.Severity}'");
                if (existingIncident.Description != incident.Description) changes.Add("Description was updated");

                // Update the incident
                existingIncident.Title = incident.Title;
                existingIncident.Description = incident.Description;
                existingIncident.Status = incident.Status;
                existingIncident.Severity = incident.Severity;
                existingIncident.AssignedToId = incident.AssignedToId;
                existingIncident.UpdatedAt = DateTime.UtcNow;

                // Add timeline entry if there are changes
                if (changes.Any())
                {
                    existingIncident.Timeline.Add(new IncidentTimeline
                    {
                        Event = "Incident Updated",
                        Description = string.Join(", ", changes),
                        UpdatedById = Guid.Parse(CurrentUserId),
                        Timestamp = DateTime.UtcNow
                    });
                }

                _context.Entry(existingIncident).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await IncidentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    return HandleError("A concurrency error occurred while updating the incident.", ex);
                }
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while updating incident with ID {id}.", ex);
            }
        }

        // DELETE: api/incidents/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,OrganizationAdmin")]
        public async Task<IActionResult> DeleteIncident(Guid id)
        {
            try
            {
                var incident = await _context.Incidents.FindAsync(id);
                if (incident == null)
                {
                    return NotFound();
                }

                // Check if the current user has access to this incident's organization
                if (!await IsAuthorized(incident.OrganizationId))
                {
                    return Forbid();
                }

                // Soft delete
                incident.IsActive = false;
                incident.UpdatedAt = DateTime.UtcNow;

                _context.Entry(incident).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while deleting incident with ID {id}.", ex);
            }
        }

        // POST: api/incidents/5/timeline
        [HttpPost("{id}/timeline")]
        public async Task<IActionResult> AddTimelineEntry(Guid id, [FromBody] TimelineEntryRequest request)
        {
            try
            {
                var incident = await _context.Incidents.FindAsync(id);
                if (incident == null)
                {
                    return NotFound("Incident not found.");
                }

                // Check if the current user has access to this incident's organization
                if (!await IsAuthorized(incident.OrganizationId))
                {
                    return Forbid();
                }

                var timelineEntry = new IncidentTimeline
                {
                    IncidentId = id,
                    Event = request.Event,
                    Description = request.Description,
                    UpdatedById = Guid.Parse(CurrentUserId),
                    Timestamp = DateTime.UtcNow
                };

                _context.IncidentTimelines.Add(timelineEntry);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetIncident), new { id = incident.Id }, timelineEntry);
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while adding a timeline entry to incident with ID {id}.", ex);
            }
        }

        // POST: api/incidents/5/assign/1
        [HttpPost("{id}/assign/{userId}")]
        public async Task<IActionResult> AssignIncident(Guid id, string userId)
        {
            try
            {
                var incident = await _context.Incidents.FindAsync(id);
                if (incident == null)
                {
                    return NotFound("Incident not found.");
                }

                // Check if the current user has access to this incident's organization
                if (!await IsAuthorized(incident.OrganizationId))
                {
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(Guid.Parse(userId));
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                // Check if the user is a member of the organization
                var isUserInOrganization = await _context.Organizations
                    .Include(o => o.Users)
                    .AnyAsync(o => o.Id == incident.OrganizationId && o.Users.Any(u => u.Id == user.Id));

                if (!isUserInOrganization)
                {
                    return BadRequest("The specified user is not a member of the incident's organization.");
                }

                var previousAssigneeId = incident.AssignedToId;
                incident.AssignedToId = user.Id;
                incident.UpdatedAt = DateTime.UtcNow;

                // Add timeline entry
                var timelineEntry = new IncidentTimeline
                {
                    IncidentId = id,
                    Event = "Incident Assigned",
                    Description = $"Incident was assigned to {user.FirstName} {user.LastName}.",
                    UpdatedById = Guid.Parse(CurrentUserId),
                    Timestamp = DateTime.UtcNow
                };

                _context.IncidentTimelines.Add(timelineEntry);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while assigning incident with ID {id} to user with ID {userId}.", ex);
            }
        }

        private async Task<bool> IncidentExists(Guid id)
        {
            return await _context.Incidents.AnyAsync(e => e.Id == id && e.IsActive);
        }
    }

    public class TimelineEntryRequest
    {
        public required string Event { get; set; }
        public required string Description { get; set; }
    }
}
