using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();
    }
}
