using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public class ContactService : IContactService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ContactService> _logger;

        public ContactService(HttpClient httpClient, ILogger<ContactService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PagedResult<ContactDto>> GetContactsAsync(int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            try
            {
                var url = $"api/contacts?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    url += $"&search={Uri.EscapeDataString(searchTerm)}";
                }
                
                var result = await _httpClient.GetFromJsonAsync<PagedResult<ContactDto>>(url);
                return result ?? new PagedResult<ContactDto> { Items = new List<ContactDto>(), TotalCount = 0 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contacts");
                return new PagedResult<ContactDto> { Items = new List<ContactDto>(), TotalCount = 0 };
            }
        }
        
        public async Task<IEnumerable<ContactDto>> GetContactsByIncidentIdAsync(string incidentId)
        {
            if (string.IsNullOrWhiteSpace(incidentId))
            {
                _logger.LogWarning("GetContactsByIncidentIdAsync called with null or empty incidentId");
                return new List<ContactDto>();
            }

            try
            {
                var result = await _httpClient.GetFromJsonAsync<IEnumerable<ContactDto>>($"api/incidents/{incidentId}/contacts");
                return result ?? new List<ContactDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contacts for incident {IncidentId}", incidentId);
                return new List<ContactDto>();
            }
        }

        public async Task<ContactDto> GetContactByIdAsync(Guid id)
        {
            try
            {
                var contact = await _httpClient.GetFromJsonAsync<ContactDto>($"api/contacts/{id}");
                if (contact == null)
                {
                    _logger.LogWarning("Contact with ID {ContactId} not found", id);
                    throw new KeyNotFoundException($"Contact with ID {id} not found");
                }
                return contact;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Contact with ID {ContactId} not found", id);
                throw new KeyNotFoundException($"Contact with ID {id} not found", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contact with ID {ContactId}", id);
                throw;
            }
        }

        public async Task<ContactDto> CreateContactAsync(ContactDto contact)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/contacts", contact);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<ContactDto>();
                if (result == null)
                {
                    _logger.LogError("Received null response when creating contact");
                    throw new InvalidOperationException("Failed to deserialize the contact from the API response");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contact");
                throw;
            }
        }

        public async Task UpdateContactAsync(Guid id, ContactDto contact)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/contacts/{id}", contact);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating contact with ID {id}");
                throw;
            }
        }

        public async Task DeleteContactAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/contacts/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting contact with ID {id}");
                throw;
            }
        }
    }
}
