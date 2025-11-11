using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.Infrastructure.Models
{
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public string Token { get; set; } = string.Empty;
        
        public DateTime Expires { get; set; }
        public DateTime Created { get; set; }
        public string? CreatedByIp { get; set; }
        public DateTime? Revoked { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByToken { get; set; }
        public string? ReasonRevoked { get; set; }
        
        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsRevoked => Revoked != null;
        public bool IsActive => !IsRevoked && !IsExpired;
        
        [Required]
        public Guid UserId { get; set; }
        
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
}
