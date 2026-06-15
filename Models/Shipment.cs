using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaSkincare.Models
{
    public enum ShipmentStatus
    {
        Pending, InTransit, Delivered, Delayed, Returned, Lost
    }

    public class Shipment
    {
        public int ShipmentId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Tracking Number")]
        public string TrackingNumber { get; set; } = string.Empty;

        [MaxLength(150)]
        [Display(Name = "Delivery Company")]
        public string? DeliveryCompany { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Delivery Cost")]
        public decimal DeliveryCost { get; set; }

        public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;

        [Display(Name = "Estimated Delivery Date")]
        public DateTime? EstimatedDeliveryDate { get; set; }

        [Display(Name = "Actual Delivery Date")]
        public DateTime? ActualDeliveryDate { get; set; }

        public DateTime ShipmentDate { get; set; } = DateTime.UtcNow;

        [MaxLength(200)]
        public string? Region { get; set; }

        [MaxLength(500)]
        [Display(Name = "Delivery Address")]
        public string? DeliveryAddress { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public bool IsOnTime => ActualDeliveryDate.HasValue &&
                                EstimatedDeliveryDate.HasValue &&
                                ActualDeliveryDate <= EstimatedDeliveryDate;

        // Navigation
        public Order? Order { get; set; }
    }
}
