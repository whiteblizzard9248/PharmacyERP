using MediatR;
using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Features.Invoices.Queries;

public record GetInvoiceByIdQuery(Guid InvoiceId) : IRequest<CreateInvoiceDto?>;

public sealed class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, CreateInvoiceDto?>
{
    private readonly IPharmacyDbContext _context;

    public GetInvoiceByIdQueryHandler(IPharmacyDbContext context)
    {
        _context = context;
    }

    public async Task<CreateInvoiceDto?> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && !i.IsDeleted, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        return new CreateInvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            PatientName = invoice.PatientName,
            DoctorName = invoice.DoctorName,
            Date = invoice.InvoiceDate,
            Items = invoice.Items.Select(item => new InvoiceItemDto
            {
                Description = item.Description ?? "",
                Package = Enum.TryParse<PackageType>(item.Package, out var result) ? result : PackageType.Unit,
                Mfg = item.Mfg ?? string.Empty,
                Batch = item.Batch ?? string.Empty,
                ExpiryDate = item.ExpiryDate!,
                Quantity = item.Quantity,
                Rate = item.Rate,
                GstPercentage = item.GstPercentage
            }).ToList()
        };
    }
}
