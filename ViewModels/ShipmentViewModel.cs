using Microsoft.AspNetCore.Mvc.Rendering;
using PharmaSkincare.Models;
using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.ViewModels
{
    public class ShipmentViewModel
    {
        public int ShipmentId { get; set; }

        [Required, Display(Name = "Order")]
        public int OrderId { get; set; }

        [Required, MaxLength(100), Display(Name = "Tracking Number")]
        public string TrackingNumber { get; set; } = string.Empty;

        [MaxLength(150), Display(Name = "Delivery Company")]
        public string? DeliveryCompany { get; set; }

        [Range(0, double.MaxValue), Display(Name = "Delivery Cost (JD)")]
        public decimal DeliveryCost { get; set; }

        public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;

        [Display(Name = "Estimated Delivery Date")]
        public DateTime? EstimatedDeliveryDate { get; set; }

        [Display(Name = "Actual Delivery Date")]
        public DateTime? ActualDeliveryDate { get; set; }

        [MaxLength(200)]
        public string? Region { get; set; }

        [MaxLength(500), Display(Name = "Delivery Address")]
        public string? DeliveryAddress { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Display info
        public string? CustomerName { get; set; }
        public string? OrderReference { get; set; }

        public SelectList? Orders { get; set; }
        public SelectList? StatusList { get; set; }
    }

    public class ShipmentListViewModel
    {
        public IEnumerable<Shipment> Shipments { get; set; } = new List<Shipment>();
        public string? SearchTerm { get; set; }
        public ShipmentStatus? StatusFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }
}
