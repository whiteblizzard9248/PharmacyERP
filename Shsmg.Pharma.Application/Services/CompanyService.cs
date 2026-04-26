using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;
using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Application.Services;

public sealed class CompanyService(IPharmacyDbContext context, ILogger<CompanyService> logger, ILicenseService licenseService) : ICompanyService
{
    private readonly IPharmacyDbContext _context = context;
    private readonly ILogger<CompanyService> _logger = logger;
    private readonly ILicenseService _licenseService = licenseService;

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
        var currentValidationResult = _licenseService.Validate(dto.LicenseKey!, dto.HardwareId!);
        var currentValidationResultStr = JsonSerializer.Serialize(currentValidationResult, new JsonSerializerOptions { WriteIndented = true });
        _logger.LogInformation($"License info {currentValidationResultStr}");

        if (existingCompany != null)
        {
            existingCompany.Name = currentValidationResult.LicensePayload!.Company;
            existingCompany.Address = dto.Address;
            existingCompany.LicenseNumber = currentValidationResult.LicensePayload.LicenseId;
            existingCompany.ContactNumber = dto.ContactNumber;
            existingCompany.LicenseKey = dto.LicenseKey;
            existingCompany.LicenseExpiry = currentValidationResult.LicensePayload.Expiry;
            existingCompany.HardwareId = currentValidationResult.LicensePayload.HardwareId;
            existingCompany.IsActivated = dto.IsActivated;
            existingCompany.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Company information updated successfully.");
            return existingCompany.Id;
        }

        var newCompany = new Company
        {
            Name = currentValidationResult.LicensePayload!.Company,
            Address = dto.Address,
            LicenseNumber = currentValidationResult.LicensePayload.LicenseId,
            ContactNumber = dto.ContactNumber,
            LicenseKey = dto.LicenseKey,
            LicenseExpiry = currentValidationResult.LicensePayload.Expiry,
            HardwareId = currentValidationResult.LicensePayload.HardwareId,
        };

        _context.Companies.Add(newCompany);
        _logger.LogInformation("Creating new company information.");
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return newCompany.Id;
    }
}
