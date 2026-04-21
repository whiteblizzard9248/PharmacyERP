using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;
using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Application.Services;

public sealed class CompanyService(IPharmacyDbContext context, ILogger<CompanyService> logger) : ICompanyService
{
    private readonly IPharmacyDbContext _context = context;
    private readonly ILogger<CompanyService> _logger = logger;

    public async Task<CompanyDto?> GetCompanyAsync()
    {
        _logger.LogInformation("Attempting to retrieve company information.");
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
            ContactNumber = company.ContactNumber,
            LicenseKey = company.LicenseKey,
            LicenseExpiry = company.LicenseExpiry,
            HardwareId = company.HardwareId,
            IsActivated = company.IsActivated

        };
    }

    public async Task<Guid> CreateOrUpdateCompanyAsync(CompanyDto dto)
    {
        _logger.LogInformation("Attempting to create or update company information.");
        var existingCompany = dto.Id != Guid.Empty
            ? await _context.Companies.FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted)
            : await _context.Companies.FirstOrDefaultAsync(c => !c.IsDeleted);

        if (existingCompany != null)
        {
            existingCompany.Name = dto.Name;
            existingCompany.Address = dto.Address;
            existingCompany.LicenseNumber = dto.LicenseNumber;
            existingCompany.ContactNumber = dto.ContactNumber;
            existingCompany.LicenseKey = dto.LicenseKey;
            existingCompany.LicenseExpiry = dto.LicenseExpiry;
            existingCompany.HardwareId = dto.HardwareId;
            existingCompany.IsActivated = dto.IsActivated;
            existingCompany.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Company information updated successfully.");
            return existingCompany.Id;
        }

        var newCompany = new Company
        {
            Name = dto.Name,
            Address = dto.Address,
            LicenseNumber = dto.LicenseNumber,
            ContactNumber = dto.ContactNumber,
            LicenseKey = dto.LicenseKey,
            LicenseExpiry = dto.LicenseExpiry,
            HardwareId = dto.HardwareId
        };

        _context.Companies.Add(newCompany);
        _logger.LogInformation("Creating new company information.");
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return newCompany.Id;
    }
}
