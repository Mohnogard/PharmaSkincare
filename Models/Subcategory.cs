using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.Models
{
    public enum SizeType { None = 0, Ml = 1, Pills = 2 }

    public class Subcategory
    {
        public int SubcategoryId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required, MaxLength(200)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Display(Name = "Size Type")]
        public SizeType SizeType { get; set; } = SizeType.None;

        [MaxLength(500)]
        [Display(Name = "Main Image")]
        public string? MainImageUrl { get; set; }

        [MaxLength(200)]
        [Display(Name = "Formula Type")]
        public string? FormulaType { get; set; }

        [Display(Name = "Product Format")]
        public ProductFormat ProductFormat { get; set; } = ProductFormat.Spray;

        [MaxLength(500)]
        [Display(Name = "Health Benefits")]
        public string? HealthBenefits { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation
        public Category? Category { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
