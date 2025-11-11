using System;
using System.Threading.Tasks;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public interface IOrganizationService
    {
        Task<PagedResult<OrganizationDto>> GetOrganizationsAsync(int page = 1, int pageSize = 10, string? searchTerm = null);
        Task<OrganizationDto?> GetOrganizationByIdAsync(Guid id);
        Task<OrganizationDto?> CreateOrganizationAsync(OrganizationDto organization);
        Task UpdateOrganizationAsync(Guid id, OrganizationDto organization);
        Task DeleteOrganizationAsync(Guid id);
    }
}
