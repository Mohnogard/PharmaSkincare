using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PharmaSkincare.Models;
using PharmaSkincare.Services;
using PharmaSkincare.ViewModels;
using Stripe;
using System.Security.Claims;

namespace PharmaSkincare.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IActivityLogService _activityLog;
        private readonly IFileService _fileService;
        private readonly IOrderService _orderService;
        private readonly IWishlistService _wishlistService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AccountController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IActivityLogService activityLog,
            IFileService fileService,
            IOrderService orderService,
            IWishlistService wishlistService,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _activityLog = activityLog;
            _fileService = fileService;
            _orderService = orderService;
            _wishlistService = wishlistService;
            _configuration = configuration;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");
            ViewData["ReturnUrl"] = returnUrl;
            var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            ViewBag.HasGoogleAuth = schemes.Any(s => s.Name == "Google");
            return View("Auth", new AuthViewModel { ActiveTab = "login" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewData["ReturnUrl"] = returnUrl;
                return View("Auth", new AuthViewModel { Login = vm, ActiveTab = "login" });
            }

            // Block deactivated accounts before a session is ever created
            var userCheck = await _userManager.FindByEmailAsync(vm.Email);
            if (userCheck != null && !userCheck.IsActive)
            {
                ModelState.AddModelError("", "Your account has been deactivated. Contact an administrator.");
                ViewData["ReturnUrl"] = returnUrl;
                return View("Auth", new AuthViewModel { Login = vm, ActiveTab = "login" });
            }

            var result = await _signInManager.PasswordSignInAsync(vm.Email, vm.Password, vm.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(vm.Email);
                if (user != null)
                {
                    user.LastLoginDate = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                    await _activityLog.LogAsync(user.Id, "User Login", "Account", null, null, HttpContext.Connection.RemoteIpAddress?.ToString());
                }

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                if (user != null && await _userManager.IsInRoleAsync(user, "Customer"))
                    return RedirectToAction("Index", "Shop");
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
                ModelState.AddModelError("", "Account is locked due to multiple failed attempts. Try again later.");
            else
                ModelState.AddModelError("", "Invalid email or password.");

            ViewData["ReturnUrl"] = returnUrl;
            return View("Auth", new AuthViewModel { Login = vm, ActiveTab = "login" });
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");
            return View("Auth", new AuthViewModel { ActiveTab = "register" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("Auth", new AuthViewModel { Register = vm, ActiveTab = "register" });

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                PhoneNumber = vm.Phone,
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, vm.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");
                await _signInManager.SignInAsync(user, isPersistent: false);
                await _activityLog.LogAsync(user.Id, "User Registered", "Account");
                return RedirectToAction("Index", "Shop");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View("Auth", new AuthViewModel { Register = vm, ActiveTab = "register" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                    await _activityLog.LogAsync(user.Id, "User Logout", "Account");
            }
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Shop");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Success"] = "If your email is registered, you will receive a password reset link shortly.";
                return RedirectToAction("Login");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Account", new { token, email = vm.Email }, Request.Scheme)!;

            await _emailService.SendPasswordResetEmailAsync(user.Email!, user.FullName, resetLink);
            TempData["Success"] = "If your email is registered, you will receive a password reset link shortly.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            var vm = new ResetPasswordViewModel { Token = token, Email = email };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user == null) { TempData["Error"] = "Invalid request."; return RedirectToAction("Login"); }

            var result = await _userManager.ResetPasswordAsync(user, vm.Token, vm.Password);
            if (result.Succeeded)
            {
                TempData["Success"] = "Password reset successfully. You can now log in.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(vm);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var vm = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                City = user.City,
                Country = user.Country,
                PostalCode = user.PostalCode,
                CurrentImageUrl = user.ProfileImageUrl
            };

            if (await _userManager.IsInRoleAsync(user, "Customer"))
            {
                var orders = await _orderService.GetCustomerOrdersAsync(user.Id);
                var wishlistItems = await _wishlistService.GetWishlistAsync(user.Id);
                ViewBag.AllOrders = orders.ToList();
                ViewBag.WishlistItems = wishlistItems.ToList();
                ViewBag.StripePublishableKey = _configuration["Stripe:PublishableKey"] ?? "";
                ViewBag.UserEmail = user.Email;
                ViewBag.FullName = user.FullName;
                return View("CustomerProfile", vm);
            }

            return View(vm);
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CreateSetupIntent()
        {
            var secretKey = _configuration["Stripe:SecretKey"] ?? "";
            StripeConfiguration.ApiKey = secretKey;
            var service = new SetupIntentService();
            var options = new SetupIntentCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" }
            };
            var intent = service.Create(options);
            return Json(new { clientSecret = intent.ClientSecret });
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(EditProfileViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FirstName = vm.FirstName;
            user.LastName = vm.LastName;
            user.PhoneNumber = vm.PhoneNumber;
            user.Address = vm.Address;
            user.City = vm.City;
            user.Country = vm.Country;
            user.PostalCode = vm.PostalCode;

            if (vm.ProfileImage != null)
            {
                var imgUrl = await _fileService.SaveImageAsync(vm.ProfileImage, "profiles");
                if (imgUrl != null) user.ProfileImageUrl = imgUrl;
            }

            await _userManager.UpdateAsync(user);
            await _activityLog.LogAsync(user.Id, "Profile Updated", "Account");
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }

        // ── External OAuth ──────────────────────────────────────────
        [HttpGet]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null) { TempData["Error"] = "Social login failed. Please try again."; return RedirectToAction(nameof(Login)); }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                var existingUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (existingUser != null && await _userManager.IsInRoleAsync(existingUser, "Customer"))
                    return RedirectToAction("Index", "Shop");
                return RedirectToAction("Index", "Dashboard");
            }

            // New user via social login
            var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
            var firstName = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value ?? "User";
            var lastName  = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value ?? "";

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Could not retrieve your email from the social provider.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email, Email = email, EmailConfirmed = true,
                    FirstName = firstName, LastName = lastName,
                    IsActive = true, CreatedDate = DateTime.UtcNow
                };
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    TempData["Error"] = string.Join(" ", createResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Login));
                }
                await _userManager.AddToRoleAsync(user, "Customer");
            }

            await _userManager.AddLoginAsync(user, info);
            await _signInManager.SignInAsync(user, isPersistent: false);
            await _activityLog.LogAsync(user.Id, $"Login via {info.LoginProvider}", "Account");
            return RedirectToAction("Index", "Shop");
        }

        public IActionResult AccessDenied() => View();
    }
}
