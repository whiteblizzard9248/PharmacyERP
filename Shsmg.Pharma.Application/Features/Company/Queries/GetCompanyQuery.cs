using MediatR;
using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Features.Company.Queries;

public record GetCompanyQuery : IRequest<CompanyDto?>;

public sealed class GetCompanyQueryHandler(IPharmacyDbContext context) : IRequestHandler<GetCompanyQuery, CompanyDto?>
{
    private readonly IPharmacyDbContext _context = context;

    public async Task<CompanyDto?> Handle(GetCompanyQuery request, CancellationToken cancellationToken)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => !c.IsDeleted, cancellationToken);

        return company is null
            ? null
            : new CompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                Address = company.Address ?? string.Empty,
                LicenseNumber = company.LicenseNumber,
                ContactNumber = company.ContactNumber
            };
    }
}
