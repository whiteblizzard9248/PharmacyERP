using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Domain.Models;
using Shsmg.Pharma.Infra.Auth;
using Shsmg.Pharma.Application.Common;

namespace Shsmg.Pharma.Infra.Persistence;

public class PharmacyDbContext(DbContextOptions<PharmacyDbContext> options, RowVersionInterceptor rowVersionInterceptor) : IdentityDbContext<AppUser>(options), IPharmacyDbContext
{
    public DbSet<Company> Companies { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceAuditLog> InvoiceAuditLogs { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(rowVersionInterceptor);
    }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);


        builder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.LicenseNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ContactNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Address).HasColumnType("text");

            entity.Property(e => e.LicenseKey).HasMaxLength(500);
            entity.Property(e => e.HardwareId).HasMaxLength(200);

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.ContactNumber);
            entity.HasIndex(e => e.LicenseNumber).IsUnique();
            entity.HasIndex(e => e.HardwareId);

            // Ensure soft delete is always active
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // 3. Invoice Configuration
        builder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Postgres Specific: Map Decimals to ensure accuracy for Rupees
            entity.Property(e => e.GrossTotal).HasPrecision(12, 2);
            entity.Property(e => e.TaxTotal).HasPrecision(12, 2);
            entity.Property(e => e.NetTotal).HasPrecision(12, 2);

            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PatientName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.DoctorName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.RowVersion).IsConcurrencyToken().ValueGeneratedNever();

            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.InvoiceDate);
            entity.HasIndex(e => e.PatientName);

            // Soft Delete Filter
            entity.HasQueryFilter(e => !e.IsDeleted);

            // Relationship
            entity.HasMany(e => e.Items)
                  .WithOne()
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InvoiceAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Summary).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.SnapshotJson).HasColumnType("text");
            entity.Property(e => e.PerformedBy).IsRequired().HasMaxLength(256);

            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.PerformedAt);
        });

        // 4. Invoice Items Configuration
        builder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.HsnCode).HasMaxLength(10);
            entity.Property(e => e.Package).HasMaxLength(50);
            entity.Property(e => e.Mfg).HasMaxLength(100);
            entity.Property(e => e.Batch).HasMaxLength(50);
            entity.Property(e => e.ExpiryDate).HasMaxLength(20); // String as requested

            entity.HasIndex("InvoiceId");
            entity.HasIndex(e => e.HsnCode);
            entity.HasIndex(e => e.Batch);

            // Financials
            entity.Property(e => e.Rate).HasPrecision(12, 2);
            entity.Property(e => e.GstPercentage).HasPrecision(5, 2);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.HsnCode).HasMaxLength(10);
            entity.Property(e => e.Package).HasMaxLength(50);
            entity.Property(e => e.Mfg).HasMaxLength(100);
            entity.Property(e => e.Batch).HasMaxLength(50);
            entity.Property(e => e.ExpiryDate).HasMaxLength(20);
            entity.Property(e => e.Rate).HasPrecision(12, 2);
            entity.Property(e => e.GstPercentage).HasPrecision(5, 2);

            entity.HasIndex(e => e.Description);
            entity.HasIndex(e => e.Batch);
            entity.HasIndex(e => e.HsnCode);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
