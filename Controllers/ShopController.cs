using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSkincare.Data;
using PharmaSkincare.Models;
using PharmaSkincare.Services;
using PharmaSkincare.ViewModels;
using Stripe;

namespace PharmaSkincare.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOrderService _orderService;
        private readonly IConfiguration _configuration;
        private readonly ICartService _cartService;
        private readonly IEmailService _emailService;

        public ShopController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
            IOrderService orderService, IConfiguration configuration, ICartService cartService,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _orderService = orderService;
            _configuration = configuration;
            _cartService = cartService;
            _emailService = emailService;
        }

        private async Task SetNavCategories() =>
            ViewBag.NavCategories = await _context.Categories.Where(c => c.IsActive).ToListAsync();

        private async Task SetWishlistIds()
        {
            if (User.IsInRole("Customer"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var ids = await _context.WishlistItems
                        .Where(w => w.CustomerId == user.Id)
                        .Select(w => w.ProductId)
                        .ToListAsync();
                    ViewBag.WishlistProductIds = new HashSet<int>(ids);
                    return;
                }
            }
            ViewBag.WishlistProductIds = new HashSet<int>();
        }

        // ── Browse pages — show Subcategory cards ─────────────────────────

        public async Task<IActionResult> Index(string? search, int? categoryId, string? format, string? sort, int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Subcategories
                .Include(s => s.Category)
                .Include(s => s.Products.Where(p => p.IsActive))
                    .ThenInclude(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Where(s => s.IsActive && s.Products.Any(p => p.IsActive))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s => s.Name.Contains(search) || (s.Description != null && s.Description.Contains(search)));

            if (categoryId.HasValue)
                query = query.Where(s => s.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(format) && Enum.TryParse<ProductFormat>(format, out var pf))
                query = query.Where(s => s.ProductFormat == pf);

            query = sort switch
            {
                "name" => query.OrderBy(s => s.Name),
                _ => query.OrderByDescending(s => s.CreatedDate)
            };

            var total = await query.CountAsync();
            var subcategories = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Sort = sort;
            ViewBag.SearchTerm = search;
            ViewBag.CategoryFilter = categoryId;
            ViewBag.FormatFilter = format;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.TotalCount = total;
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();

            await SetNavCategories();
            await SetWishlistIds();

            if (string.IsNullOrWhiteSpace(search) && !categoryId.HasValue && string.IsNullOrWhiteSpace(format))
            {
                ViewBag.HomepageTestimonials = await _context.Testimonials
                    .Where(t => t.IsApproved)
                    .OrderByDescending(t => t.CreatedDate)
                    .Take(6)
                    .ToListAsync();
            }

            return View(subcategories);
        }

        public async Task<IActionResult> ShopAll(string? search, int? categoryId, string? format, string? sort, decimal? minPrice, decimal? maxPrice, string? benefit, int page = 1)
        {
            const int pageSize = 24;
            var query = _context.Subcategories
                .Include(s => s.Category)
                .Include(s => s.Products.Where(p => p.IsActive))
                    .ThenInclude(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Where(s => s.IsActive && s.Products.Any(p => p.IsActive))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s => s.Name.Contains(search) || (s.Description != null && s.Description.Contains(search)));

            if (categoryId.HasValue)
                query = query.Where(s => s.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(format) && Enum.TryParse<ProductFormat>(format, out var pf))
                query = query.Where(s => s.ProductFormat == pf);

            if (!string.IsNullOrWhiteSpace(benefit))
                query = query.Where(s => s.HealthBenefits != null && s.HealthBenefits.Contains(benefit));

            query = sort switch
            {
                "name_asc"  => query.OrderBy(s => s.Name),
                "name_desc" => query.OrderByDescending(s => s.Name),
                _           => query.OrderByDescending(s => s.CreatedDate)
            };

            var total = await query.CountAsync();
            var subcategories = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // Price filter applied in memory (product prices vary per size)
            if (minPrice.HasValue)
                subcategories = subcategories.Where(s => s.Products.Any(p => p.FinalPrice >= minPrice.Value)).ToList();
            if (maxPrice.HasValue)
                subcategories = subcategories.Where(s => s.Products.Any(p => p.FinalPrice <= maxPrice.Value)).ToList();

            ViewBag.Sort = sort;
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Format = format;
            ViewBag.Benefit = benefit;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.TotalCount = total;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            ViewBag.Formats = Enum.GetValues<ProductFormat>();

            await SetNavCategories();
            await SetWishlistIds();
            return View(subcategories);
        }

        // ── Subcategory detail page ────────────────────────────────────────

        public async Task<IActionResult> Subcategory(int id)
        {
            var sub = await _context.Subcategories
                .Include(s => s.Category)
                .Include(s => s.Products.Where(p => p.IsActive))
                    .ThenInclude(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Include(s => s.Products.Where(p => p.IsActive))
                    .ThenInclude(p => p.Reviews.Where(r => r.IsApproved))
                .FirstOrDefaultAsync(s => s.SubcategoryId == id && s.IsActive);

            if (sub == null) return NotFound();

            ViewBag.RelatedSubcategories = await _context.Subcategories
                .Include(s => s.Products.Where(p => p.IsActive))
                .Where(s => s.CategoryId == sub.CategoryId && s.SubcategoryId != id && s.IsActive)
                .Take(4)
                .ToListAsync();

            bool hasPurchased = false;
            if (User.IsInRole("Customer"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    hasPurchased = await _context.OrderItems
                        .AnyAsync(oi => oi.Product!.SubcategoryId == id && oi.Order!.CustomerId == user.Id);
                }
            }
            ViewBag.HasPurchasedSubcategory = hasPurchased;

            // Which products are wishlisted
            var wishlistIds = new HashSet<int>();
            if (User.IsInRole("Customer"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var ids = await _context.WishlistItems
                        .Where(w => w.CustomerId == user.Id &&
                                    sub.Products.Select(p => p.ProductId).Contains(w.ProductId))
                        .Select(w => w.ProductId)
                        .ToListAsync();
                    wishlistIds = new HashSet<int>(ids);
                }
            }
            ViewBag.WishlistProductIds = wishlistIds;

            await SetNavCategories();
            return View(sub);
        }

        // Backward compat — old ProductDetails route redirects to Subcategory
        public async Task<IActionResult> ProductDetails(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return RedirectToActionPermanent(nameof(Subcategory), new { id = product.SubcategoryId });
        }

        // ── Other shop pages ───────────────────────────────────────────────

        public async Task<IActionResult> Partners()
        {
            await SetNavCategories();
            var partners = await _context.PharmacyPartners
                .Where(p => p.Status == PartnershipStatus.Active)
                .OrderBy(p => p.Name)
                .ToListAsync();
            return View(partners);
        }

        public async Task<IActionResult> Doctors()
        {
            await SetNavCategories();
            var doctors = await _context.Doctors
                .Include(d => d.Endorsements.Where(e => e.IsActive))
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();
            return View(doctors);
        }

        public async Task<IActionResult> Recommendations()
        {
            await SetNavCategories();
            var subcategories = await _context.Subcategories
                .Include(s => s.Category)
                .Include(s => s.Products.Where(p => p.IsActive))
                    .ThenInclude(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Include(s => s.Products.Where(p => p.IsActive))
                    .ThenInclude(p => p.Endorsements.Where(e => e.IsActive))
                        .ThenInclude(e => e.Doctor)
                .Where(s => s.IsActive && s.Products.Any(p => p.IsActive && p.Endorsements.Any(e => e.IsActive)))
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
            return View(subcategories);
        }

        // ── Reviews ────────────────────────────────────────────────────────

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var hasPurchased = await _context.OrderItems
                .AnyAsync(oi => oi.ProductId == productId && oi.Order!.CustomerId == user.Id);
            if (!hasPurchased)
            {
                TempData["Error"] = "You can only review products you have purchased.";
                return RedirectToAction(nameof(Subcategory), new { id = product.SubcategoryId });
            }

            var existing = await _context.ProductReviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.CustomerId == user.Id);
            if (existing != null)
            {
                existing.Rating = rating;
                existing.Comment = comment;
                existing.CreatedDate = DateTime.UtcNow;
                existing.IsApproved = false;
            }
            else
            {
                _context.ProductReviews.Add(new ProductReview
                {
                    ProductId = productId,
                    CustomerId = user.Id,
                    Rating = rating,
                    Comment = comment,
                    CreatedDate = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Thank you for your review! It will appear once approved.";
            return RedirectToAction(nameof(Subcategory), new { id = product.SubcategoryId });
        }

        // ── Checkout ───────────────────────────────────────────────────────

        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cartItems = _cartService.GetCart();
            if (!cartItems.Any())
            {
                TempData["Error"] = "Your basket is empty.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            var vm = new CreateOrderViewModel
            {
                City = user?.City ?? "",
                Country = user?.Country ?? "Jordan",
                ShippingAddress = user?.Address ?? "",
                PostalCode = user?.PostalCode ?? "",
                CartItems = cartItems.Select(MapCartItem).ToList()
            };

            var pk = _configuration["Stripe:PublishableKey"] ?? "";
            var sk = _configuration["Stripe:SecretKey"] ?? "";
            ViewBag.StripePublishableKey = pk;

            if (!string.IsNullOrEmpty(pk) && !pk.StartsWith("pk_test_YOUR") &&
                !string.IsNullOrEmpty(sk) && !sk.StartsWith("sk_test_YOUR"))
            {
                try
                {
                    StripeConfiguration.ApiKey = sk;
                    var subtotal = cartItems.Sum(i => i.UnitPrice * i.Quantity);
                    var shipping = subtotal >= 40 ? 0m : 5m;
                    var total = subtotal + shipping;
                    // JOD uses 3 decimal places (fils), multiply by 1000
                    var amountInFils = (long)Math.Round(total * 1000);
                    var service = new PaymentIntentService();
                    var intent = service.Create(new PaymentIntentCreateOptions
                    {
                        Amount = amountInFils,
                        Currency = "jod",
                        AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }
                    });
                    ViewBag.StripeClientSecret = intent.ClientSecret;
                }
                catch { /* Stripe unavailable — fall back to cash-only */ }
            }

            await SetNavCategories();
            return View(vm);
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CreateOrderViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var cartItems = _cartService.GetCart();
            if (!cartItems.Any())
            {
                TempData["Error"] = "Your basket is empty.";
                return RedirectToAction(nameof(Index));
            }

            vm.CartItems = cartItems.Select(MapCartItem).ToList();

            if (!ModelState.IsValid)
            {
                ViewBag.StripePublishableKey = _configuration["Stripe:PublishableKey"] ?? "";
                await SetNavCategories();
                return View("Checkout", vm);
            }

            var order = await _orderService.CreateOrderAsync(user.Id, vm);
            _cartService.Clear();

            var confirmedOrder = await _orderService.GetOrderByIdAsync(order.OrderId);
            if (confirmedOrder != null && !string.IsNullOrWhiteSpace(user.Email))
                _ = _emailService.SendOrderConfirmationAsync(user.Email, user.FullName, confirmedOrder);

            TempData["Success"] = $"Order #{order.OrderId} placed! We'll process it shortly.";
            return RedirectToAction("Details", "Orders", new { id = order.OrderId });
        }

        private static CartItemViewModel MapCartItem(CartItem i) => new()
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            ImageUrl = i.ImageUrl,
            VariantSize = i.VariantSize
        };
    }
}
