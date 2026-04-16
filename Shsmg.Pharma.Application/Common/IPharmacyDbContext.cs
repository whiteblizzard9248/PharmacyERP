using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Domain.Models;
namespace Shsmg.Pharma.Application.Common;

public interface IPharmacyDbContext
{
    DbSet<Company> Companies { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}