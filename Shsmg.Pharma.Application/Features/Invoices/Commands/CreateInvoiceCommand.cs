using MediatR;
using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;
using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Application.Features.Invoices.Commands;

public record CreateInvoiceCommand(CreateInvoiceDto InvoiceDto) : IRequest<Guid>;

public class CreateInvoiceHandler(IPharmacyDbContext context) : IRequestHandler<CreateInvoiceCommand, Guid>
{
    private readonly IPharmacyDbContext _context = context;

    public async Task<Guid> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var dto = request.InvoiceDto;
        var isEdit = dto.Id != Guid.Empty;
        var invoice = isEdit
            ? await _context.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == dto.Id && !i.IsDeleted, cancellationToken)
            : null;

        if (invoice is null)
        {
            invoice = new Invoice
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
                Items = [.. dto.Items.Select(item => new InvoiceItem
                {
                    Id = item.Id,
                    Description = item.Description,
                    Package = item.Package.ToString(),
                    Mfg = item.Mfg,
                    Batch = item.Batch,
                    ExpiryDate = item.ExpiryDate,
                    Quantity = item.Quantity,
                    Rate = item.Rate,
                    GstPercentage = item.GstPercentage
                })]
            };

            _context.Invoices.Add(invoice);
        }
        else
        {
            // Update main Invoice properties
            _context.Entry(invoice).CurrentValues.SetValues(dto); // Shorthand to copy matching properties
            invoice.InvoiceDate = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);

            // 1. Get the list of IDs currently in the DTO
            var dtoItemIds = dto.Items.Select(i => i.Id).Where(id => id != Guid.Empty).ToList();

            // 2. Identify items to Remove (Soft Delete)
            // IMPORTANT: Only remove items that aren't already marked IsDeleted
            var itemsToRemove = invoice.Items
                .Where(i => !dtoItemIds.Contains(i.Id) && !i.IsDeleted)
                .ToList();

            foreach (var item in itemsToRemove)
            {
                _context.InvoiceItems.Remove(item);
            }

            // 3. Update or Add
            foreach (var dtoItem in dto.Items)
            {
                // Find existing item, ensuring we don't try to update a "Deleted" one
                var existingItem = invoice.Items
                    .FirstOrDefault(i => i.Id == dtoItem.Id && i.Id != Guid.Empty && !i.IsDeleted);

                if (existingItem != null)
                {
                    // UPDATE: EF handles the tracking, just update values
                    existingItem.Description = dtoItem.Description;
                    existingItem.Quantity = dtoItem.Quantity;
                    existingItem.Rate = dtoItem.Rate;
                    existingItem.GstPercentage = dtoItem.GstPercentage;
                    existingItem.Batch = dtoItem.Batch;
                    existingItem.ExpiryDate = dtoItem.ExpiryDate;
                    existingItem.Package = dtoItem.Package.ToString();
                }
                else if (dtoItem.Id == Guid.Empty || !invoice.Items.Any(i => i.Id == dtoItem.Id))
                {
                    // ADD: New item
                    invoice.Items.Add(new InvoiceItem
                    {
                        Description = dtoItem.Description,
                        Quantity = dtoItem.Quantity,
                        Rate = dtoItem.Rate,
                        GstPercentage = dtoItem.GstPercentage,
                        Batch = dtoItem.Batch,
                        ExpiryDate = dtoItem.ExpiryDate,
                        Package = dtoItem.Package.ToString()
                    });
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return invoice.Id;
    }

    private static string GenerateInvoiceNumber(DateTime date)
    {
        return $"INV-{date:yyyyMMdd}-{Generate(6)}";
    }

    private static string Generate(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();

        return new string([.. Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)])]);
    }
}
