using Microsoft.EntityFrameworkCore;
using PharmaSkincare.Data;
using PharmaSkincare.Models;
using PharmaSkincare.ViewModels;

namespace PharmaSkincare.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ProductListViewModel> GetProductsAsync(string? search, int? subcategoryId, bool? lowStock, string? format, int page, int pageSize)
        {
            var query = _context.Products
                .Include(p => p.Subcategory!).ThenInclude(s => s.Category)
                .Include(p => p.Images)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.SKU.Contains(search) ||
                    p.Subcategory!.Name.Contains(search) ||
                    (p.Size != null && p.Size.Contains(search)));

            if (subcategoryId.HasValue)
                query = query.Where(p => p.SubcategoryId == subcategoryId);

            if (lowStock == true)
                query = query.Where(p => p.StockQuantity <= p.MinimumStockLevel);

            if (!string.IsNullOrWhiteSpace(format) && Enum.TryParse<ProductFormat>(format, out var pf))
                query = query.Where(p => p.Subcategory!.ProductFormat == pf);

            var total = await query.CountAsync();
            var products = await query
                .OrderBy(p => p.Subcategory!.Name).ThenBy(p => p.Price)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new ProductListViewModel
            {
                Products = products,
                SearchTerm = search,
                SubcategoryFilter = subcategoryId,
                LowStockFilter = lowStock,
                FormatFilter = format,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                TotalCount = total
            };
        }

        public async Task<Product?> GetProductByIdAsync(int id) =>
            await _context.Products
                .Include(p => p.Subcategory!).ThenInclude(s => s.Category)
                .Include(p => p.Certificates)
                .Include(p => p.Endorsements).ThenInclude(e => e.Doctor)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.ProductId == id);

        public async Task<Product> CreateProductAsync(ProductViewModel vm, string? imageUrl)
        {
            var product = new Product
            {
                SubcategoryId = vm.SubcategoryId,
                Size = vm.Size,
                Price = vm.Price,
                DiscountPercent = vm.DiscountPercent > 0 ? vm.DiscountPercent : null,
                SKU = vm.SKU,
                StockQuantity = vm.StockQuantity,
                MinimumStockLevel = vm.MinimumStockLevel,
                ImageUrl = imageUrl ?? vm.ImageUrl,
                IsActive = vm.IsActive,
                CreatedDate = DateTime.UtcNow
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task UpdateProductAsync(ProductViewModel vm, string? imageUrl)
        {
            var product = await _context.Products.FindAsync(vm.ProductId);
            if (product == null) return;

            product.SubcategoryId = vm.SubcategoryId;
            product.Size = vm.Size;
            product.Price = vm.Price;
            product.DiscountPercent = vm.DiscountPercent > 0 ? vm.DiscountPercent : null;
            product.SKU = vm.SKU;
            product.StockQuantity = vm.StockQuantity;
            product.MinimumStockLevel = vm.MinimumStockLevel;
            product.IsActive = vm.IsActive;
            product.UpdatedDate = DateTime.UtcNow;

            if (imageUrl != null) product.ImageUrl = imageUrl;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null) { product.IsActive = false; await _context.SaveChangesAsync(); }
        }

        public async Task<bool> SKUExistsAsync(string sku, int? excludeId = null)
        {
            var query = _context.Products.Where(p => p.SKU == sku);
            if (excludeId.HasValue) query = query.Where(p => p.ProductId != excludeId);
            return await query.AnyAsync();
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync() =>
            await _context.Products
                .Include(p => p.Subcategory!).ThenInclude(s => s.Category)
                .Where(p => p.IsActive && p.StockQuantity <= p.MinimumStockLevel)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();

        public async Task<IEnumerable<Product>> GetAllActiveAsync() =>
            await _context.Products
                .Include(p => p.Subcategory!).ThenInclude(s => s.Category)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Subcategory!.Name)
                .ToListAsync();
    }
}
