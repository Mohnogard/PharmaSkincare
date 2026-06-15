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
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly IActivityLogService _activityLog;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InventoryController(IInventoryService inventoryService, IActivityLogService activityLog,
            ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _inventoryService = inventoryService;
            _activityLog = activityLog;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, int? productId, TransactionType? type, DateTime? from, DateTime? to, int page = 1)
        {
            var vm = await _inventoryService.GetTransactionsAsync(search, productId, type, from, to, page, 15);
            var allProducts = await _context.Products.Include(p => p.Subcategory!).Where(p => p.IsActive).OrderBy(p => p.Subcategory!.Name).ToListAsync();
            vm.Products = new SelectList(allProducts.Select(p => new { p.ProductId, DisplayName = p.DisplayName }), "ProductId", "DisplayName");
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Adjust()
        {
            var vm = new InventoryAdjustmentViewModel();
            await PopulateDropdowns(vm);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(InventoryAdjustmentViewModel vm)
        {
            if (!ModelState.IsValid) { await PopulateDropdowns(vm); return View(vm); }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var product = await _context.Products.FindAsync(vm.ProductId);
            if (product == null) { ModelState.AddModelError("", "Product not found."); await PopulateDropdowns(vm); return View(vm); }

            if ((vm.TransactionType == TransactionType.StockOut || vm.TransactionType == TransactionType.Damaged || vm.TransactionType == TransactionType.Transfer)
                && product.StockQuantity < vm.Quantity)
            {
                ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockQuantity}");
                await PopulateDropdowns(vm);
                return View(vm);
            }

            await _inventoryService.RecordTransactionAsync(vm, user.Id);
            await _activityLog.LogAsync(user.Id, $"Inventory {vm.TransactionType}", "InventoryTransaction", vm.ProductId,
                $"{vm.Quantity} units - {product.DisplayName}");

            TempData["Success"] = $"Inventory adjusted successfully for '{product.DisplayName}'.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ProductHistory(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var history = await _inventoryService.GetProductHistoryAsync(productId);
            ViewBag.Product = product;
            return View(history);
        }

        public async Task<IActionResult> LowStock()
        {
            var products = await _context.Products
                .Include(p => p.Subcategory!).ThenInclude(s => s.Category)
                .Where(p => p.IsActive && p.StockQuantity <= p.MinimumStockLevel)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();
            return View(products);
        }

        private async Task PopulateDropdowns(InventoryAdjustmentViewModel vm)
        {
            var products = await _context.Products.Include(p => p.Subcategory!).Where(p => p.IsActive).OrderBy(p => p.Subcategory!.Name).ToListAsync();
            vm.Products = new SelectList(products.Select(p => new { p.ProductId, DisplayName = p.DisplayName }), "ProductId", "DisplayName", vm.ProductId);
            vm.TransactionTypes = new SelectList(Enum.GetValues<TransactionType>().Select(t => new { Value = t, Text = t.ToString() }), "Value", "Text", vm.TransactionType);

            if (vm.ProductId > 0)
            {
                var product = products.FirstOrDefault(p => p.ProductId == vm.ProductId);
                if (product != null)
                {
                    vm.ProductName = product.DisplayName;
                    vm.CurrentStock = product.StockQuantity;
                }
            }
        }
    }
}
