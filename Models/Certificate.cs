using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.Models
{
    public enum CertificateStatus
    {
        Valid, Expired, PendingRenewal, Revoked
    }

    public class Certificate
    {
        public int CertificateId { get; set; }

        [Required, MaxLength(300)]
        [Display(Name = "Certificate Name")]
        public string CertificateName { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        [Display(Name = "Issuing Organization")]
        public string IssuingOrganization { get; set; } = string.Empty;

        [Display(Name = "Issue Date")]
        public DateTime IssueDate { get; set; }

        [Display(Name = "Expiry Date")]
        public DateTime ExpiryDate { get; set; }

        [MaxLength(500)]
        [Display(Name = "File Path")]
        public string? FilePath { get; set; }

        public int? ProductId { get; set; }

        [MaxLength(100)]
        [Display(Name = "Certificate Number")]
        public string? CertificateNumber { get; set; }

        [MaxLength(200)]
        [Display(Name = "Certificate Type")]
        public string? CertificateType { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public int DaysUntilExpiry => (ExpiryDate - DateTime.UtcNow).Days;

        public bool IsNearingExpiry => DaysUntilExpiry <= 30 && DaysUntilExpiry > 0;

        public bool IsExpired => ExpiryDate < DateTime.UtcNow;

        public CertificateStatus Status => IsExpired ? CertificateStatus.Expired :
                                           IsNearingExpiry ? CertificateStatus.PendingRenewal :
                                           CertificateStatus.Valid;

        // Navigation
        public Product? Product { get; set; }
    }
}
