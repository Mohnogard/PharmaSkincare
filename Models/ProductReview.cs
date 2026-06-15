using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.Models
{
    public class ProductReview
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public string CustomerId { get; set; } = string.Empty;

        [Required, Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; } = false;

        // Navigation
        public Product? Product { get; set; }
        public ApplicationUser? Customer { get; set; }
    }
}
