using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSkincare.Models
{
    public enum CampaignStatus
    {
        Draft, Active, Paused, Completed, Cancelled
    }

    public class Campaign
    {
        public int CampaignId { get; set; }

        [Required, MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal Budget { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount Spent")]
        public decimal AmountSpent { get; set; }

        public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

        [MaxLength(500)]
        [Display(Name = "Target Audience")]
        public string? TargetAudience { get; set; }

        [MaxLength(200)]
        public string? Platform { get; set; }

        [MaxLength(500)]
        [Display(Name = "Campaign Image")]
        public string? ImageUrl { get; set; }

        public int Impressions { get; set; }

        public int Clicks { get; set; }

        public int Conversions { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsActive => Status == CampaignStatus.Active &&
                                StartDate <= DateTime.UtcNow &&
                                EndDate >= DateTime.UtcNow;
    }
}
