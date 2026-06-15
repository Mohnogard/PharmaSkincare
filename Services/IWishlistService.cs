using PharmaSkincare.Models;

namespace PharmaSkincare.Services
{
    public interface IWishlistService
    {
        Task<List<WishlistItem>> GetWishlistAsync(string userId);
        Task<bool> ToggleAsync(string userId, int productId);
        Task<bool> IsInWishlistAsync(string userId, int productId);
        Task<int> GetCountAsync(string userId);
    }
}
