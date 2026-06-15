using Microsoft.EntityFrameworkCore;
using PharmaSkincare.Data;
using PharmaSkincare.Models;
using PharmaSkincare.ViewModels;

namespace PharmaSkincare.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;

        public InventoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<InventoryTransactionListViewModel> GetTransactionsAsync(string? search, int? productId, TransactionType? type, DateTime? from, DateTime? to, int page, int pageSize)
        {
            var query = _context.InventoryTransactions
                .Include(t => t.Product).ThenInclude(p => p!.Subcategory!)
                .Include(t => t.User)
                .AsQueryable();

            if (productId.HasValue)
                query = query.Where(t => t.ProductId == productId);

            if (type.HasValue)
                query = query.Where(t => t.TransactionType == type);

            if (from.HasValue)
                query = query.Where(t => t.TransactionDate >= from);

            if (to.HasValue)
                query = query.Where(t => t.TransactionDate <= to);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(t => t.Product!.Subcategory!.Name.Contains(search) || (t.BatchNumber != null && t.BatchNumber.Contains(search)));

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(t => t.TransactionDate)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return new InventoryTransactionListViewModel
            {
                Transactions = items,
                SearchTerm = search,
                ProductFilter = productId,
                TypeFilter = type,
                DateFrom = from,
                DateTo = to,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                TotalCount = total
            };
        }

        public async Task RecordTransactionAsync(InventoryAdjustmentViewModel vm, string userId)
        {
            var product = await _context.Products.FindAsync(vm.ProductId);
            if (product == null) return;

            int quantityBefore = product.StockQuantity;
            int quantityAfter;

            switch (vm.TransactionType)
            {
                case TransactionType.StockIn:
                case TransactionType.Returned:
                    quantityAfter = quantityBefore + vm.Quantity;
                    product.StockQuantity += vm.Quantity;
                    break;
                case TransactionType.StockOut:
                case TransactionType.Damaged:
                case TransactionType.Transfer:
                    quantityAfter = quantityBefore - vm.Quantity;
                    product.StockQuantity -= vm.Quantity;
                    break;
                case TransactionType.Adjustment:
                    quantityAfter = vm.Quantity;
                    product.StockQuantity = vm.Quantity;
                    break;
                default:
                    quantityAfter = quantityBefore;
                    break;
            }

            var transaction = new InventoryTransaction
            {
                ProductId = vm.ProductId,
                TransactionType = vm.TransactionType,
                Quantity = vm.Quantity,
                QuantityBefore = quantityBefore,
                QuantityAfter = quantityAfter,
                TransactionDate = DateTime.UtcNow,
                UserId = userId,
                BatchNumber = vm.BatchNumber,
                ExpiryDate = vm.ExpiryDate,
                Notes = vm.Notes,
                ReferenceNumber = vm.ReferenceNumber
            };

            _context.InventoryTransactions.Add(transaction);
            product.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<InventoryTransaction>> GetProductHistoryAsync(int productId) =>
            await _context.InventoryTransactions
                .Include(t => t.User)
                .Where(t => t.ProductId == productId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
    }
}
