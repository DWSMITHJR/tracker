using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Tracker.API.Data;
using Tracker.Infrastructure.Data;
using Tracker.Infrastructure.Models;
using Tracker.Shared.Models;

namespace Tracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Operator")]
    public class LogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LogsController> _logger;

        public LogsController(ApplicationDbContext context, ILogger<LogsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<ActionResult<global::Tracker.Shared.Models.LogQueryResult>> GetLogs(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? level = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Base query
                var query = _context.Logs.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var searchTermLower = searchTerm.ToLower();
                    query = query.Where(l => 
                        (l.Message != null && l.Message.ToLower().Contains(searchTermLower)) ||
                        (l.Source != null && l.Source.ToLower().Contains(searchTermLower)) ||
                        (l.UserId != null && l.UserId.ToLower().Contains(searchTermLower)));
                }

                if (!string.IsNullOrEmpty(level))
                {
                    query = query.Where(l => l.Level == level);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(l => l.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    // Add one day to include the entire end date
                    var endDateInclusive = endDate.Value.Date.AddDays(1);
                    query = query.Where(l => l.Timestamp < endDateInclusive);
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply ordering and pagination
                var logs = await query
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new Tracker.Shared.Models.LogEntry
                    {
                        Id = l.Id,
                        Timestamp = l.Timestamp,
                        Level = l.Level,
                        Message = l.Message,
                        Source = l.Source,
                        UserId = l.UserId,
                        Exception = l.Exception,
                        Properties = l.Properties
                    })
                    .ToListAsync();

                // Create result with fully qualified type names to avoid ambiguity
                var result = new global::Tracker.Shared.Models.LogQueryResult
                {
                    Items = logs,
                    TotalCount = totalCount
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving logs");
                return StatusCode(500, "An error occurred while retrieving logs");
            }
        }
    }
}
