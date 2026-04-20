using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using FluentValidation;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;
using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Application.Services;

public sealed class InvoiceService(
    IPharmacyDbContext context,
    IValidator<CreateInvoiceDto> createValidator,
    ICurrentUserAccessor currentUserAccessor) : IInvoiceService
{
    private readonly IPharmacyDbContext _context = context;
    private readonly IValidator<CreateInvoiceDto> _createValidator = createValidator;
    private readonly ICurrentUserAccessor _currentUserAccessor = currentUserAccessor;

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

    public async Task<IReadOnlyList<InvoiceAuditLogDto>> GetInvoiceAuditLogsAsync(Guid invoiceId)
    {
        return await _context.InvoiceAuditLogs
            .AsNoTracking()
            .Where(log => log.InvoiceId == invoiceId)
            .OrderByDescending(log => log.PerformedAt)
            .Select(log => new InvoiceAuditLogDto
            {
                Id = log.Id,
                Action = log.Action,
                Summary = log.Summary,
                PerformedAt = log.PerformedAt,
                PerformedBy = log.PerformedBy
            })
            .ToListAsync();
    }

    public async Task<Guid> CreateInvoiceAsync(CreateInvoiceDto dto)
    {
        await _createValidator.ValidateAndThrowAsync(dto);

        var now = DateTime.UtcNow;
        var actor = _currentUserAccessor.GetCurrentUserIdentifier();

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
            CreatedAt = now,
            CreatedBy = actor,
            LastModified = now,
            LastModifiedBy = actor,
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
                LastModified = now,
                LastModifiedBy = actor,
                CreatedAt = now,
                CreatedBy = actor
            })]
        };

        _context.Invoices.Add(invoice);
        _context.InvoiceAuditLogs.Add(new InvoiceAuditLog
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Action = "Created",
            Summary = $"Created invoice with {invoice.Items.Count} line item(s).",
            SnapshotJson = SerializeAuditSnapshot(invoice),
            PerformedAt = now,
            PerformedBy = actor
        });

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

        var beforeSnapshot = BuildAuditSnapshot(invoice);
        var now = DateTime.UtcNow;
        var actor = _currentUserAccessor.GetCurrentUserIdentifier();

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
        invoice.InvoiceDate = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
        invoice.GrossTotal = dto.GrossTotal;
        invoice.TaxTotal = dto.TotalGst;
        invoice.NetTotal = dto.NetTotal;
        SyncItems(invoice, dto);
        invoice.LastModified = now;
        invoice.LastModifiedBy = actor;
        _context.Entry(invoice).Property(x => x.LastModified).IsModified = true;

        var afterSnapshot = BuildAuditSnapshot(invoice);
        _context.InvoiceAuditLogs.Add(new InvoiceAuditLog
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Action = "Updated",
            Summary = BuildUpdateSummary(beforeSnapshot, afterSnapshot),
            SnapshotJson = JsonSerializer.Serialize(new
            {
                Before = beforeSnapshot,
                After = afterSnapshot
            }),
            PerformedAt = now,
            PerformedBy = actor
        });

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
                existing.LastModifiedBy = _currentUserAccessor.GetCurrentUserIdentifier();
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
                    Mfg = dtoItem.Mfg,
                    Batch = dtoItem.Batch,
                    ExpiryDate = dtoItem.ExpiryDate,
                    Quantity = dtoItem.Quantity,
                    Rate = dtoItem.Rate,
                    GstPercentage = dtoItem.GstPercentage,
                    CreatedAt = now,
                    CreatedBy = _currentUserAccessor.GetCurrentUserIdentifier(),
                    LastModified = now,
                    LastModifiedBy = _currentUserAccessor.GetCurrentUserIdentifier(),
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

    private static string SerializeAuditSnapshot(Invoice invoice)
    {
        return JsonSerializer.Serialize(BuildAuditSnapshot(invoice));
    }

    private static InvoiceAuditSnapshot BuildAuditSnapshot(Invoice invoice)
    {
        return new InvoiceAuditSnapshot
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            PatientName = invoice.PatientName,
            DoctorName = invoice.DoctorName,
            GrossTotal = invoice.Items.Where(i => !i.IsDeleted).Sum(i => i.Quantity * i.Rate / (1 + (i.GstPercentage / 100))),
            TaxTotal = invoice.Items.Where(i => !i.IsDeleted).Sum(i => (i.Quantity * i.Rate) - (i.Quantity * i.Rate / (1 + (i.GstPercentage / 100)))),
            NetTotal = invoice.Items.Where(i => !i.IsDeleted).Sum(i => i.Quantity * i.Rate),
            Items = [.. invoice.Items
                .Where(i => !i.IsDeleted)
                .OrderBy(i => i.CreatedAt)
                .Select(i => new InvoiceAuditItemSnapshot
                {
                    Id = i.Id,
                    InventoryItemId = i.InventoryItemId,
                    Description = i.Description ?? string.Empty,
                    HsnCode = i.HsnCode ?? string.Empty,
                    Package = i.Package ?? string.Empty,
                    Mfg = i.Mfg ?? string.Empty,
                    Batch = i.Batch ?? string.Empty,
                    ExpiryDate = i.ExpiryDate ?? string.Empty,
                    Quantity = i.Quantity,
                    Rate = i.Rate,
                    GstPercentage = i.GstPercentage
                })]
        };
    }

    private static string BuildUpdateSummary(InvoiceAuditSnapshot before, InvoiceAuditSnapshot after)
    {
        var changes = new List<string>();

        if (!string.Equals(before.PatientName, after.PatientName, StringComparison.Ordinal))
            changes.Add("patient");
        if (!string.Equals(before.DoctorName, after.DoctorName, StringComparison.Ordinal))
            changes.Add("doctor");
        if (before.InvoiceDate != after.InvoiceDate)
            changes.Add("date");
        if (!string.Equals(before.InvoiceNumber, after.InvoiceNumber, StringComparison.Ordinal))
            changes.Add("invoice number");

        var beforeItems = before.Items.ToDictionary(i => i.Id);
        var afterItems = after.Items.ToDictionary(i => i.Id);

        var added = afterItems.Keys.Except(beforeItems.Keys).Count();
        var removed = beforeItems.Keys.Except(afterItems.Keys).Count();
        var updated = afterItems.Keys.Intersect(beforeItems.Keys).Count(id => !AreEqual(beforeItems[id], afterItems[id]));

        if (added > 0)
            changes.Add($"{added} item(s) added");
        if (updated > 0)
            changes.Add($"{updated} item(s) updated");
        if (removed > 0)
            changes.Add($"{removed} item(s) removed");

        return changes.Count == 0
            ? "Saved invoice without material data changes."
            : $"Updated {string.Join(", ", changes)}.";
    }

    private static bool AreEqual(InvoiceAuditItemSnapshot left, InvoiceAuditItemSnapshot right)
    {
        return left.InventoryItemId == right.InventoryItemId
            && left.Description == right.Description
            && left.HsnCode == right.HsnCode
            && left.Package == right.Package
            && left.Mfg == right.Mfg
            && left.Batch == right.Batch
            && left.ExpiryDate == right.ExpiryDate
            && left.Quantity == right.Quantity
            && left.Rate == right.Rate
            && left.GstPercentage == right.GstPercentage;
    }

    private static string GenerateCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string([.. Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)])]);
    }

    private sealed class InvoiceAuditSnapshot
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public decimal GrossTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal NetTotal { get; set; }
        public List<InvoiceAuditItemSnapshot> Items { get; set; } = [];
    }

    private sealed class InvoiceAuditItemSnapshot
    {
        public Guid Id { get; set; }
        public Guid? InventoryItemId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string HsnCode { get; set; } = string.Empty;
        public string Package { get; set; } = string.Empty;
        public string Mfg { get; set; } = string.Empty;
        public string Batch { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal GstPercentage { get; set; }
    }
}
