using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;
using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Application.Services;

public sealed class InvoiceService(IPharmacyDbContext context, IValidator<CreateInvoiceDto> createValidator) : IInvoiceService
{
    private readonly IPharmacyDbContext _context = context;
    private readonly IValidator<CreateInvoiceDto> _createValidator = createValidator;

    public async Task<IEnumerable<InvoiceSummaryDto>> GetInvoiceSummariesAsync()
    {
        return await _context.Invoices
            .AsNoTracking()
            .Where(i => !i.IsDeleted)
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new InvoiceSummaryDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                Date = i.InvoiceDate,
                PatientName = i.PatientName,
                NetTotal = i.NetTotal,
                IsDeleted = i.IsDeleted
            })
            .ToListAsync();
    }

    public async Task<InvoiceDetailDto?> GetInvoiceByIdAsync(Guid id)
    {
        return await _context.Invoices
            .Where(i => i.Id == id && !i.IsDeleted)
            .Select(i => new InvoiceDetailDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                PatientName = i.PatientName,
                DoctorName = i.DoctorName,
                Date = i.InvoiceDate,
                RowVersion = i.RowVersion,
                Items = i.Items
                    .Where(item => !item.IsDeleted)
                    .Select(item => new InvoiceItemDto
                    {
                        Id = item.Id,
                        InventoryItemId = item.InventoryItemId,
                        Description = item.Description ?? "",
                        HsnCode = item.HsnCode ?? "",
                        Package = item.Package ?? PackageType.Unit.ToString(),
                        Mfg = item.Mfg ?? string.Empty,
                        Batch = item.Batch ?? string.Empty,
                        ExpiryDate = item.ExpiryDate ?? string.Empty,
                        Quantity = item.Quantity,
                        Rate = item.Rate,
                        GstPercentage = item.GstPercentage
                    })
                    .ToList()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreateInvoiceAsync(CreateInvoiceDto dto)
    {
        await _createValidator.ValidateAndThrowAsync(dto);

        var invoice = new Invoice
        {
            InvoiceNumber = string.IsNullOrWhiteSpace(dto.InvoiceNumber)
                ? GenerateInvoiceNumber(dto.Date)
                : dto.InvoiceNumber,
            PatientName = dto.PatientName,
            DoctorName = dto.DoctorName,
            InvoiceDate = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc),
            GrossTotal = dto.GrossTotal,
            TaxTotal = dto.TotalGst,
            NetTotal = dto.NetTotal,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            RowVersion = dto.RowVersion,
            Items = [.. dto.Items.Select(item => new InvoiceItem
            {
                Id = Guid.NewGuid(),
                InventoryItemId = item.InventoryItemId,
                Description = item.Description,
                HsnCode = item.HsnCode,
                Package = item.Package,
                Mfg = item.Mfg,
                Batch = item.Batch,
                ExpiryDate = item.ExpiryDate,
                Quantity = item.Quantity,
                Rate = item.Rate,
                GstPercentage = item.GstPercentage,
                LastModified = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            })]
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return invoice.Id;
    }

    public async Task<Guid> UpdateInvoiceAsync(UpdateInvoiceDto dto)
    {
        await _createValidator.ValidateAndThrowAsync(new CreateInvoiceDto
        {
            InvoiceNumber = dto.InvoiceNumber,
            PatientName = dto.PatientName,
            DoctorName = dto.DoctorName,
            Date = dto.Date,
            Items = dto.Items,
            RowVersion = dto.RowVersion
        });

        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == dto.Id)
            ?? throw new Exception("Invoice not found");

        _context.Entry(invoice)
            .Property(x => x.RowVersion)
            .OriginalValue = dto.RowVersion;

        foreach (var item in dto.Items)
        {
            if (item.Id == Guid.Empty)
                item.Id = Guid.NewGuid();
        }

        invoice.PatientName = dto.PatientName;
        invoice.DoctorName = dto.DoctorName;
        invoice.InvoiceDate = dto.Date;
        SyncItems(invoice, dto);
        invoice.LastModified = DateTime.UtcNow;
        _context.Entry(invoice).Property(x => x.LastModified).IsModified = true;

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        return invoice.Id;
    }

    private void SyncItems(Invoice invoice, UpdateInvoiceDto dto)
    {
        var now = DateTime.UtcNow;
        var incomingMap = dto.Items.ToDictionary(i => i.Id);

        // 1. Handle Deletions: Mark missing items as deleted
        foreach (var existing in invoice.Items.ToList())
        {
            if (!incomingMap.ContainsKey(existing.Id))
            {
                existing.IsDeleted = true;
                existing.LastModified = now;
                // No need to manually set IsModified here; EF detects the change
            }
        }

        // 2. Handle Updates and Additions
        foreach (var dtoItem in dto.Items)
        {
            var existing = invoice.Items.FirstOrDefault(i => i.Id == dtoItem.Id);

            if (existing != null)
            {
                // Just update the values. EF detects the changes automatically.
                existing.Description = dtoItem.Description;
                existing.InventoryItemId = dtoItem.InventoryItemId;
                existing.HsnCode = dtoItem.HsnCode;
                existing.Package = dtoItem.Package;
                existing.Batch = dtoItem.Batch;
                existing.ExpiryDate = dtoItem.ExpiryDate;
                existing.Quantity = dtoItem.Quantity;
                existing.Rate = dtoItem.Rate;
                existing.GstPercentage = dtoItem.GstPercentage;
                existing.LastModified = now;
                existing.IsDeleted = false;
            }
            else
            {
                // ADDITION: Explicitly set the Shadow Property to ensure the link is preserved
                var newItem = new InvoiceItem
                {
                    Id = dtoItem.Id,
                    InventoryItemId = dtoItem.InventoryItemId,
                    Description = dtoItem.Description,
                    HsnCode = dtoItem.HsnCode,
                    Package = dtoItem.Package,
                    Batch = dtoItem.Batch,
                    ExpiryDate = dtoItem.ExpiryDate,
                    Quantity = dtoItem.Quantity,
                    Rate = dtoItem.Rate,
                    GstPercentage = dtoItem.GstPercentage,
                    CreatedAt = now,
                    LastModified = now,
                    IsDeleted = false
                };

                // Manually set the shadow property to guarantee persistence
                _context.Entry(newItem).Property(e => e.InvoiceId).CurrentValue = invoice.Id;
                invoice.Items.Add(newItem);
                _context.Entry(newItem).State = EntityState.Added;
            }
        }
    }

    private static string GenerateInvoiceNumber(DateTime date)
    {
        return $"INV-{date:yyyyMMdd}-{GenerateCode(6)}";
    }

    private static string GenerateCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string([.. Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)])]);
    }
}
