using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;
using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Application.Services;

public sealed class PurchaseInvoiceService(
    IPharmacyDbContext context,
    IValidator<CreatePurchaseInvoiceDto> validator,
    ICurrentUserAccessor currentUserAccessor) : IPurchaseInvoiceService
{
    private readonly IPharmacyDbContext _context = context;
    private readonly IValidator<CreatePurchaseInvoiceDto> _validator = validator;
    private readonly ICurrentUserAccessor _currentUserAccessor = currentUserAccessor;

    public async Task<IEnumerable<PurchaseInvoiceSummaryDto>> GetPurchaseInvoicesAsync(string? search = null)
    {
        var query = _context.PurchaseInvoices
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            var pattern = $"%{normalized.Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(x =>
                EF.Functions.Like(x.PurchaseInvoiceNumber, pattern) ||
                EF.Functions.Like(x.SupplierInvoiceNumber, pattern) ||
                EF.Functions.Like(x.Supplier!.Name, pattern));
        }

        return await query
            .OrderByDescending(x => x.PurchaseDate)
            .Select(x => new PurchaseInvoiceSummaryDto
            {
                Id = x.Id,
                SupplierId = x.SupplierId,
                PurchaseInvoiceNumber = x.PurchaseInvoiceNumber,
                SupplierInvoiceNumber = x.SupplierInvoiceNumber,
                SupplierName = x.Supplier != null ? x.Supplier.Name : string.Empty,
                Date = x.PurchaseDate,
                NetTotal = x.NetTotal
            })
            .ToListAsync();
    }

    public async Task<PurchaseInvoiceDetailDto?> GetPurchaseInvoiceByIdAsync(Guid id)
    {
        return await _context.PurchaseInvoices
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Items.Where(i => !i.IsDeleted))
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new PurchaseInvoiceDetailDto
            {
                Id = x.Id,
                PurchaseInvoiceNumber = x.PurchaseInvoiceNumber,
                SupplierInvoiceNumber = x.SupplierInvoiceNumber,
                SupplierId = x.SupplierId,
                SupplierName = x.Supplier != null ? x.Supplier.Name : string.Empty,
                Date = x.PurchaseDate,
                Notes = x.Notes,
                RowVersion = x.RowVersion,
                Items = x.Items
                    .Where(i => !i.IsDeleted)
                    .Select(i => new PurchaseInvoiceItemDto
                    {
                        Id = i.Id,
                        InventoryItemId = i.InventoryItemId,
                        Description = i.Description,
                        HsnCode = i.HsnCode,
                        Package = i.Package,
                        Mfg = i.Mfg,
                        Batch = i.Batch,
                        ExpiryDate = i.ExpiryDate,
                        Quantity = i.Quantity,
                        Rate = i.Rate,
                        GstPercentage = i.GstPercentage
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreatePurchaseInvoiceAsync(CreatePurchaseInvoiceDto dto)
    {
        await _validator.ValidateAndThrowAsync(dto);
        await EnsureSupplierExists(dto.SupplierId);

        var now = DateTime.UtcNow;
        var actor = _currentUserAccessor.GetCurrentUserIdentifier();
        var invoice = new PurchaseInvoice
        {
            Id = Guid.NewGuid(),
            PurchaseInvoiceNumber = string.IsNullOrWhiteSpace(dto.PurchaseInvoiceNumber)
                ? GeneratePurchaseInvoiceNumber(dto.Date)
                : dto.PurchaseInvoiceNumber.Trim(),
            SupplierInvoiceNumber = dto.SupplierInvoiceNumber.Trim(),
            SupplierId = dto.SupplierId,
            PurchaseDate = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc),
            Notes = dto.Notes.Trim(),
            GrossTotal = dto.GrossTotal,
            TaxTotal = dto.TotalGst,
            NetTotal = dto.NetTotal,
            CreatedAt = now,
            CreatedBy = actor,
            LastModified = now,
            LastModifiedBy = actor,
            RowVersion = dto.RowVersion
        };

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == dto.SupplierId)
            ?? throw new ValidationException("Supplier not found.");
        supplier.RecordPurchase(invoice.NetTotal);

        foreach (var item in dto.Items)
        {
            var inventoryId = await ApplyStockIncreaseAsync(item, null, now, actor);
            invoice.Items.Add(new PurchaseInvoiceItem
            {
                Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id,
                InventoryItemId = inventoryId,
                Description = item.Description.Trim(),
                HsnCode = item.HsnCode.Trim(),
                Package = item.Package.Trim(),
                Mfg = item.Mfg.Trim(),
                Batch = item.Batch.Trim(),
                ExpiryDate = item.ExpiryDate.Trim(),
                Quantity = item.Quantity,
                Rate = item.Rate,
                GstPercentage = item.GstPercentage,
                CreatedAt = now,
                CreatedBy = actor,
                LastModified = now,
                LastModifiedBy = actor
            });
        }

        _context.PurchaseInvoices.Add(invoice);
        await _context.SaveChangesAsync();
        return invoice.Id;
    }

    public async Task<Guid> UpdatePurchaseInvoiceAsync(UpdatePurchaseInvoiceDto dto)
    {
        await _validator.ValidateAndThrowAsync(new CreatePurchaseInvoiceDto
        {
            PurchaseInvoiceNumber = dto.PurchaseInvoiceNumber,
            SupplierInvoiceNumber = dto.SupplierInvoiceNumber,
            SupplierId = dto.SupplierId,
            Date = dto.Date,
            Notes = dto.Notes,
            Items = dto.Items,
            RowVersion = dto.RowVersion
        });
        await EnsureSupplierExists(dto.SupplierId);

        var invoice = await _context.PurchaseInvoices
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == dto.Id)
            ?? throw new Exception("Purchase invoice not found.");

        var existingSupplier = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == invoice.SupplierId)
            ?? throw new Exception("Existing supplier not found.");
        existingSupplier.OutstandingAmount = Math.Max(0, existingSupplier.OutstandingAmount - invoice.NetTotal);

        var newSupplier = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == dto.SupplierId)
            ?? throw new Exception("Supplier not found.");

        _context.Entry(invoice).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;

        var now = DateTime.UtcNow;
        var actor = _currentUserAccessor.GetCurrentUserIdentifier();

        foreach (var existing in invoice.Items.Where(x => !x.IsDeleted).ToList())
        {
            await ApplyStockDecreaseAsync(existing, now, actor);
        }

        foreach (var existing in invoice.Items)
        {
            existing.IsDeleted = true;
            existing.LastModified = now;
            existing.LastModifiedBy = actor;
        }

        invoice.SupplierId = dto.SupplierId;
        invoice.PurchaseInvoiceNumber = string.IsNullOrWhiteSpace(dto.PurchaseInvoiceNumber)
            ? invoice.PurchaseInvoiceNumber
            : dto.PurchaseInvoiceNumber.Trim();
        invoice.SupplierInvoiceNumber = dto.SupplierInvoiceNumber.Trim();
        invoice.PurchaseDate = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
        invoice.Notes = dto.Notes.Trim();
        invoice.GrossTotal = dto.GrossTotal;
        invoice.TaxTotal = dto.TotalGst;
        invoice.NetTotal = dto.NetTotal;
        invoice.LastModified = now;
        invoice.LastModifiedBy = actor;

        newSupplier.RecordPurchase(invoice.NetTotal);

        foreach (var item in dto.Items)
        {
            var existing = invoice.Items.FirstOrDefault(x => x.Id == item.Id);
            var fallbackInventoryId = existing?.InventoryItemId;
            var inventoryId = await ApplyStockIncreaseAsync(item, fallbackInventoryId, now, actor);

            if (existing is not null)
            {
                existing.InventoryItemId = inventoryId;
                existing.Description = item.Description.Trim();
                existing.HsnCode = item.HsnCode.Trim();
                existing.Package = item.Package.Trim();
                existing.Mfg = item.Mfg.Trim();
                existing.Batch = item.Batch.Trim();
                existing.ExpiryDate = item.ExpiryDate.Trim();
                existing.Quantity = item.Quantity;
                existing.Rate = item.Rate;
                existing.GstPercentage = item.GstPercentage;
                existing.IsDeleted = false;
                existing.LastModified = now;
                existing.LastModifiedBy = actor;
            }
            else
            {
                invoice.Items.Add(new PurchaseInvoiceItem
                {
                    Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id,
                    InventoryItemId = inventoryId,
                    Description = item.Description.Trim(),
                    HsnCode = item.HsnCode.Trim(),
                    Package = item.Package.Trim(),
                    Mfg = item.Mfg.Trim(),
                    Batch = item.Batch.Trim(),
                    ExpiryDate = item.ExpiryDate.Trim(),
                    Quantity = item.Quantity,
                    Rate = item.Rate,
                    GstPercentage = item.GstPercentage,
                    CreatedAt = now,
                    CreatedBy = actor,
                    LastModified = now,
                    LastModifiedBy = actor
                });
            }
        }

        await _context.SaveChangesAsync();
        return invoice.Id;
    }

    public async Task DeletePurchaseInvoiceAsync(Guid id)
    {
        var invoice = await _context.PurchaseInvoices
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new Exception("Purchase invoice not found.");

        if (invoice.IsDeleted)
        {
            return;
        }

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(x => x.Id == invoice.SupplierId)
            ?? throw new Exception("Supplier not found.");
        supplier.OutstandingAmount = Math.Max(0, supplier.OutstandingAmount - invoice.NetTotal);

        var now = DateTime.UtcNow;
        var actor = _currentUserAccessor.GetCurrentUserIdentifier();

        foreach (var item in invoice.Items.Where(x => !x.IsDeleted))
        {
            await ApplyStockDecreaseAsync(item, now, actor);
            item.IsDeleted = true;
            item.LastModified = now;
            item.LastModifiedBy = actor;
        }

        invoice.IsDeleted = true;
        invoice.LastModified = now;
        invoice.LastModifiedBy = actor;

        await _context.SaveChangesAsync();
    }

    private async Task EnsureSupplierExists(Guid supplierId)
    {
        var exists = await _context.Suppliers.AnyAsync(x => x.Id == supplierId && !x.IsDeleted);
        if (!exists)
        {
            throw new Exception("Supplier not found.");
        }
    }

    private async Task<Guid> ApplyStockIncreaseAsync(PurchaseInvoiceItemDto item, Guid? fallbackInventoryId, DateTime now, string actor)
    {
        var targetInventoryId = item.InventoryItemId ?? fallbackInventoryId;
        InventoryItem inventory;

        if (targetInventoryId.HasValue)
        {
            inventory = await _context.InventoryItems.FirstOrDefaultAsync(x => x.Id == targetInventoryId.Value)
                ?? throw new Exception("Linked inventory item not found.");
        }
        else
        {
            inventory = new InventoryItem
            {
                Id = Guid.NewGuid(),
                CreatedAt = now,
                CreatedBy = actor
            };
            _context.InventoryItems.Add(inventory);
        }

        inventory.Description = item.Description.Trim();
        inventory.HsnCode = item.HsnCode.Trim();
        inventory.Package = item.Package.Trim();
        inventory.Mfg = item.Mfg.Trim();
        inventory.Batch = item.Batch.Trim();
        inventory.ExpiryDate = item.ExpiryDate.Trim();
        inventory.QuantityInStock += item.Quantity;
        inventory.Rate = item.Rate;
        inventory.GstPercentage = item.GstPercentage;
        inventory.IsDeleted = false;
        inventory.LastModified = now;
        inventory.LastModifiedBy = actor;

        return inventory.Id;
    }

    private async Task ApplyStockDecreaseAsync(PurchaseInvoiceItem item, DateTime now, string actor)
    {
        if (!item.InventoryItemId.HasValue)
        {
            return;
        }

        var inventory = await _context.InventoryItems.FirstOrDefaultAsync(x => x.Id == item.InventoryItemId.Value);
        if (inventory is null)
        {
            return;
        }

        inventory.QuantityInStock = Math.Max(0, inventory.QuantityInStock - item.Quantity);
        inventory.LastModified = now;
        inventory.LastModifiedBy = actor;
    }

    private static string GeneratePurchaseInvoiceNumber(DateTime date)
    {
        return $"PINV-{date:yyyyMMdd}-{GenerateCode(5)}";
    }

    private static string GenerateCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string([.. Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)])]);
    }
}
