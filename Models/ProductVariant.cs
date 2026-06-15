using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSkincare.Models
{
    public class ProductVariant
    {
        [Key]
        public int ProductVariantId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Size / Option")]
        public string Size { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 99999.99)]
        public decimal Price { get; set; }

        [Range(0, 100)]
        [Display(Name = "Discount %")]
        public int? DiscountPercent { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        // Computed — not mapped
        public decimal FinalPrice => DiscountPercent.HasValue && DiscountPercent > 0
            ? Math.Round(Price * (1 - DiscountPercent.Value / 100m), 2)
            : Price;

        public bool HasDiscount => DiscountPercent.HasValue && DiscountPercent > 0;

        public Product? Product { get; set; }
    }
}
