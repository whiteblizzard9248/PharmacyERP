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
            invoice.PatientName = dto.PatientName;
            invoice.DoctorName = dto.DoctorName;
            invoice.InvoiceDate = dto.Date;
            invoice.GrossTotal = dto.GrossTotal;
            invoice.TaxTotal = dto.TotalGst;
            invoice.NetTotal = dto.NetTotal;

            var existingItems = invoice.Items.ToList();
            foreach (var existingItem in existingItems)
            {
                _context.InvoiceItems.Remove(existingItem);
            }

            invoice.Items = [.. dto.Items.Select(item => new InvoiceItem
            {
                Description = item.Description,
                Package = item.Package.ToString(),
                Mfg = item.Mfg,
                Batch = item.Batch,
                ExpiryDate = item.ExpiryDate,
                Quantity = item.Quantity,
                Rate = item.Rate,
                GstPercentage = item.GstPercentage
            })];
        }

        await _context.SaveChangesAsync(cancellationToken);
        return invoice.Id;
    }

    private static string GenerateInvoiceNumber(DateTime date)
    {
        return $"INV-{date:yyyyMMdd}-{Guid.NewGuid():N}".ToUpperInvariant();
    }
}
