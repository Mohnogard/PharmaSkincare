using PharmaSkincare.Models;

namespace PharmaSkincare.Services
{
    public interface ICartService
    {
        List<CartItem> GetCart();
        void AddItem(int productId, string name, decimal price, string? imageUrl, string? sku, string? category = null, int qty = 1, string? variantSize = null);
        void RemoveItem(int productId);
        void UpdateQuantity(int productId, int qty);
        void Clear();
        int GetItemCount();
        decimal GetTotal();
    }
}
