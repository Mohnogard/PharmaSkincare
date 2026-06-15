using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.Models
{
    public class ActivityLog
    {
        public int ActivityLogId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Entity Type")]
        public string? EntityType { get; set; }

        public int? EntityId { get; set; }

        [MaxLength(2000)]
        public string? Details { get; set; }

        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation
        public ApplicationUser? User { get; set; }
    }
}
