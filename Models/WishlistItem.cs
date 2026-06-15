using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.Models
{
    public class WishlistItem
    {
        [Key]
        public int WishlistItemId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public DateTime AddedDate { get; set; } = DateTime.UtcNow;

        public ApplicationUser Customer { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
