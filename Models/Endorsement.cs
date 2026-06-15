using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.Models
{
    public class Endorsement
    {
        public int EndorsementId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Display(Name = "Endorsement Date")]
        public DateTime EndorsementDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Expiry Date")]
        public DateTime? ExpiryDate { get; set; }

        [MaxLength(500)]
        [Display(Name = "Document Path")]
        public string? DocumentPath { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(200)]
        [Display(Name = "Endorsement Type")]
        public string? EndorsementType { get; set; }

        // Navigation
        public Doctor? Doctor { get; set; }
        public Product? Product { get; set; }
    }
}
