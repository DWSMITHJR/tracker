using System.Collections.Generic;

namespace Tracker.Shared.Models;

/// <summary>
/// Represents a paginated result of log entries
/// </summary>
public class LogQueryResult
{
    /// <summary>
    /// Gets or sets the list of log entries in the current page
    /// </summary>
    public List<LogEntry> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of log entries across all pages
    /// </summary>
    public int TotalCount { get; set; }
}
