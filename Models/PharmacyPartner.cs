using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSkincare.Models
{
    public enum PartnershipStatus
    {
        Active, Inactive, Pending, Terminated
    }

    public class PharmacyPartner
    {
        public int PharmacyPartnerId { get; set; }

        [Required, MaxLength(300)]
        [Display(Name = "Pharmacy Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [EmailAddress, MaxLength(200)]
        public string? Email { get; set; }

        [Phone, MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [Display(Name = "Agreement Date")]
        public DateTime? AgreementDate { get; set; }

        [Display(Name = "Agreement Expiry")]
        public DateTime? AgreementExpiry { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Commission Rate (%)")]
        [Range(0, 100)]
        public decimal CommissionRate { get; set; }

        public PartnershipStatus Status { get; set; } = PartnershipStatus.Active;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public string? LogoUrl { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
