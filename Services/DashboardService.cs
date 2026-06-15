using Microsoft.EntityFrameworkCore;
using PharmaSkincare.Data;
using PharmaSkincare.Models;
using PharmaSkincare.ViewModels;

namespace PharmaSkincare.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;

        public DashboardService(ApplicationDbContext context, IOrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var now = DateTime.UtcNow;

            var vm = new DashboardViewModel
            {
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalShipments = await _context.Shipments.CountAsync(),
                LowStockCount = await _context.Products.CountAsync(p => p.IsActive && p.StockQuantity <= p.MinimumStockLevel),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
                ActiveCampaigns = await _context.Campaigns.CountAsync(c => c.Status == CampaignStatus.Active),
                ExpiringCertificates = await _context.Certificates.CountAsync(c => c.IsActive && c.ExpiryDate <= now.AddDays(30) && c.ExpiryDate > now),
                TotalRevenue = await _orderService.GetTotalRevenueAsync(),
                MonthlyRevenue = await _orderService.GetMonthlyRevenueAsync(now.Year, now.Month),

                LowStockProducts = await _context.Products
                    .Include(p => p.Subcategory!).ThenInclude(s => s.Category)
                    .Where(p => p.IsActive && p.StockQuantity <= p.MinimumStockLevel)
                    .OrderBy(p => p.StockQuantity)
                    .Take(5)
                    .ToListAsync(),

                ExpiringCertificatesList = await _context.Certificates
                    .Include(c => c.Product)
                    .Where(c => c.IsActive && c.ExpiryDate <= now.AddDays(30))
                    .OrderBy(c => c.ExpiryDate)
                    .Take(5)
                    .ToListAsync(),

                ReturnedOrdersCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Returned),

                RecentOrders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(8)
                    .ToListAsync(),

                RecentShipments = await _context.Shipments
                    .Include(s => s.Order).ThenInclude(o => o.Customer)
                    .OrderByDescending(s => s.ShipmentDate)
                    .Take(8)
                    .ToListAsync(),

                ActiveCampaignsList = await _context.Campaigns
                    .Where(c => c.Status == CampaignStatus.Active)
                    .OrderByDescending(c => c.StartDate)
                    .Take(6)
                    .ToListAsync(),

                RecentActivities = await _context.ActivityLogs
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .ToListAsync(),

                TopSellingProducts = await _orderService.GetTopSellingProductsAsync(5),
                MonthlyRevenueData = await _orderService.GetMonthlyRevenueDataAsync(6),

                OrderStatusBreakdown = await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new OrderStatusItem { Status = g.Key.ToString(), Count = g.Count() })
                    .ToListAsync(),

                ShipmentStatusBreakdown = await _context.Shipments
                    .GroupBy(s => s.Status)
                    .Select(g => new ShipmentStatusItem { Status = g.Key.ToString(), Count = g.Count() })
                    .ToListAsync()
            };

            // count customers
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
            if (customerRole != null)
                vm.TotalCustomers = await _context.UserRoles.CountAsync(ur => ur.RoleId == customerRole.Id);

            return vm;
        }
    }
}
