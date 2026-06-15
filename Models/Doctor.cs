using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.Models
{
    public class Doctor
    {
        public int DoctorId { get; set; }

        [Required, MaxLength(200)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Specialty { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Qualification { get; set; }

        [EmailAddress, MaxLength(200)]
        public string? Email { get; set; }

        [Phone, MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        [Display(Name = "License Number")]
        public string? LicenseNumber { get; set; }

        [MaxLength(500)]
        public string? Hospital { get; set; }

        [MaxLength(200)]
        public string? City { get; set; }

        [MaxLength(200)]
        public string? Country { get; set; }

        public string? ProfileImageUrl { get; set; }

        [MaxLength(1000)]
        public string? Bio { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Endorsement> Endorsements { get; set; } = new List<Endorsement>();
    }
}
