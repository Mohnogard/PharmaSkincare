using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSkincare.Models
{
    public enum OrderStatus
    {
        Pending, Processing, Packed, Shipped, Delivered, Cancelled, Returned
    }

    public class Order
    {
        public int OrderId { get; set; }

        [Required]
        public string CustomerId { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal => TotalAmount + ShippingCost - DiscountAmount;

        [MaxLength(500)]
        [Display(Name = "Shipping Address")]
        public string? ShippingAddress { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(20)]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(100)]
        [Display(Name = "Payment Method")]
        public string? PaymentMethod { get; set; }

        public bool IsPaid { get; set; } = false;

        public DateTime? PaidDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        // Navigation
        public ApplicationUser? Customer { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public Shipment? Shipment { get; set; }
    }
}
