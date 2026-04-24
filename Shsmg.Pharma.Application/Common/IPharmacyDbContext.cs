using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Shsmg.Pharma.Domain.Models;
namespace Shsmg.Pharma.Application.Common;

public interface IPharmacyDbContext
{
    DbSet<Company> Companies { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceAuditLog> InvoiceAuditLogs { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<PurchaseInvoice> PurchaseInvoices { get; }
    DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; }
    DbSet<Customer> Customers { get; }

    ChangeTracker ChangeTracker { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
