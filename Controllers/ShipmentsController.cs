using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaSkincare.Data;
using PharmaSkincare.Models;
using PharmaSkincare.Services;
using PharmaSkincare.ViewModels;

namespace PharmaSkincare.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ShipmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogService _activityLog;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShipmentsController(ApplicationDbContext context, IActivityLogService activityLog,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _activityLog = activityLog;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, ShipmentStatus? status, int page = 1)
        {
            const int pageSize = 15;
            var query = _context.Shipments.Include(s => s.Order).ThenInclude(o => o!.Customer).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s => s.TrackingNumber.Contains(search) || (s.DeliveryCompany != null && s.DeliveryCompany.Contains(search)));

            if (status.HasValue)
                query = query.Where(s => s.Status == status);

            var total = await query.CountAsync();
            var shipments = await query.OrderByDescending(s => s.ShipmentDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var vm = new ShipmentListViewModel
            {
                Shipments = shipments,
                SearchTerm = search,
                StatusFilter = status,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                TotalCount = total
            };
            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var shipment = await _context.Shipments.Include(s => s.Order).ThenInclude(o => o!.Customer)
                                                   .Include(s => s.Order!.OrderItems).ThenInclude(oi => oi.Product)
                                                   .FirstOrDefaultAsync(s => s.ShipmentId == id);
            if (shipment == null) return NotFound();
            return View(shipment);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new ShipmentViewModel();
            await PopulateDropdowns(vm);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShipmentViewModel vm)
        {
            if (!ModelState.IsValid) { await PopulateDropdowns(vm); return View(vm); }

            if (await _context.Shipments.AnyAsync(s => s.TrackingNumber == vm.TrackingNumber))
            {
                ModelState.AddModelError("TrackingNumber", "A shipment with this tracking number already exists.");
                await PopulateDropdowns(vm);
                return View(vm);
            }

            var shipment = new Shipment
            {
                OrderId = vm.OrderId,
                TrackingNumber = vm.TrackingNumber,
                DeliveryCompany = vm.DeliveryCompany,
                DeliveryCost = vm.DeliveryCost,
                Status = vm.Status,
                EstimatedDeliveryDate = vm.EstimatedDeliveryDate,
                Region = vm.Region,
                DeliveryAddress = vm.DeliveryAddress,
                Notes = vm.Notes,
                ShipmentDate = DateTime.UtcNow
            };

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            // Update order status to Shipped
            var order = await _context.Orders.FindAsync(vm.OrderId);
            if (order != null) { order.Status = OrderStatus.Shipped; await _context.SaveChangesAsync(); }

            var user = await _userManager.GetUserAsync(User);
            if (user != null) await _activityLog.LogAsync(user.Id, "Shipment Created", "Shipment", shipment.ShipmentId, vm.TrackingNumber);

            TempData["Success"] = $"Shipment {vm.TrackingNumber} created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null) return NotFound();

            var vm = new ShipmentViewModel
            {
                ShipmentId = shipment.ShipmentId,
                OrderId = shipment.OrderId,
                TrackingNumber = shipment.TrackingNumber,
                DeliveryCompany = shipment.DeliveryCompany,
                DeliveryCost = shipment.DeliveryCost,
                Status = shipment.Status,
                EstimatedDeliveryDate = shipment.EstimatedDeliveryDate,
                ActualDeliveryDate = shipment.ActualDeliveryDate,
                Region = shipment.Region,
                DeliveryAddress = shipment.DeliveryAddress,
                Notes = shipment.Notes
            };
            await PopulateDropdowns(vm);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ShipmentViewModel vm)
        {
            if (!ModelState.IsValid) { await PopulateDropdowns(vm); return View(vm); }

            var shipment = await _context.Shipments.FindAsync(vm.ShipmentId);
            if (shipment == null) return NotFound();

            shipment.TrackingNumber = vm.TrackingNumber;
            shipment.DeliveryCompany = vm.DeliveryCompany;
            shipment.DeliveryCost = vm.DeliveryCost;
            shipment.Status = vm.Status;
            shipment.EstimatedDeliveryDate = vm.EstimatedDeliveryDate;
            shipment.ActualDeliveryDate = vm.ActualDeliveryDate;
            shipment.Region = vm.Region;
            shipment.DeliveryAddress = vm.DeliveryAddress;
            shipment.Notes = vm.Notes;

            await _context.SaveChangesAsync();

            // Sync order status if delivered
            if (vm.Status == ShipmentStatus.Delivered)
            {
                var order = await _context.Orders.FindAsync(vm.OrderId);
                if (order != null) { order.Status = OrderStatus.Delivered; await _context.SaveChangesAsync(); }
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null) await _activityLog.LogAsync(user.Id, "Shipment Updated", "Shipment", vm.ShipmentId, vm.TrackingNumber);

            TempData["Success"] = "Shipment updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(ShipmentViewModel vm)
        {
            var orders = await _context.Orders.Include(o => o.Customer)
                .Where(o => o.Status == OrderStatus.Processing || o.Status == OrderStatus.Packed || o.Status == OrderStatus.Pending)
                .ToListAsync();
            vm.Orders = new SelectList(orders.Select(o => new { o.OrderId, Display = $"Order #{o.OrderId} - {o.Customer?.FullName}" }), "OrderId", "Display", vm.OrderId);
            vm.StatusList = new SelectList(Enum.GetValues<ShipmentStatus>().Select(s => new { Value = s, Text = s.ToString() }), "Value", "Text", vm.Status);
        }
    }
}
