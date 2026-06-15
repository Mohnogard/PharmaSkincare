using PharmaSkincare.Models;
using PharmaSkincare.ViewModels;

namespace PharmaSkincare.Services
{
    public interface IInventoryService
    {
        Task<InventoryTransactionListViewModel> GetTransactionsAsync(string? search, int? productId, TransactionType? type, DateTime? from, DateTime? to, int page, int pageSize);
        Task RecordTransactionAsync(InventoryAdjustmentViewModel vm, string userId);
        Task<IEnumerable<InventoryTransaction>> GetProductHistoryAsync(int productId);
    }
}
