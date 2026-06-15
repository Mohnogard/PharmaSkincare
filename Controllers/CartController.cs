using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSkincare.Data;
using PharmaSkincare.Models;
using PharmaSkincare.Services;

namespace PharmaSkincare.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cart;
        private readonly ApplicationDbContext _context;

        public CartController(ICartService cart, ApplicationDbContext context)
        {
            _cart = cart;
            _context = context;
        }

        public IActionResult Index() => View(_cart.GetCart());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int qty = 1, string? returnUrl = null)
        {
            var isAjax = Request.Headers.ContainsKey("X-Ajax-Cart");
            var product = await _context.Products
                .Include(p => p.Subcategory!).ThenInclude(s => s.Category)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product?.Subcategory == null || !product.IsActive || !product.Subcategory.IsActive)
            {
                if (isAjax) return Json(new { success = false, message = "This product is unavailable." });
                return RedirectToLocal(returnUrl);
            }

            if (product.StockQuantity <= 0)
            {
                if (isAjax) return Json(new { success = false, message = "This item is out of stock." });
                TempData["Warning"] = "This item is out of stock.";
                return RedirectToLocal(returnUrl);
            }

            var currentInCart = _cart.GetCart().FirstOrDefault(i => i.ProductId == productId)?.Quantity ?? 0;
            var maxAddable = product.StockQuantity - currentInCart;

            if (maxAddable <= 0)
            {
                if (isAjax) return Json(new { success = false, message = $"You already have all {product.StockQuantity} available in your basket." });
                TempData["Warning"] = "You already have all available stock in your basket.";
                return RedirectToLocal(returnUrl);
            }

            var actualQty = Math.Min(Math.Max(1, qty), maxAddable);
            var displayName = product.Subcategory.SizeType != SizeType.None && !string.IsNullOrEmpty(product.Size)
                ? $"{product.Subcategory.Name} — {product.Size}"
                : product.Subcategory.Name;
            var image = product.ImageUrl ?? product.Subcategory.MainImageUrl;
            var categoryName = product.Subcategory.Category?.Name;

            _cart.AddItem(productId, displayName, product.FinalPrice, image, product.SKU, categoryName, actualQty, product.Size);

            if (isAjax)
            {
                var count = _cart.GetCart().Sum(i => i.Quantity);
                var msg = actualQty < qty
                    ? $"Added {actualQty} to your basket (stock limit reached)."
                    : $"\"{displayName}\" added to your basket.";
                return Json(new { success = true, cartCount = count, message = msg });
            }

            TempData["Success"] = $"\"{displayName}\" added to your basket.";
            return RedirectToLocal(returnUrl);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            _cart.RemoveItem(productId);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQty(int productId, int qty)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null) qty = Math.Min(qty, product.StockQuantity);
            _cart.UpdateQuantity(productId, Math.Max(1, qty));
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            _cart.Clear();
            return RedirectToAction(nameof(Index));
        }

        private IActionResult RedirectToLocal(string? url)
        {
            if (!string.IsNullOrEmpty(url) && Url.IsLocalUrl(url)) return Redirect(url);
            return RedirectToAction(nameof(Index));
        }
    }
}
