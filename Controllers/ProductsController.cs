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
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly IActivityLogService _activityLog;
        private readonly IFileService _fileService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductsController(IProductService productService, IActivityLogService activityLog,
            IFileService fileService, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _productService = productService;
            _activityLog = activityLog;
            _fileService = fileService;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, int? subcategoryId, bool? lowStock, int page = 1)
        {
            var vm = await _productService.GetProductsAsync(search, subcategoryId, lowStock, null, page, 10);
            vm.Subcategories = new SelectList(
                await _context.Subcategories.Include(s => s.Category).OrderBy(s => s.Name).ToListAsync(),
                "SubcategoryId", "Name");
            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new ProductViewModel();
            await PopulateDropdowns(vm);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel vm)
        {
            if (!ModelState.IsValid) { await PopulateDropdowns(vm); return View(vm); }

            if (await _productService.SKUExistsAsync(vm.SKU))
            {
                ModelState.AddModelError("SKU", "This SKU already exists.");
                await PopulateDropdowns(vm);
                return View(vm);
            }

            string? imageUrl = null;
            if (vm.ImageFile != null)
            {
                try { imageUrl = await _fileService.SaveImageAsync(vm.ImageFile); }
                catch (Exception ex) { ModelState.AddModelError("ImageFile", ex.Message); await PopulateDropdowns(vm); return View(vm); }
            }

            var product = await _productService.CreateProductAsync(vm, imageUrl);

            if (vm.AdditionalImages != null)
            {
                int order = 0;
                foreach (var file in vm.AdditionalImages)
                {
                    var url = await _fileService.SaveImageAsync(file, "products");
                    if (url != null)
                        _context.ProductImages.Add(new ProductImage { ProductId = product.ProductId, ImageUrl = url, DisplayOrder = order++ });
                }
                await _context.SaveChangesAsync();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null) await _activityLog.LogAsync(user.Id, "Product Created", "Product", product.ProductId, product.DisplayName);

            TempData["Success"] = $"Product '{product.DisplayName}' created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Subcategory!).ThenInclude(s => s.Category)
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) return NotFound();

            var vm = new ProductViewModel
            {
                ProductId = product.ProductId,
                SubcategoryId = product.SubcategoryId,
                Size = product.Size,
                Price = product.Price,
                DiscountPercent = product.DiscountPercent,
                SKU = product.SKU,
                StockQuantity = product.StockQuantity,
                MinimumStockLevel = product.MinimumStockLevel,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                ExistingImages = product.Images.ToList()
            };

            await PopulateDropdowns(vm);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductViewModel vm)
        {
            if (!ModelState.IsValid) { vm.ExistingImages = await LoadImages(vm.ProductId); await PopulateDropdowns(vm); return View(vm); }

            if (await _productService.SKUExistsAsync(vm.SKU, vm.ProductId))
            {
                ModelState.AddModelError("SKU", "This SKU already exists.");
                vm.ExistingImages = await LoadImages(vm.ProductId);
                await PopulateDropdowns(vm);
                return View(vm);
            }

            string? imageUrl = null;
            if (vm.ImageFile != null)
            {
                try { imageUrl = await _fileService.SaveImageAsync(vm.ImageFile); }
                catch (Exception ex) { ModelState.AddModelError("ImageFile", ex.Message); vm.ExistingImages = await LoadImages(vm.ProductId); await PopulateDropdowns(vm); return View(vm); }
            }

            if (vm.DeleteImageIds?.Any() == true)
            {
                var toDelete = await _context.ProductImages.Where(i => vm.DeleteImageIds.Contains(i.ProductImageId) && i.ProductId == vm.ProductId).ToListAsync();
                _context.ProductImages.RemoveRange(toDelete);
            }

            if (vm.AdditionalImages != null)
            {
                var maxOrder = await _context.ProductImages.Where(i => i.ProductId == vm.ProductId).Select(i => (int?)i.DisplayOrder).MaxAsync() ?? -1;
                foreach (var file in vm.AdditionalImages)
                {
                    var url = await _fileService.SaveImageAsync(file, "products");
                    if (url != null)
                        _context.ProductImages.Add(new ProductImage { ProductId = vm.ProductId, ImageUrl = url, DisplayOrder = ++maxOrder });
                }
            }

            await _productService.UpdateProductAsync(vm, imageUrl);
            var user = await _userManager.GetUserAsync(User);
            if (user != null) await _activityLog.LogAsync(user.Id, "Product Updated", "Product", vm.ProductId, null);

            TempData["Success"] = "Product saved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            await _productService.DeleteProductAsync(id);
            var user = await _userManager.GetUserAsync(User);
            if (user != null) await _activityLog.LogAsync(user.Id, "Product Deactivated", "Product", id, null);
            TempData["Success"] = "Product deactivated.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<ProductImage>> LoadImages(int productId) =>
            await _context.ProductImages.Where(i => i.ProductId == productId).OrderBy(i => i.DisplayOrder).ToListAsync();

        private async Task PopulateDropdowns(ProductViewModel vm)
        {
            var subs = await _context.Subcategories
                .Include(s => s.Category)
                .OrderBy(s => s.Category!.Name).ThenBy(s => s.Name)
                .ToListAsync();
            vm.Subcategories = new SelectList(
                subs.Select(s => new { s.SubcategoryId, Display = $"{s.Category?.Name} › {s.Name}" }),
                "SubcategoryId", "Display", vm.SubcategoryId);
        }
    }
}
