using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.Infrastructure.Models;

[Table("Logs")]
public class LogEntry
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public DateTime Timestamp { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Level { get; set; } = string.Empty;
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? Source { get; set; }
    
    [MaxLength(450)]
    public string? UserId { get; set; }
    
    public string? Exception { get; set; }
    
    public string Properties { get; set; } = string.Empty;
}
