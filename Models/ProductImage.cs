using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.Models
{
    public class ProductImage
    {
        [Key]
        public int ProductImageId { get; set; }

        public int ProductId { get; set; }

        [Required, MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        public bool IsPrimary { get; set; } = false;

        public Product? Product { get; set; }
    }
}
