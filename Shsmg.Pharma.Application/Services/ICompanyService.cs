using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Services;

public interface ICompanyService
{
    Task<CompanyDto?> GetCompanyAsync();
    Task<Guid> CreateOrUpdateCompanyAsync(CompanyDto dto);
}
