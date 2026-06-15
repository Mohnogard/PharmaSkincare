using PharmaSkincare.ViewModels;

namespace PharmaSkincare.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync();
    }
}
