using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Services;

public interface IInvoiceService
{
    Task<IEnumerable<InvoiceSummaryDto>> GetInvoiceSummariesAsync();
    Task<InvoiceDetailDto?> GetInvoiceByIdAsync(Guid id);
    Task<Guid> CreateInvoiceAsync(CreateInvoiceDto dto);
    Task<Guid> UpdateInvoiceAsync(UpdateInvoiceDto dto);
}
