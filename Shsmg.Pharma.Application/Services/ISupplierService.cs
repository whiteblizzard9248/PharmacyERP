using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Services;

public interface ISupplierService
{
    Task<IEnumerable<SupplierDto>> GetSuppliersAsync(string? search = null);
    Task<SupplierDto?> GetSupplierByIdAsync(Guid id);
    Task<Guid> SaveSupplierAsync(SupplierDto dto);
    Task DeleteSupplierAsync(Guid id);
}
