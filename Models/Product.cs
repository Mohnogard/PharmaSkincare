using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSkincare.Models
{
    public enum ProductFormat
    {
        Spray, Cream, RollOn, Oil, Gel, Patch, Capsule, Serum
    }

    public class Product
    {
        public int ProductId { get; set; }

        [Required]
        public int SubcategoryId { get; set; }

        // Size value — e.g. "100ml", "200 pills", null when SizeType = None
        [MaxLength(100)]
        [Display(Name = "Size / Amount")]
        public string? Size { get; set; }

        [Required, MaxLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 99999.99)]
        public decimal Price { get; set; }

        [Range(0, 100)]
        [Display(Name = "Discount %")]
        public int? DiscountPercent { get; set; }

        [Display(Name = "Stock Quantity")]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Display(Name = "Minimum Stock Level")]
        [Range(0, int.MaxValue)]
        public int MinimumStockLevel { get; set; } = 10;

        [Display(Name = "Image")]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        // Used by category cascade soft-delete
        public bool? IsActiveSnapshot { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }

        // Computed — not stored
        public decimal FinalPrice => DiscountPercent.HasValue && DiscountPercent > 0
            ? Math.Round(Price * (1 - DiscountPercent.Value / 100m), 2)
            : Price;

        public bool HasDiscount => DiscountPercent.HasValue && DiscountPercent > 0;

        public bool IsLowStock => StockQuantity <= MinimumStockLevel;

        [NotMapped]
        public string DisplayName =>
            Subcategory != null
                ? (Subcategory.SizeType != SizeType.None && !string.IsNullOrEmpty(Size)
                    ? $"{Subcategory.Name} — {Size}"
                    : Subcategory.Name)
                : Size ?? "Product";

        // Navigation
        public Subcategory? Subcategory { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
        public ICollection<Endorsement> Endorsements { get; set; } = new List<Endorsement>();
        public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    }
}
