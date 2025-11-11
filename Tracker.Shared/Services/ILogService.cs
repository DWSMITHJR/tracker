using Tracker.Shared.Models;

namespace Tracker.Shared.Services;

public interface ILogService
{
    /// <summary>
    /// Retrieves logs based on the specified filters
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="searchTerm">Optional search term to filter logs</param>
    /// <param name="level">Optional log level to filter by</param>
    /// <param name="startDate">Optional start date to filter logs</param>
    /// <param name="endDate">Optional end date to filter logs</param>
    /// <returns>A LogQueryResult containing the filtered logs and total count</returns>
    Task<LogQueryResult> GetLogsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? level = null,
        DateTime? startDate = null,
        DateTime? endDate = null);
}
