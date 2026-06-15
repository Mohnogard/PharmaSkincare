using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PharmaSkincare.Models;
using PharmaSkincare.Services;

namespace PharmaSkincare.Controllers
{
    [Authorize(Roles = "Customer")]
    public class WishlistController : Controller
    {
        private readonly IWishlistService _wishlist;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(IWishlistService wishlist, UserManager<ApplicationUser> userManager)
        {
            _wishlist = wishlist;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            return View(await _wishlist.GetWishlistAsync(user.Id));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int productId, string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var added = await _wishlist.ToggleAsync(user.Id, productId);
            TempData[added ? "Success" : "Info"] = added
                ? "Added to your wishlist."
                : "Removed from your wishlist.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }
    }
}
