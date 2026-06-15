using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaSkincare.Services;

namespace PharmaSkincare.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Customer"))
                return RedirectToAction("Profile", "Account");

            var dashboard = await _dashboardService.GetDashboardDataAsync();
            return View(dashboard);
        }
    }
}
