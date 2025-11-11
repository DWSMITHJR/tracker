using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracker.Infrastructure.Data;
using Tracker.Infrastructure.Models;

namespace Tracker.API.Controllers
{
    public class OrganizationsController : BaseController<Organization>
    {
        public OrganizationsController(ApplicationDbContext context, ILogger<OrganizationsController> logger)
            : base(context, logger)
        {
        }

        // GET: api/organizations
        [HttpGet]
        public async Task<IActionResult> GetOrganizations()
        {
            try
            {
                var organizations = await _context.Organizations
                    .Where(o => o.IsActive)
                    .ToListAsync();

                return Ok(organizations);
            }
            catch (Exception ex)
            {
                return HandleError("An error occurred while retrieving organizations.", ex);
            }
        }

        // GET: api/organizations/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrganization(Guid id)
        {
            try
            {
                var organization = await _context.Organizations
                    .Include(o => o.Users)
                    .Include(o => o.Individuals)
                    .Include(o => o.Contacts)
                    .Include(o => o.Incidents)
                    .Include(o => o.EnrollmentCodes)
                    .FirstOrDefaultAsync(o => o.Id == id && o.IsActive);

                if (organization == null)
                {
                    return NotFound();
                }

                // Check if the current user has access to this organization
                if (!await IsAuthorized(organization.Id))
                {
                    return Forbid();
                }

                return Ok(organization);
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while retrieving organization with ID {id}.", ex);
            }
        }

