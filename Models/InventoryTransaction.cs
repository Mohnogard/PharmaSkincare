using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.Models
{
    public enum TransactionType
    {
        StockIn, StockOut, Adjustment, Damaged, Returned, Transfer
    }

    public class InventoryTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public TransactionType TransactionType { get; set; }

        [Required]
        public int Quantity { get; set; }

        public int QuantityBefore { get; set; }

        public int QuantityAfter { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Batch Number")]
        public string? BatchNumber { get; set; }

        [Display(Name = "Expiry Date")]
        public DateTime? ExpiryDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(100)]
        [Display(Name = "Reference Number")]
        public string? ReferenceNumber { get; set; }

        // Navigation
        public Product? Product { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
