using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Services;

public interface IPaymentService
{
    Task<IEnumerable<PaymentSummaryDto>> GetPaymentsAsync(string? search = null);
    Task<PaymentDetailDto?> GetPaymentByIdAsync(Guid id);
    Task<Guid> CreatePaymentAsync(CreatePaymentDto dto);
    Task<Guid> UpdatePaymentAsync(UpdatePaymentDto dto);
    Task DeletePaymentAsync(Guid id);
}
