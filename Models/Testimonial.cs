using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.Models
{
    public class Testimonial
    {
        [Key]
        public int TestimonialId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string AuthorName { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        public string? CustomerId { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation
        public ApplicationUser? Customer { get; set; }
    }
}
