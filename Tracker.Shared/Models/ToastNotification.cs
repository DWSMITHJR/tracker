using System;

namespace Tracker.Shared.Models
{
    /// <summary>
    /// Represents the severity level of a toast notification
    /// </summary>
    public enum ToastLevel
    {
        /// <summary>Informational message</summary>
        Info,
        /// <summary>Success message</summary>
        Success,
        /// <summary>Warning message</summary>
        Warning,
        /// <summary>Error message</summary>
        Error
    }

    /// <summary>
    /// Represents a toast notification message
    /// </summary>
    public class Toast
    {
        /// <summary>
        /// Gets the unique identifier for this toast notification
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();
        
        /// <summary>
        /// Gets or sets the title of the toast notification
        /// </summary>
        public required string Title { get; set; }
        
        /// <summary>
        /// Gets or sets the message content of the toast notification
        /// </summary>
        public required string Message { get; set; }
        
        /// <summary>
        /// Gets the timestamp when the toast was created
        /// </summary>
        public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;
        
        /// <summary>
        /// Gets or sets the severity level of the toast
        /// </summary>
        public ToastLevel Level { get; set; } = ToastLevel.Info;
        
        /// <summary>
        /// Gets or sets the time delay before the toast automatically closes
        /// </summary>
        public TimeSpan AutoCloseDelay { get; set; } = TimeSpan.FromSeconds(5);
        
        /// <summary>
        /// Gets or sets a value indicating whether the toast should close automatically
        /// </summary>
        public bool AutoClose { get; set; } = true;
        
        /// <summary>
        /// Gets or sets a value indicating whether to show a progress bar indicating remaining time
        /// </summary>
        public bool ShowProgressBar { get; set; } = true;
    }
}
