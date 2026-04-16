using MediatR;
using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Application.DTOs;
using Shsmg.Pharma.Application.Common;

namespace Shsmg.Pharma.Application.Features.Company.Commands;

public record CreateCompanyCommand(CompanyDto CompanyDto) : IRequest<Guid>;

public class CreateCompanyHandler : IRequestHandler<CreateCompanyCommand, Guid>
{
    private readonly IPharmacyDbContext _context;

    public CreateCompanyHandler(IPharmacyDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var dto = request.CompanyDto;

        // 1. Use provided Id when editing, otherwise fall back to the first active company.
        var existingCompany = dto.Id != Guid.Empty
            ? await _context.Companies.FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted, cancellationToken)
            : await _context.Companies.FirstOrDefaultAsync(c => !c.IsDeleted, cancellationToken);

        if (existingCompany != null)
        {
            existingCompany.Name = dto.Name;
            existingCompany.Address = dto.Address;
            existingCompany.LicenseNumber = dto.LicenseNumber;
            existingCompany.ContactNumber = dto.ContactNumber;
            existingCompany.InvoiceHeaderText = dto.InvoiceHeaderText;
            existingCompany.InvoiceFooterText = dto.InvoiceFooterText;
            existingCompany.PrintShowGst = dto.PrintShowGst;
            existingCompany.PrintShowExpiry = dto.PrintShowExpiry;
            existingCompany.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return existingCompany.Id;
        }

        // 2. Create a new store profile when none exists.
        var newCompany = new Shsmg.Pharma.Domain.Models.Company
        {
            Name = dto.Name,
            Address = dto.Address,
            LicenseNumber = dto.LicenseNumber,
            ContactNumber = dto.ContactNumber,
            InvoiceHeaderText = dto.InvoiceHeaderText,
            InvoiceFooterText = dto.InvoiceFooterText,
            PrintShowGst = dto.PrintShowGst,
            PrintShowExpiry = dto.PrintShowExpiry
        };

        _context.Companies.Add(newCompany);
        await _context.SaveChangesAsync(cancellationToken);

        return newCompany.Id;
    }
}