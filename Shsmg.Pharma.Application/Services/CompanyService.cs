using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;
using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Application.Services;

public sealed class CompanyService(IPharmacyDbContext context) : ICompanyService
{
    private readonly IPharmacyDbContext _context = context;

    public async Task<CompanyDto?> GetCompanyAsync()
    {
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => !c.IsDeleted);

        if (company is null)
            return null;

        return new CompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            Address = company.Address ?? string.Empty,
            LicenseNumber = company.LicenseNumber,
            ContactNumber = company.ContactNumber
        };
    }

    public async Task<Guid> CreateOrUpdateCompanyAsync(CompanyDto dto)
    {
        var existingCompany = dto.Id != Guid.Empty
            ? await _context.Companies.FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted)
            : await _context.Companies.FirstOrDefaultAsync(c => !c.IsDeleted);

        if (existingCompany != null)
        {
            existingCompany.Name = dto.Name;
            existingCompany.Address = dto.Address;
            existingCompany.LicenseNumber = dto.LicenseNumber;
            existingCompany.ContactNumber = dto.ContactNumber;
            existingCompany.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingCompany.Id;
        }

        var newCompany = new Company
        {
            Name = dto.Name,
            Address = dto.Address,
            LicenseNumber = dto.LicenseNumber,
            ContactNumber = dto.ContactNumber,
        };

        _context.Companies.Add(newCompany);
        await _context.SaveChangesAsync();
        return newCompany.Id;
    }
}
