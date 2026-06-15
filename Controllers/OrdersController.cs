using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PharmaSkincare.Models;
using PharmaSkincare.Services;
using PharmaSkincare.ViewModels;

namespace PharmaSkincare.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IActivityLogService _activityLog;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public OrdersController(IOrderService orderService, IActivityLogService activityLog,
            UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _orderService = orderService;
            _activityLog = activityLog;
            _userManager = userManager;
            _emailService = emailService;
        }

        [Authorize(Roles = "Admin,WarehouseEmployee")]
        public async Task<IActionResult> Index(string? search, OrderStatus? status, DateTime? from, DateTime? to, int page = 1)
        {
            var vm = await _orderService.GetOrdersAsync(search, status, from, to, page, 15);
            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();

            // Customers can only view their own orders
            if (User.IsInRole("Customer"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || order.CustomerId != user.Id)
                    return Forbid();
            }

            return View(order);
        }

        public async Task<IActionResult> Invoice(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();

            if (User.IsInRole("Customer"))
            {
                var u = await _userManager.GetUserAsync(User);
                if (u == null || order.CustomerId != u.Id) return Forbid();
            }

            return View(order);
        }

        [Authorize(Roles = "Admin,WarehouseEmployee")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();

            await _orderService.UpdateOrderStatusAsync(id, status);
            var user = await _userManager.GetUserAsync(User);
            if (user != null) await _activityLog.LogAsync(user.Id, $"Order Status Updated to {status}", "Order", id);

            if (order.Customer?.Email != null)
                _ = _emailService.SendOrderStatusUpdateAsync(order.Customer.Email, order.Customer.FullName, id, status.ToString());

            TempData["Success"] = $"Order #{id} status updated to {status}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var orders = await _orderService.GetCustomerOrdersAsync(user.Id);
            return View(orders);
        }

        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateOrderViewModel());
        }

        [Authorize(Roles = "Customer")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrderViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!vm.CartItems.Any())
            {
                ModelState.AddModelError("", "Your cart is empty.");
                return View(vm);
            }

            var order = await _orderService.CreateOrderAsync(user.Id, vm);
            await _activityLog.LogAsync(user.Id, "Order Placed", "Order", order.OrderId, $"JD {order.TotalAmount:F2}");

            TempData["Success"] = $"Order #{order.OrderId} placed successfully!";
            return RedirectToAction(nameof(Details), new { id = order.OrderId });
        }
    }
}
