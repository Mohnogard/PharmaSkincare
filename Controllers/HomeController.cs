using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSkincare.Data;
using PharmaSkincare.Models;

namespace PharmaSkincare.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index() => RedirectToAction("Index", "Shop");
        public IActionResult Error() => View();

        private async Task SetNavCategories() =>
            ViewBag.NavCategories = await _context.Categories.Where(c => c.IsActive).ToListAsync();

        public async Task<IActionResult> About()
        {
            await SetNavCategories();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Contact()
        {
            await SetNavCategories();
            return View(new ContactMessage());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactMessage model)
        {
            await SetNavCategories();
            if (!ModelState.IsValid) return View(model);

            model.CreatedDate = DateTime.UtcNow;
            _context.ContactMessages.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Thank you for your message! We'll get back to you within 24 hours.";
            return RedirectToAction(nameof(Contact));
        }

        public async Task<IActionResult> Support()
        {
            await SetNavCategories();
            return View();
        }

        public async Task<IActionResult> Testimonials()
        {
            await SetNavCategories();
            var list = await _context.Testimonials
                .Include(t => t.Customer)
                .Where(t => t.IsApproved)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            bool hasPlacedOrder = false;
            if (User.IsInRole("Customer"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                    hasPlacedOrder = await _context.Orders.AnyAsync(o => o.CustomerId == user.Id);
            }
            ViewBag.HasPlacedOrder = hasPlacedOrder;

            return View(list);
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitTestimonial(string title, string content, int rating)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Only customers who have placed at least one order can leave a testimonial
            var hasOrder = await _context.Orders.AnyAsync(o => o.CustomerId == user.Id);
            if (!hasOrder)
            {
                TempData["Error"] = "You can only leave a testimonial after placing an order with us.";
                return RedirectToAction(nameof(Testimonials));
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Please fill in all required fields.";
                return RedirectToAction(nameof(Testimonials));
            }

            _context.Testimonials.Add(new Testimonial
            {
                Title = title.Trim(),
                Content = content.Trim(),
                AuthorName = user.FullName,
                Rating = Math.Clamp(rating, 1, 5),
                CustomerId = user.Id,
                CreatedDate = DateTime.UtcNow,
                IsApproved = false
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Thank you! Your testimonial is under review and will appear once approved.";
            return RedirectToAction(nameof(Testimonials));
        }
    }
}
