using PharmaSkincare.Models;
using PharmaSkincare.ViewModels;

namespace PharmaSkincare.Services
{
    public interface IOrderService
    {
        Task<OrderListViewModel> GetOrdersAsync(string? search, OrderStatus? status, DateTime? from, DateTime? to, int page, int pageSize);
        Task<Order?> GetOrderByIdAsync(int id);
        Task<Order> CreateOrderAsync(string customerId, CreateOrderViewModel vm);
        Task UpdateOrderStatusAsync(int orderId, OrderStatus status);
        Task<IEnumerable<Order>> GetCustomerOrdersAsync(string customerId);
        Task<decimal> GetTotalRevenueAsync();
        Task<decimal> GetMonthlyRevenueAsync(int year, int month);
        Task<List<TopProductItem>> GetTopSellingProductsAsync(int count = 5);
        Task<List<MonthlyRevenueItem>> GetMonthlyRevenueDataAsync(int months = 6);
    }
}
