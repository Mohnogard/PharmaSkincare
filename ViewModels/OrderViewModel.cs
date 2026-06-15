using Microsoft.AspNetCore.Mvc.Rendering;
using PharmaSkincare.Models;
using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.ViewModels
{
    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Display(Name = "Shipping Address"), Required]
        public string? ShippingAddress { get; set; }

        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public string? Notes { get; set; }
        public string? PaymentMethod { get; set; }
        public bool IsPaid { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GrandTotal => TotalAmount + ShippingCost - DiscountAmount;

        public List<OrderItemViewModel> Items { get; set; } = new();
        public Shipment? Shipment { get; set; }

        public SelectList? StatusList { get; set; }
    }

    public class OrderItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSKU { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal SubTotal => (UnitPrice * Quantity) - Discount;
    }

    public class CreateOrderViewModel
    {
        [Required, Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public string Country { get; set; } = string.Empty;

        public string? PostalCode { get; set; }
        public string? Notes { get; set; }
        public string? PaymentMethod { get; set; }

        public List<CartItemViewModel> CartItems { get; set; } = new();
    }

    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? VariantSize { get; set; }
        public decimal SubTotal => UnitPrice * Quantity;
    }

    public class OrderListViewModel
    {
        public IEnumerable<Order> Orders { get; set; } = new List<Order>();
        public string? SearchTerm { get; set; }
        public OrderStatus? StatusFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }
}
