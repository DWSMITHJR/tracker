namespace Tracker.Shared.Models;

/// <summary>
/// Represents a timeline entry for an incident
/// </summary>
public class TimelineEntryDto
{
    /// <summary>
    /// The unique identifier for the timeline entry
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The ID of the incident this entry belongs to
    /// </summary>
    public required string IncidentId { get; set; }

    /// <summary>
    /// When this timeline entry was created
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The type of event that occurred
    /// </summary>
    public required string Event { get; set; }

    /// <summary>
    /// Detailed description of the event
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// ID of the user who made this change
    /// </summary>
    public required string UpdatedById { get; set; }

    /// <summary>
    /// Name of the user who made this change
    /// </summary>
    public required string UpdatedByName { get; set; }
}

/// <summary>
/// DTO for creating a new timeline entry
/// </summary>
public class CreateTimelineEntryDto
{
    /// <summary>
    /// The type of event that occurred
    /// </summary>
    public required string Event { get; set; }

    /// <summary>
    /// Detailed description of the event
    /// </summary>
    public required string Description { get; set; }
}
