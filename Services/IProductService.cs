using PharmaSkincare.Models;
using PharmaSkincare.ViewModels;

namespace PharmaSkincare.Services
{
    public interface IProductService
    {
        Task<ProductListViewModel> GetProductsAsync(string? search, int? subcategoryId, bool? lowStock, string? format, int page, int pageSize);
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product> CreateProductAsync(ProductViewModel vm, string? imageUrl);
        Task UpdateProductAsync(ProductViewModel vm, string? imageUrl);
        Task DeleteProductAsync(int id);
        Task<bool> SKUExistsAsync(string sku, int? excludeId = null);
        Task<IEnumerable<Product>> GetLowStockProductsAsync();
        Task<IEnumerable<Product>> GetAllActiveAsync();
    }
}
