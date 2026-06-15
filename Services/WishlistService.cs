using Microsoft.EntityFrameworkCore;
using PharmaSkincare.Data;
using PharmaSkincare.Models;

namespace PharmaSkincare.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _context;

        public WishlistService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<WishlistItem>> GetWishlistAsync(string userId) =>
            await _context.WishlistItems
                .Include(w => w.Product).ThenInclude(p => p.Subcategory!).ThenInclude(s => s.Category)
                .Where(w => w.CustomerId == userId)
                .OrderByDescending(w => w.AddedDate)
                .ToListAsync();

        public async Task<bool> ToggleAsync(string userId, int productId)
        {
            var existing = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.CustomerId == userId && w.ProductId == productId);

            if (existing != null)
            {
                _context.WishlistItems.Remove(existing);
                await _context.SaveChangesAsync();
                return false;
            }

            _context.WishlistItems.Add(new WishlistItem
            {
                CustomerId = userId,
                ProductId = productId,
                AddedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsInWishlistAsync(string userId, int productId) =>
            await _context.WishlistItems.AnyAsync(w => w.CustomerId == userId && w.ProductId == productId);

        public async Task<int> GetCountAsync(string userId) =>
            await _context.WishlistItems.CountAsync(w => w.CustomerId == userId);
    }
}
