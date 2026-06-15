using Microsoft.AspNetCore.Mvc.Rendering;
using PharmaSkincare.Models;
using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.ViewModels
{
    public class ProductViewModel
    {
        public int ProductId { get; set; }

        [Required, Display(Name = "Subcategory")]
        public int SubcategoryId { get; set; }

        [MaxLength(100), Display(Name = "Size / Amount")]
        public string? Size { get; set; }

        [Required, MaxLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required, Range(0.01, 99999.99)]
        [Display(Name = "Price (JD)")]
        public decimal Price { get; set; }

        [Range(0, 100), Display(Name = "Discount %")]
        public int? DiscountPercent { get; set; }

        [Display(Name = "Stock Quantity"), Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Display(Name = "Minimum Stock Level"), Range(0, int.MaxValue)]
        public int MinimumStockLevel { get; set; } = 10;

        public string? ImageUrl { get; set; }

        [Display(Name = "Upload Image")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Additional Gallery Images")]
        public List<IFormFile>? AdditionalImages { get; set; }

        public List<PharmaSkincare.Models.ProductImage>? ExistingImages { get; set; }
        public List<int>? DeleteImageIds { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public SelectList? Subcategories { get; set; }
    }

    public class SubcategoryViewModel
    {
        public int SubcategoryId { get; set; }

        [Required, Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Display(Name = "Size Type")]
        public SizeType SizeType { get; set; } = SizeType.None;

        [MaxLength(200), Display(Name = "Formula Type")]
        public string? FormulaType { get; set; }

        [Display(Name = "Product Format")]
        public ProductFormat ProductFormat { get; set; } = ProductFormat.Spray;

        [MaxLength(500), Display(Name = "Health Benefits")]
        public string? HealthBenefits { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Main Image")]
        public IFormFile? MainImageFile { get; set; }
        public string? MainImageUrl { get; set; }

        public SelectList? Categories { get; set; }
    }

    public class ProductListViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public string? SearchTerm { get; set; }
        public int? SubcategoryFilter { get; set; }
        public bool? LowStockFilter { get; set; }
        public string? FormatFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public SelectList? Subcategories { get; set; }
    }
}
