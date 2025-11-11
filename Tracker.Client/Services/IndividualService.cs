using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public class IndividualService : IIndividualService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IndividualService> _logger;

        public IndividualService(HttpClient httpClient, ILogger<IndividualService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PagedResult<IndividualDto>> GetIndividualsAsync(int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            try
            {
                var url = $"api/individuals?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    url += $"&search={Uri.EscapeDataString(searchTerm)}";
                }
                
                var result = await _httpClient.GetFromJsonAsync<PagedResult<IndividualDto>>(url);
                return result ?? new PagedResult<IndividualDto> { Items = new List<IndividualDto>(), TotalCount = 0 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching individuals");
                return new PagedResult<IndividualDto> { Items = new List<IndividualDto>(), TotalCount = 0 };
            }
        }

        public async Task<IndividualDto?> GetIndividualByIdAsync(Guid id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IndividualDto>($"api/individuals/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching individual with ID {IndividualId}", id);
                return null;
            }
        }

        public async Task<IndividualDto?> CreateIndividualAsync(IndividualDto individual)
        {
            if (individual == null)
            {
                _logger.LogError("Cannot create individual: Individual data is null");
                return null;
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/individuals", individual);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IndividualDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating individual");
                return null;
            }
        }

        public async Task UpdateIndividualAsync(Guid id, IndividualDto individual)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/individuals/{id}", individual);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating individual with ID {id}");
                throw;
            }
        }

        public async Task DeleteIndividualAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/individuals/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting individual with ID {id}");
                throw;
            }
        }
    }
}
