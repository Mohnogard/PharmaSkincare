namespace PharmaSkincare.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSku { get; set; }
        public string? ImageUrl { get; set; }
        public string? CategoryName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        // VariantSize kept for order display ("100ml" shown in order summary)
        public string? VariantSize { get; set; }
        public decimal Subtotal => UnitPrice * Quantity;
    }
}
