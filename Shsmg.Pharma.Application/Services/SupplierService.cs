using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;
using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Application.Services;

public sealed class SupplierService(IPharmacyDbContext context, IValidator<SupplierDto> validator) : ISupplierService
{
    private readonly IPharmacyDbContext _context = context;
    private readonly IValidator<SupplierDto> _validator = validator;

    public async Task<IEnumerable<SupplierDto>> GetSuppliersAsync(string? search = null)
    {
        var query = _context.Suppliers
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            var pattern = $"%{normalized.Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(x =>
                EF.Functions.Like(x.Name, pattern) ||
                EF.Functions.Like(x.PhoneNumber, pattern) ||
                EF.Functions.Like(x.GstNumber, pattern));
        }

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new SupplierDto
            {
                Id = x.Id,
                Name = x.Name,
                ContactPerson = x.ContactPerson,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                Address = x.Address,
                GstNumber = x.GstNumber
            })
            .ToListAsync();
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(Guid id)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new SupplierDto
            {
                Id = x.Id,
                Name = x.Name,
                ContactPerson = x.ContactPerson,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                Address = x.Address,
                GstNumber = x.GstNumber
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> SaveSupplierAsync(SupplierDto dto)
    {
        await _validator.ValidateAndThrowAsync(dto);

        var now = DateTime.UtcNow;
        var isNew = dto.Id == Guid.Empty;

        Supplier entity;
        if (isNew)
        {
            entity = new Supplier
            {
                Id = Guid.NewGuid(),
                CreatedAt = now
            };
            _context.Suppliers.Add(entity);
        }
        else
        {
            entity = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == dto.Id)
                ?? throw new Exception("Supplier not found.");
        }

        entity.Name = dto.Name.Trim();
        entity.ContactPerson = dto.ContactPerson.Trim();
        entity.PhoneNumber = dto.PhoneNumber.Trim();
        entity.Email = dto.Email.Trim();
        entity.Address = dto.Address.Trim();
        entity.GstNumber = dto.GstNumber.Trim();
        entity.LastModified = now;
        entity.IsDeleted = false;

        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task DeleteSupplierAsync(Guid id)
    {
        var entity = await _context.Suppliers
            .Include(x => x.PurchaseInvoices)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new Exception("Supplier not found.");

        if (entity.PurchaseInvoices.Any(x => !x.IsDeleted))
        {
            throw new Exception("Supplier cannot be deleted because purchase invoices exist for this supplier.");
        }

        entity.IsDeleted = true;
        entity.LastModified = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
