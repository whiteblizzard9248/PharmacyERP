using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Services;

public interface IPurchaseInvoiceService
{
    Task<IEnumerable<PurchaseInvoiceSummaryDto>> GetPurchaseInvoicesAsync(string? search = null);
    Task<PurchaseInvoiceDetailDto?> GetPurchaseInvoiceByIdAsync(Guid id);
    Task<Guid> CreatePurchaseInvoiceAsync(CreatePurchaseInvoiceDto dto);
    Task<Guid> UpdatePurchaseInvoiceAsync(UpdatePurchaseInvoiceDto dto);
    Task DeletePurchaseInvoiceAsync(Guid id);
}
