using System;
using System.Threading.Tasks;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public interface IIndividualService
    {
        Task<PagedResult<IndividualDto>> GetIndividualsAsync(int page = 1, int pageSize = 10, string? searchTerm = null);
        Task<IndividualDto?> GetIndividualByIdAsync(Guid id);
        Task<IndividualDto?> CreateIndividualAsync(IndividualDto individual);
        Task UpdateIndividualAsync(Guid id, IndividualDto individual);
        Task DeleteIndividualAsync(Guid id);
    }
}
