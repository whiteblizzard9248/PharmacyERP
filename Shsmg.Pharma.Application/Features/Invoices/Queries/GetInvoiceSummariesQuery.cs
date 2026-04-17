using MediatR;
using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;

namespace Shsmg.Pharma.Application.Features.Invoices.Queries;

public record GetInvoiceSummariesQuery : IRequest<IEnumerable<InvoiceSummaryDto>>;

public sealed class GetInvoiceSummariesQueryHandler(IPharmacyDbContext context) : IRequestHandler<GetInvoiceSummariesQuery, IEnumerable<InvoiceSummaryDto>>
{
    private readonly IPharmacyDbContext _context = context;

    public async Task<IEnumerable<InvoiceSummaryDto>> Handle(GetInvoiceSummariesQuery request, CancellationToken cancellationToken)
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
            .ToListAsync(cancellationToken);
    }
}
