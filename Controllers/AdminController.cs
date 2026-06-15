using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSkincare.Data;
using PharmaSkincare.Models;
using PharmaSkincare.Services;
using PharmaSkincare.ViewModels;
using ClosedXML.Excel;

namespace PharmaSkincare.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IActivityLogService _activityLog;
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
            IActivityLogService activityLog, ApplicationDbContext context, IFileService fileService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _activityLog = activityLog;
            _context = context;
            _fileService = fileService;
        }

        public async Task<IActionResult> Users(string? search, int page = 1)
        {
            const int pageSize = 20;
            var usersQuery = _userManager.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                usersQuery = usersQuery.Where(u => u.Email!.Contains(search) || u.FirstName.Contains(search) || u.LastName.Contains(search));

            usersQuery = usersQuery.OrderBy(u => u.FirstName);
            var total = await usersQuery.CountAsync();
            var users = await usersQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var vmList = new List<UserManagementViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                vmList.Add(new UserManagementViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    CreatedDate = user.CreatedDate,
                    LastLoginDate = user.LastLoginDate,
                    Roles = roles.ToList()
                });
            }

            ViewBag.Search = search;
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.TotalCount = total;
            return View(vmList);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            var admin = await _userManager.GetUserAsync(User);
            if (admin != null) await _activityLog.LogAsync(admin.Id, $"Role Assigned: {role}", "User", null, $"To: {user.Email}");

            TempData["Success"] = $"Role '{role}' assigned to {user.FullName}.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Prevent admin from deactivating themselves
            var currentAdmin = await _userManager.GetUserAsync(User);
            if (currentAdmin?.Id == userId)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Users));
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            var action = user.IsActive ? "User Activated" : "User Deactivated";
            if (currentAdmin != null) await _activityLog.LogAsync(currentAdmin.Id, action, "User", null, user.Email);

            TempData["Success"] = $"{user.FullName} has been {(user.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> ActivityLogs(int page = 1)
        {
            const int pageSize = 25;
            var total = await _context.ActivityLogs.CountAsync();
            var logs = await _context.ActivityLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.TotalCount = total;
            return View(logs);
        }

        public async Task<IActionResult> Reports()
        {
            var now = DateTime.UtcNow;

            // Monthly revenue: pull last 6 months of orders into memory, then group
            var sixMonthsAgo = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
            var recentOrders = await _context.Orders
                .Where(o => o.OrderDate >= sixMonthsAgo)
                .ToListAsync();

            var monthlyData = Enumerable.Range(0, 6).Select(i =>
            {
                var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(i - 5);
                var monthEnd = monthStart.AddMonths(1);
                var mo = recentOrders.Where(o =>
                    o.OrderDate >= monthStart && o.OrderDate < monthEnd &&
                    o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Returned);
                return new MonthlyRevenueItem
                {
                    Month = monthStart.ToString("MMM yy"),
                    Revenue = mo.Sum(o => o.TotalAmount + o.ShippingCost - o.DiscountAmount),
                    Orders = mo.Count()
                };
            }).ToList();

            // Top 5 products by revenue
            var topRaw = await _context.OrderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(oi => oi.Quantity), Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity) })
                .OrderByDescending(p => p.Revenue)
                .Take(5)
                .ToListAsync();

            var productIds = topRaw.Select(p => p.ProductId).ToList();
            var productNames = await _context.Products
                .Include(p => p.Subcategory!)
                .Where(p => productIds.Contains(p.ProductId))
                .Select(p => new { p.ProductId, DisplayName = p.Subcategory!.Name })
                .ToDictionaryAsync(p => p.ProductId, p => p.DisplayName);

            var topProducts = topRaw.Select(p => new TopProductItem
            {
                ProductName = productNames.GetValueOrDefault(p.ProductId, "Unknown"),
                TotalSold = p.TotalSold,
                Revenue = p.Revenue
            }).ToList();

            // Order status breakdown
            var statusRaw = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var statusBreakdown = statusRaw
                .Select(g => new OrderStatusItem { Status = g.Status.ToString(), Count = g.Count })
                .ToList();

            var viewModel = new ReportsViewModel
            {
                TotalRevenue = await _context.Orders
                    .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Returned)
                    .SumAsync(o => o.TotalAmount + o.ShippingCost - o.DiscountAmount),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive),
                TotalCustomers = await _context.Users.CountAsync(),
                ShipmentOnTimeRate = await GetOnTimeRateAsync(),
                TopCategories = await GetTopCategoriesAsync(),
                MonthlyRevenueData = monthlyData,
                TopSellingProducts = topProducts,
                OrderStatusBreakdown = statusBreakdown
            };

            return View(viewModel);
        }

        public async Task<IActionResult> ExportInventory()
        {
            var products = await _context.Products.Include(p => p.Subcategory!).ThenInclude(s => s.Category).Where(p => p.IsActive).ToListAsync();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Inventory");

            ws.Cell(1, 1).Value = "SKU";
            ws.Cell(1, 2).Value = "Product Name";
            ws.Cell(1, 3).Value = "Category";
            ws.Cell(1, 4).Value = "Format";
            ws.Cell(1, 5).Value = "Stock";
            ws.Cell(1, 6).Value = "Min Stock";
            ws.Cell(1, 7).Value = "Price (JD)";
            ws.Cell(1, 8).Value = "Status";

            var headerRow = ws.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(0x1a, 0x7a, 0x5e);
            headerRow.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            for (int i = 0; i < products.Count; i++)
            {
                var p = products[i];
                ws.Cell(i + 2, 1).Value = p.SKU;
                ws.Cell(i + 2, 2).Value = p.DisplayName;
                ws.Cell(i + 2, 3).Value = p.Subcategory?.Category?.Name ?? "";
                ws.Cell(i + 2, 4).Value = p.Subcategory?.ProductFormat.ToString() ?? "";
                ws.Cell(i + 2, 5).Value = p.StockQuantity;
                ws.Cell(i + 2, 6).Value = p.MinimumStockLevel;
                ws.Cell(i + 2, 7).Value = p.Price;
                ws.Cell(i + 2, 8).Value = p.IsLowStock ? "LOW STOCK" : "OK";

                if (p.IsLowStock)
                    ws.Row(i + 2).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(0xff, 0xe0, 0xe0);
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Inventory_{DateTime.UtcNow:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> PendingContent()
        {
            ViewBag.PendingReviews = await _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.Customer)
                .Where(r => !r.IsApproved)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
            ViewBag.PendingTestimonials = await _context.Testimonials
                .Include(t => t.Customer)
                .Where(t => !t.IsApproved)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
            ViewBag.ContactMessages = await _context.ContactMessages
                .OrderByDescending(m => m.CreatedDate)
                .Take(20)
                .ToListAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReview(int id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review != null) { review.IsApproved = true; await _context.SaveChangesAsync(); TempData["Success"] = "Review approved and published."; }
            return RedirectToAction(nameof(PendingContent));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReview(int id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review != null) { _context.ProductReviews.Remove(review); await _context.SaveChangesAsync(); TempData["Success"] = "Review removed."; }
            return RedirectToAction(nameof(PendingContent));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveTestimonial(int id)
        {
            var t = await _context.Testimonials.FindAsync(id);
            if (t != null) { t.IsApproved = true; await _context.SaveChangesAsync(); TempData["Success"] = "Testimonial approved and published."; }
            return RedirectToAction(nameof(PendingContent));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectTestimonial(int id)
        {
            var t = await _context.Testimonials.FindAsync(id);
            if (t != null) { _context.Testimonials.Remove(t); await _context.SaveChangesAsync(); TempData["Success"] = "Testimonial removed."; }
            return RedirectToAction(nameof(PendingContent));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkContactRead(int id)
        {
            var m = await _context.ContactMessages.FindAsync(id);
            if (m != null) { m.IsRead = true; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(PendingContent));
        }

        // ── Category Management ────────────────────────────────────────

        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .Include(c => c.Subcategories).ThenInclude(s => s.Products)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(categories);
        }

        [HttpGet]
        public IActionResult CreateCategory() => View(new Category());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Categories.Add(model);
            await _context.SaveChangesAsync();
            var user = await _userManager.GetUserAsync(User);
            if (user != null) await _activityLog.LogAsync(user.Id, "Category Created", "Category", model.CategoryId, model.Name);
            TempData["Success"] = $"Category '{model.Name}' created.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Subcategories).ThenInclude(s => s.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(Category model)
        {
            if (!ModelState.IsValid)
            {
                model.Subcategories = await _context.Subcategories.Where(s => s.CategoryId == model.CategoryId).ToListAsync();
                return View(model);
            }
            var category = await _context.Categories.FindAsync(model.CategoryId);
            if (category == null) return NotFound();
            category.Name = model.Name;
            category.Description = model.Description;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Category updated.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Subcategories).ThenInclude(s => s.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            int subCount = category.Subcategories.Count;

            if (category.IsActive)
            {
                foreach (var sub in category.Subcategories)
                {
                    foreach (var p in sub.Products) { p.IsActiveSnapshot = p.IsActive; p.IsActive = false; }
                    sub.IsActive = false;
                }
                category.IsActive = false;
                if (user != null) await _activityLog.LogAsync(user.Id, "Category Deactivated", "Category", id, category.Name);
                TempData["Success"] = $"Category '{category.Name}' and {subCount} subcategorie(s) deactivated.";
            }
            else
            {
                foreach (var sub in category.Subcategories)
                {
                    foreach (var p in sub.Products) { p.IsActive = p.IsActiveSnapshot ?? true; p.IsActiveSnapshot = null; }
                    sub.IsActive = true;
                }
                category.IsActive = true;
                if (user != null) await _activityLog.LogAsync(user.Id, "Category Restored", "Category", id, category.Name);
                TempData["Success"] = $"Category '{category.Name}' and its subcategories restored.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Categories));
        }

        // ── Private helpers ────────────────────────────────────────────

        private async Task<double> GetOnTimeRateAsync()
        {
            var delivered = await _context.Shipments
                .Where(s => s.Status == ShipmentStatus.Delivered && s.ActualDeliveryDate.HasValue && s.EstimatedDeliveryDate.HasValue)
                .ToListAsync();

            if (!delivered.Any()) return 0;
            var onTime = delivered.Count(s => s.ActualDeliveryDate <= s.EstimatedDeliveryDate);
            return Math.Round((double)onTime / delivered.Count * 100, 1);
        }

        private async Task<List<CategoryRevenueItem>> GetTopCategoriesAsync()
        {
            var raw = await _context.OrderItems
                .Include(oi => oi.Product).ThenInclude(p => p!.Subcategory!).ThenInclude(s => s.Category)
                .GroupBy(oi => oi.Product!.Subcategory!.Category!.Name)
                .Select(g => new { Category = g.Key, Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity) })
                .ToListAsync();

            return raw.Select(r => new CategoryRevenueItem { Category = r.Category, Revenue = r.Revenue }).ToList();
        }

        // ── Subcategory Management ─────────────────────────────────────────

        public async Task<IActionResult> Subcategories(int? categoryId, int page = 1)
        {
            const int pageSize = 15;
            var query = _context.Subcategories
                .Include(s => s.Category)
                .Include(s => s.Products)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(s => s.CategoryId == categoryId);

            query = query.OrderBy(s => s.Category!.Name).ThenBy(s => s.Name);
            var total = await query.CountAsync();

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.TotalCount = total;
            return View(await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> CreateSubcategory()
        {
            var vm = new PharmaSkincare.ViewModels.SubcategoryViewModel();
            await PopulateSubcategoryDropdowns(vm);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubcategory(PharmaSkincare.ViewModels.SubcategoryViewModel vm)
        {
            if (!ModelState.IsValid) { await PopulateSubcategoryDropdowns(vm); return View(vm); }

            string? imageUrl = null;
            if (vm.MainImageFile != null)
            {
                try { imageUrl = await _fileService.SaveImageAsync(vm.MainImageFile, "subcategories"); }
                catch (Exception ex) { ModelState.AddModelError("MainImageFile", ex.Message); await PopulateSubcategoryDropdowns(vm); return View(vm); }
            }

            var sub = new PharmaSkincare.Models.Subcategory
            {
                CategoryId = vm.CategoryId,
                Name = vm.Name,
                Description = vm.Description,
                SizeType = vm.SizeType,
                FormulaType = vm.FormulaType,
                ProductFormat = vm.ProductFormat,
                HealthBenefits = vm.HealthBenefits,
                MainImageUrl = imageUrl,
                IsActive = true
            };
            _context.Subcategories.Add(sub);
            await _context.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);
            if (user != null) await _activityLog.LogAsync(user.Id, "Subcategory Created", "Subcategory", sub.SubcategoryId, sub.Name);
            TempData["Success"] = $"Subcategory '{sub.Name}' created.";
            return RedirectToAction(nameof(Subcategories));
        }

        [HttpGet]
        public async Task<IActionResult> EditSubcategory(int id)
        {
            var sub = await _context.Subcategories.FindAsync(id);
            if (sub == null) return NotFound();

            var vm = new PharmaSkincare.ViewModels.SubcategoryViewModel
            {
                SubcategoryId = sub.SubcategoryId,
                CategoryId = sub.CategoryId,
                Name = sub.Name,
                Description = sub.Description,
                SizeType = sub.SizeType,
                FormulaType = sub.FormulaType,
                ProductFormat = sub.ProductFormat,
                HealthBenefits = sub.HealthBenefits,
                MainImageUrl = sub.MainImageUrl,
                IsActive = sub.IsActive
            };
            await PopulateSubcategoryDropdowns(vm);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubcategory(PharmaSkincare.ViewModels.SubcategoryViewModel vm)
        {
            if (!ModelState.IsValid) { await PopulateSubcategoryDropdowns(vm); return View(vm); }

            var sub = await _context.Subcategories.FindAsync(vm.SubcategoryId);
            if (sub == null) return NotFound();

            // SizeType is locked — cannot change if products exist
            if (sub.SizeType != vm.SizeType && await _context.Products.AnyAsync(p => p.SubcategoryId == vm.SubcategoryId))
            {
                ModelState.AddModelError("SizeType", "Cannot change the size type once products have been added.");
                await PopulateSubcategoryDropdowns(vm);
                return View(vm);
            }

            sub.CategoryId = vm.CategoryId;
            sub.Name = vm.Name;
            sub.Description = vm.Description;
            sub.SizeType = vm.SizeType;
            sub.FormulaType = vm.FormulaType;
            sub.ProductFormat = vm.ProductFormat;
            sub.HealthBenefits = vm.HealthBenefits;
            sub.IsActive = vm.IsActive;

            if (vm.MainImageFile != null)
            {
                try { sub.MainImageUrl = await _fileService.SaveImageAsync(vm.MainImageFile, "subcategories"); }
                catch (Exception ex) { ModelState.AddModelError("MainImageFile", ex.Message); await PopulateSubcategoryDropdowns(vm); return View(vm); }
            }

            await _context.SaveChangesAsync();
            var user = await _userManager.GetUserAsync(User);
            if (user != null) await _activityLog.LogAsync(user.Id, "Subcategory Updated", "Subcategory", sub.SubcategoryId, sub.Name);
            TempData["Success"] = $"Subcategory '{sub.Name}' updated.";
            return RedirectToAction(nameof(Subcategories));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSubcategory(int id)
        {
            var sub = await _context.Subcategories
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.SubcategoryId == id);
            if (sub == null) return NotFound();

            if (sub.IsActive)
            {
                foreach (var p in sub.Products) { p.IsActiveSnapshot = p.IsActive; p.IsActive = false; }
                sub.IsActive = false;
                TempData["Success"] = $"'{sub.Name}' deactivated.";
            }
            else
            {
                foreach (var p in sub.Products) { p.IsActive = p.IsActiveSnapshot ?? true; p.IsActiveSnapshot = null; }
                sub.IsActive = true;
                TempData["Success"] = $"'{sub.Name}' restored.";
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Subcategories));
        }

        private async Task PopulateSubcategoryDropdowns(PharmaSkincare.ViewModels.SubcategoryViewModel vm)
        {
            vm.Categories = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(),
                "CategoryId", "Name", vm.CategoryId);
        }
    }
}
