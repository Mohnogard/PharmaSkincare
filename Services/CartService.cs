using System.Text.Json;
using PharmaSkincare.Models;

namespace PharmaSkincare.Services
{
    public class CartService : ICartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CartKey = "PS_Cart";

        public CartService(IHttpContextAccessor httpContextAccessor) =>
            _httpContextAccessor = httpContextAccessor;

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public List<CartItem> GetCart()
        {
            var json = Session.GetString(CartKey);
            if (string.IsNullOrEmpty(json)) return new();
            try { return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new(); }
            catch { return new(); }
        }

        private void Save(List<CartItem> cart) =>
            Session.SetString(CartKey, JsonSerializer.Serialize(cart));

        public void AddItem(int productId, string name, decimal price, string? imageUrl, string? sku, string? category = null, int qty = 1, string? variantSize = null)
        {
            var cart = GetCart();
            var existing = cart.FirstOrDefault(i => i.ProductId == productId);
            if (existing != null)
                existing.Quantity = Math.Min(existing.Quantity + qty, 99);
            else
                cart.Add(new CartItem
                {
                    ProductId = productId,
                    ProductName = name,
                    UnitPrice = price,
                    ImageUrl = imageUrl,
                    ProductSku = sku,
                    CategoryName = category,
                    Quantity = qty,
                    VariantSize = variantSize
                });
            Save(cart);
        }

        public void RemoveItem(int productId)
        {
            var cart = GetCart();
            cart.RemoveAll(i => i.ProductId == productId);
            Save(cart);
        }

        public void UpdateQuantity(int productId, int qty)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                if (qty <= 0) cart.Remove(item);
                else item.Quantity = Math.Min(qty, 99);
            }
            Save(cart);
        }

        public void Clear() => Session.Remove(CartKey);

        public int GetItemCount() => GetCart().Sum(i => i.Quantity);
        public decimal GetTotal() => GetCart().Sum(i => i.Subtotal);
    }
}
