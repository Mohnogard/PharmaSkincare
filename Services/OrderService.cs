using Microsoft.EntityFrameworkCore;
using PharmaSkincare.Data;
using PharmaSkincare.Models;
using PharmaSkincare.ViewModels;

namespace PharmaSkincare.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<OrderListViewModel> GetOrdersAsync(string? search, OrderStatus? status, DateTime? from, DateTime? to, int page, int pageSize)
        {
            var query = _context.Orders.Include(o => o.Customer)
                                       .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                                       .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(o => o.Customer!.FirstName.Contains(search) ||
                                         o.Customer.LastName.Contains(search) ||
                                         o.Customer.Email!.Contains(search) ||
                                         o.OrderId.ToString() == search);

            if (status.HasValue)
                query = query.Where(o => o.Status == status);

            if (from.HasValue)
                query = query.Where(o => o.OrderDate >= from);

            if (to.HasValue)
                query = query.Where(o => o.OrderDate <= to);

            var total = await query.CountAsync();
            var orders = await query.OrderByDescending(o => o.OrderDate)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

            return new OrderListViewModel
            {
                Orders = orders,
                SearchTerm = search,
                StatusFilter = status,
                DateFrom = from,
                DateTo = to,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                TotalCount = total
            };
        }

        public async Task<Order?> GetOrderByIdAsync(int id) =>
            await _context.Orders.Include(o => o.Customer)
                                 .Include(o => o.OrderItems).ThenInclude(oi => oi.Product).ThenInclude(p => p!.Subcategory!)
                                 .Include(o => o.Shipment)
                                 .FirstOrDefaultAsync(o => o.OrderId == id);

        public async Task<Order> CreateOrderAsync(string customerId, CreateOrderViewModel vm)
        {
            var order = new Order
            {
                CustomerId = customerId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                ShippingAddress = vm.ShippingAddress,
                City = vm.City,
                Country = vm.Country,
                PostalCode = vm.PostalCode,
                Notes = vm.Notes,
                PaymentMethod = vm.PaymentMethod,
                ShippingCost = 0m
            };

            decimal total = 0;
            foreach (var item in vm.CartItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null || product.StockQuantity < item.Quantity) continue;

                var orderItem = new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    VariantSize = item.VariantSize
                };
                order.OrderItems.Add(orderItem);
                total += item.UnitPrice * item.Quantity;
                product.StockQuantity -= item.Quantity;
            }

            order.TotalAmount = total;
            order.ShippingCost = total >= 40m ? 0m : 5.00m;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task UpdateOrderStatusAsync(int orderId, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return;

            order.Status = status;
            order.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Order>> GetCustomerOrdersAsync(string customerId) =>
            await _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                                 .Include(o => o.Shipment)
                                 .Where(o => o.CustomerId == customerId)
                                 .OrderByDescending(o => o.OrderDate)
                                 .ToListAsync();

        public async Task<decimal> GetTotalRevenueAsync() =>
            await _context.Orders.Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Returned)
                                 .SumAsync(o => o.TotalAmount + o.ShippingCost - o.DiscountAmount);

        public async Task<decimal> GetMonthlyRevenueAsync(int year, int month) =>
            await _context.Orders.Where(o => o.OrderDate.Year == year && o.OrderDate.Month == month &&
                                             o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Returned)
                                 .SumAsync(o => o.TotalAmount);

        public async Task<List<TopProductItem>> GetTopSellingProductsAsync(int count = 5)
        {
            return await _context.OrderItems
                .Include(oi => oi.Product).ThenInclude(p => p!.Subcategory!)
                .GroupBy(oi => new { oi.ProductId, oi.Product!.Subcategory!.Name })
                .Select(g => new TopProductItem
                {
                    ProductName = g.Key.Name,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<MonthlyRevenueItem>> GetMonthlyRevenueDataAsync(int months = 6)
        {
            var result = new List<MonthlyRevenueItem>();
            var now = DateTime.UtcNow;

            for (int i = months - 1; i >= 0; i--)
            {
                var date = now.AddMonths(-i);
                var revenue = await GetMonthlyRevenueAsync(date.Year, date.Month);
                var orderCount = await _context.Orders.CountAsync(o => o.OrderDate.Year == date.Year && o.OrderDate.Month == date.Month);

                result.Add(new MonthlyRevenueItem
                {
                    Month = date.ToString("MMM yyyy"),
                    Revenue = revenue,
                    Orders = orderCount
                });
            }

            return result;
        }
    }
}
