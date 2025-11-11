using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public class IncidentService : IIncidentService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IncidentService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public IncidentService(HttpClient httpClient, ILogger<IncidentService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<IncidentDto?> GetIncidentByIdAsync(Guid id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IncidentDto>($"api/incidents/{id}", _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incident by id: {Id}", id);
                return null;
            }
        }

        public async Task<PagedResult<IncidentDto>> GetIncidentsAsync(int page = 1, int pageSize = 10, string? searchQuery = null, string? status = null)
        {
            try
            {
                var query = $"api/incidents?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrEmpty(searchQuery))
                    query += $"&search={Uri.EscapeDataString(searchQuery)}";
                if (!string.IsNullOrEmpty(status))
                    query += $"&status={Uri.EscapeDataString(status)}";

                var response = await _httpClient.GetAsync(query);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API Error ({StatusCode}): {Response}", response.StatusCode, errorContent);
                    
                    return new PagedResult<IncidentDto> 
                    { 
                        Items = new List<IncidentDto>(),
                        PageNumber = page,
                        PageSize = pageSize,
                        TotalCount = 0,
                        Error = $"Error: {response.StatusCode} - {response.ReasonPhrase}"
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<PagedResult<IncidentDto>>(_jsonOptions);
                return result ?? new PagedResult<IncidentDto> 
                { 
                    Items = new List<IncidentDto>(),
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    Error = "Error: Invalid response from server"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting incidents");
                return new PagedResult<IncidentDto> 
                { 
                    Items = new List<IncidentDto>(),
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalCount = 0,
                    Error = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<IncidentDto?> CreateIncidentAsync(IncidentDto incident)
        {
            try
            {
                var content = JsonContent.Create(incident, options: _jsonOptions);
                var response = await _httpClient.PostAsync("api/incidents", content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IncidentDto>(_jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating incident");
                return null;
            }
        }

        public async Task<IncidentDto?> UpdateIncidentAsync(IncidentDto incident)
        {
            try
            {
                var content = JsonContent.Create(incident, options: _jsonOptions);
                var response = await _httpClient.PutAsync($"api/incidents/{incident.Id}", content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IncidentDto>(_jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating incident: {Id}", incident.Id);
                return null;
            }
        }

        public async Task<PagedResult<TimelineEntryDto>> GetTimelineEntriesAsync(Guid incidentId, int page = 1, int pageSize = 10)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<PagedResult<TimelineEntryDto>>(
                    $"api/incidents/{incidentId}/timeline?page={page}&pageSize={pageSize}", _jsonOptions)
                    ?? new PagedResult<TimelineEntryDto> { Items = new List<TimelineEntryDto>() };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching timeline entries for incident with ID {IncidentId}", incidentId);
                return new PagedResult<TimelineEntryDto> { Items = new List<TimelineEntryDto>() };
            }
        }

        public async Task<bool> UpdateIncidentStatusAsync(Guid incidentId, string status, string? comment = null)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(
                    $"api/incidents/{incidentId}/status", 
                    new { Status = status, Comment = comment },
                    _jsonOptions);
                
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for incident {IncidentId}", incidentId);
                return false;
            }
        }

        public async Task<bool> DeleteIncidentAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/incidents/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting incident with ID {IncidentId}", id);
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetIncidentStatusesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<string>>("api/incidents/statuses", _jsonOptions)
                    ?? new List<string> { "Open", "In Progress", "Resolved", "Closed" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching incident statuses");
                return new List<string> { "Open", "In Progress", "Resolved", "Closed" };
            }
        }

        public async Task<IEnumerable<string>> GetIncidentPrioritiesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<string>>("api/incidents/priorities", _jsonOptions)
                    ?? new List<string> { "Low", "Medium", "High", "Critical" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching incident priorities");
                return new List<string> { "Low", "Medium", "High", "Critical" };
            }
        }
    }
}
