using Microsoft.AspNetCore.Mvc.Rendering;
using PharmaSkincare.Models;
using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.ViewModels
{
    public class InventoryAdjustmentViewModel
    {
        [Required, Display(Name = "Product")]
        public int ProductId { get; set; }

        [Required, Display(Name = "Transaction Type")]
        public TransactionType TransactionType { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [MaxLength(100), Display(Name = "Batch Number")]
        public string? BatchNumber { get; set; }

        [Display(Name = "Expiry Date")]
        public DateTime? ExpiryDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(100), Display(Name = "Reference Number")]
        public string? ReferenceNumber { get; set; }

        public SelectList? Products { get; set; }
        public SelectList? TransactionTypes { get; set; }

        // Display info
        public string? ProductName { get; set; }
        public int CurrentStock { get; set; }
    }

    public class InventoryTransactionListViewModel
    {
        public IEnumerable<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
        public string? SearchTerm { get; set; }
        public int? ProductFilter { get; set; }
        public TransactionType? TypeFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public SelectList? Products { get; set; }
    }
}
