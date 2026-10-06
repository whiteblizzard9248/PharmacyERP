using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;
using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Application.Services;

public sealed class CompanyService(IPharmacyDbContext context,
 ILogger<CompanyService> logger,
  ILicenseService licenseService) : ICompanyService
{
    public async Task<CompanyDto?> GetCompanyAsync()
    {
        logger.LogInformation("Attempting to retrieve company information.");
        var company = await context.Companies
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
        logger.LogInformation("Attempting to create or update company information.");
        var existingCompany = dto.Id != Guid.Empty
            ? await context.Companies.FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted)
            : await context.Companies.FirstOrDefaultAsync(c => !c.IsDeleted);
        
        var currentValidationResult = licenseService.Validate(dto.LicenseKey!, dto.HardwareId!);
        var currentValidationResultStr = JsonSerializer.Serialize(currentValidationResult, JsonDefaults.StandardOptions);
        logger.LogInformation($"""License info {currentValidationResultStr}""");

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

            await context.SaveChangesAsync();
            logger.LogInformation("Company information updated successfully.");
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

        context.Companies.Add(newCompany);
        logger.LogInformation("Creating new company information.");
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return newCompany.Id;
    }
}
