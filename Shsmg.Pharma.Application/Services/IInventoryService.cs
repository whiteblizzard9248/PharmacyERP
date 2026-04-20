using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Services;

public interface IInventoryService
{
    Task<IEnumerable<InventoryItemSummaryDto>> GetInventoryItemsAsync(string? searchQuery = null);
    Task<InventoryItemDto?> GetInventoryItemByIdAsync(Guid id);
    Task<IReadOnlyList<InventoryItemDto>> SearchInventoryItemsAsync(string query, int take = 10);
    Task<Guid> SaveInventoryItemAsync(InventoryItemDto dto);
    Task DeleteInventoryItemAsync(Guid id);
}
