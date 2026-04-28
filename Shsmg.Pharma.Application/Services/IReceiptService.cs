using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Services;

public interface IReceiptService
{
    Task<IEnumerable<ReceiptSummaryDto>> GetReceiptsAsync(string? search = null);
    Task<ReceiptDetailDto?> GetReceiptByIdAsync(Guid id);
    Task<Guid> CreateReceiptAsync(CreateReceiptDto dto);
    Task<Guid> UpdateReceiptAsync(UpdateReceiptDto dto);
    Task DeleteReceiptAsync(Guid id);
}