        // POST: api/organizations
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateOrganization([FromBody] Organization organization)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                organization.CreatedAt = DateTime.UtcNow;
                _context.Organizations.Add(organization);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetOrganization), new { id = organization.Id }, organization);
            }
            catch (Exception ex)
            {
                return HandleError("An error occurred while creating the organization.", ex);
            }
        }

        // PUT: api/organizations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrganization(Guid id, [FromBody] Organization organization)
        {
            try
            {
                if (id != organization.Id)
                {
                    return BadRequest("ID in the URL does not match the ID in the request body.");
                }

                // Check if the current user has access to this organization
                if (!await IsAuthorized(organization.Id))
                {
                    return Forbid();
                }

                var existingOrganization = await _context.Organizations.FindAsync(id);
                if (existingOrganization == null)
                {
                    return NotFound();
                }

                // Update only the allowed properties
                existingOrganization.Name = organization.Name;
                existingOrganization.Type = organization.Type;
                existingOrganization.Phone = organization.Phone;
                existingOrganization.Email = organization.Email;
                existingOrganization.Street = organization.Street;
                existingOrganization.City = organization.City;
                existingOrganization.State = organization.State;
                existingOrganization.ZipCode = organization.ZipCode;
                existingOrganization.Country = organization.Country;
                existingOrganization.UpdatedAt = DateTime.UtcNow;

                _context.Entry(existingOrganization).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await OrganizationExists(id))
                {
                    return NotFound();
                }
                else
                {
                    return HandleError("A concurrency error occurred while updating the organization.", ex);
                }
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while updating organization with ID {id}.", ex);
            }
        }

        // DELETE: api/organizations/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteOrganization(Guid id, [FromQuery] bool hardDelete = false)
        {
            try
            {
                var organization = await _context.Organizations
                    .Include(o => o.Users)
                    .Include(o => o.Individuals)
                    .Include(o => o.Contacts)
                    .Include(o => o.Incidents)
                    .Include(o => o.EnrollmentCodes)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (organization == null)
                {
                    return NotFound();
                }

                if (hardDelete)
                {
                    // Use hard delete with transaction
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // First, handle related entities that would cause foreign key violations
                        // 1. Remove users from the organization (doesn't delete users, just removes the relationship)
                        foreach (var user in organization.Users.ToList())
                        {
                            user.OrganizationId = null;
                        }

                        // 2. Delete related entities in the correct order to respect foreign key constraints
                        _context.IncidentAttachments.RemoveRange(
                            _context.IncidentAttachments
                                .Where(ia => _context.Incidents
                                    .Where(i => i.OrganizationId == organization.Id)
                                    .Select(i => i.Id)
                                    .Contains(ia.IncidentId))
                        );

                        _context.IncidentTimelines.RemoveRange(
                            _context.IncidentTimelines
                                .Where(it => _context.Incidents
                                    .Where(i => i.OrganizationId == organization.Id)
                                    .Select(i => i.Id)
                                    .Contains(it.IncidentId))
                        );

                        _context.IncidentIndividuals.RemoveRange(
                            _context.IncidentIndividuals
                                .Where(ii => _context.Incidents
                                    .Where(i => i.OrganizationId == organization.Id)
                                    .Select(i => i.Id)
                                    .Contains(ii.IncidentId))
                        );

                        // 3. Delete the main entities
                        _context.Incidents.RemoveRange(organization.Incidents);
                        _context.Individuals.RemoveRange(organization.Individuals);
                        _context.Contacts.RemoveRange(organization.Contacts);
                        _context.EnrollmentCodes.RemoveRange(organization.EnrollmentCodes);
                        
                        // 4. Finally, delete the organization
                        _context.Organizations.Remove(organization);
                        
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        
                        _logger.LogInformation("Organization {OrganizationId} and all related data have been hard deleted.", id);
                        return NoContent();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error occurred during hard delete of organization {OrganizationId}", id);
                        return HandleError($"An error occurred while hard deleting organization with ID {id}.", ex);
                    }
                }
                else
                {
                    // Soft delete
                    organization.IsActive = false;
                    organization.UpdatedAt = DateTime.UtcNow;

                    _context.Entry(organization).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Organization {OrganizationId} has been soft deleted.", id);
                    return NoContent();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting organization {OrganizationId}", id);
                return HandleError($"An error occurred while deleting organization with ID {id}.", ex);
            }
        }

        // POST: api/organizations/5/users
        [HttpPost("{id}/users/{userId}")]
        [Authorize(Roles = "Admin,OrganizationAdmin")]
        public async Task<IActionResult> AddUserToOrganization(Guid id, string userId)
        {
            try
            {
                // Check if the current user has access to this organization
                if (!await IsAuthorized(id) && CurrentUserRole != "Admin")
                {
                    return Forbid();
                }

                var organization = await _context.Organizations
                    .Include(o => o.Users)
                    .FirstOrDefaultAsync(o => o.Id == id && o.IsActive);

                if (organization == null)
                {
                    return NotFound("Organization not found.");
                }

                var user = await _context.Users.FindAsync(Guid.Parse(userId));
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                if (organization.Users.Any(u => u.Id == user.Id))
                {
                    return BadRequest("User is already a member of this organization.");
                }

                organization.Users.Add(user);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while adding user to organization with ID {id}.", ex);
            }
        }

        // DELETE: api/organizations/5/users/1
        [HttpDelete("{id}/users/{userId}")]
        [Authorize(Roles = "Admin,OrganizationAdmin")]
        public async Task<IActionResult> RemoveUserFromOrganization(Guid id, string userId)
        {
            try
            {
                // Check if the current user has access to this organization
                if (!await IsAuthorized(id) && CurrentUserRole != "Admin")
                {
                    return Forbid();
                }

                var organization = await _context.Organizations
                    .Include(o => o.Users)
                    .FirstOrDefaultAsync(o => o.Id == id && o.IsActive);

                if (organization == null)
                {
                    return NotFound("Organization not found.");
                }

                var user = organization.Users.FirstOrDefault(u => u.Id.ToString() == userId);
                if (user == null)
                {
                    return NotFound("User is not a member of this organization.");
                }

                organization.Users.Remove(user);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while removing user from organization with ID {id}.", ex);
            }
        }

        private async Task<bool> OrganizationExists(Guid id)
        {
            return await _context.Organizations.AnyAsync(e => e.Id == id && e.IsActive);
        }
    }
}
