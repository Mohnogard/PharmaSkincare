using PharmaSkincare.Models;

namespace PharmaSkincare.ViewModels
{
    public class DashboardViewModel
    {
        // Shared stats
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int TotalShipments { get; set; }
        public int TotalCustomers { get; set; }
        public int LowStockCount { get; set; }
        public int PendingOrders { get; set; }
        public int ActiveCampaigns { get; set; }
        public int ExpiringCertificates { get; set; }
        public int ReturnedOrdersCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }

        // Admin / all-roles lists
        public List<Product> LowStockProducts { get; set; } = new();
        public List<Certificate> ExpiringCertificatesList { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
        public List<ActivityLog> RecentActivities { get; set; } = new();
        public List<TopProductItem> TopSellingProducts { get; set; } = new();
        public List<MonthlyRevenueItem> MonthlyRevenueData { get; set; } = new();
        public List<OrderStatusItem> OrderStatusBreakdown { get; set; } = new();
        public List<ShipmentStatusItem> ShipmentStatusBreakdown { get; set; } = new();
        public List<Shipment> RecentShipments { get; set; } = new();

        // Marketing-specific
        public List<Campaign> ActiveCampaignsList { get; set; } = new();
    }

    public class CustomerDashboardViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public List<Order> RecentOrders { get; set; } = new();
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public int WishlistCount { get; set; }
        public List<WishlistItem> WishlistPreview { get; set; } = new();
    }

    public class PartnerDashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public Doctor? Doctor { get; set; }
        public PharmacyPartner? PharmacyPartner { get; set; }
        public List<Endorsement> Endorsements { get; set; } = new();
    }

    public class TopProductItem
    {
        public string ProductName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class MonthlyRevenueItem
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }

    public class OrderStatusItem
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ShipmentStatusItem
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ReportsViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public double ShipmentOnTimeRate { get; set; }
        public List<CategoryRevenueItem> TopCategories { get; set; } = new();
        public List<MonthlyRevenueItem> MonthlyRevenueData { get; set; } = new();
        public List<TopProductItem> TopSellingProducts { get; set; } = new();
        public List<OrderStatusItem> OrderStatusBreakdown { get; set; } = new();
    }

    public class CategoryRevenueItem
    {
        public string Category { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }
}
