using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Domain.Models;
using Shsmg.Pharma.Infra.Auth;
using Shsmg.Pharma.Application.Common;

namespace Shsmg.Pharma.Infra.Persistence;

public class PharmacyDbContext(DbContextOptions<PharmacyDbContext> options, RowVersionInterceptor rowVersionInterceptor) : IdentityDbContext<AppUser>(options), IPharmacyDbContext
{
    public DbSet<Company> Companies { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceAuditLog> InvoiceAuditLogs { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
    public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
    public DbSet<Customer> Customers { get; set; }
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

        builder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ContactPerson).HasMaxLength(150);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.GstNumber).HasMaxLength(30);
            entity.Property(e => e.Address).HasColumnType("text");

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.PhoneNumber);
            entity.HasIndex(e => e.GstNumber);

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

        builder.Entity<PurchaseInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PurchaseInvoiceNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SupplierInvoiceNumber).HasMaxLength(50);
            entity.Property(e => e.Notes).HasColumnType("text");
            entity.Property(e => e.GrossTotal).HasPrecision(12, 2);
            entity.Property(e => e.TaxTotal).HasPrecision(12, 2);
            entity.Property(e => e.NetTotal).HasPrecision(12, 2);
            entity.Property(e => e.RowVersion).IsConcurrencyToken().ValueGeneratedNever();

            entity.HasIndex(e => e.PurchaseInvoiceNumber).IsUnique();
            entity.HasIndex(e => e.PurchaseDate);
            entity.HasIndex(e => e.SupplierId);

            entity.HasOne(e => e.Supplier)
                .WithMany(s => s.PurchaseInvoices)
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Items)
                .WithOne()
                .HasForeignKey(e => e.PurchaseInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => !e.IsDeleted);
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

        builder.Entity<PurchaseInvoiceItem>(entity =>
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

            entity.HasIndex(e => e.PurchaseInvoiceId);
            entity.HasIndex(e => e.InventoryItemId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // 5. Customer Configuration - DDD Aggregate Root
        builder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Core properties
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Type).HasDefaultValue(CustomerType.WalkIn);

            // Financial tracking
            entity.Property(e => e.CreditLimit).HasPrecision(12, 2).HasDefaultValue(0m);
            entity.Property(e => e.OutstandingAmount).HasPrecision(12, 2).HasDefaultValue(0m);
            entity.Property(e => e.LifetimeValue).HasPrecision(12, 2).HasDefaultValue(0m);

            // Blacklist
            entity.Property(e => e.IsBlacklisted).HasDefaultValue(false);
            entity.Property(e => e.BlacklistReason).HasMaxLength(500);

            // Owned value objects: Address (BillingAddress)
            entity.OwnsOne(e => e.BillingAddress, addr =>
            {
                addr.Property(a => a.Street).HasColumnName("BillingStreet").IsRequired().HasMaxLength(250);
                addr.Property(a => a.Street2).HasColumnName("BillingStreet2").HasMaxLength(250);
                addr.Property(a => a.City).HasColumnName("BillingCity").IsRequired().HasMaxLength(100);
                addr.Property(a => a.State).HasColumnName("BillingState").IsRequired().HasMaxLength(100);
                addr.Property(a => a.PostalCode).HasColumnName("BillingPostalCode").IsRequired().HasMaxLength(10);
                addr.Property(a => a.Country).HasColumnName("BillingCountry").IsRequired().HasMaxLength(100);
                addr.Property(a => a.Latitude).HasColumnName("BillingLatitude");
                addr.Property(a => a.Longitude).HasColumnName("BillingLongitude");
            });

            // Owned value objects: Address (ShippingAddress)
            entity.OwnsOne(e => e.ShippingAddress, addr =>
            {
                addr.Property(a => a.Street).HasColumnName("ShippingStreet").HasMaxLength(250);
                addr.Property(a => a.Street2).HasColumnName("ShippingStreet2").HasMaxLength(250);
                addr.Property(a => a.City).HasColumnName("ShippingCity").HasMaxLength(100);
                addr.Property(a => a.State).HasColumnName("ShippingState").HasMaxLength(100);
                addr.Property(a => a.PostalCode).HasColumnName("ShippingPostalCode").HasMaxLength(10);
                addr.Property(a => a.Country).HasColumnName("ShippingCountry").HasMaxLength(100);
                addr.Property(a => a.Latitude).HasColumnName("ShippingLatitude");
                addr.Property(a => a.Longitude).HasColumnName("ShippingLongitude");
            });

            // Owned value objects: PatientInfo
            entity.OwnsOne(e => e.PatientInfo, pi =>
            {
                pi.Property(p => p.Age).HasColumnName("PatientAge");
                pi.Property(p => p.Gender).HasColumnName("PatientGender").HasMaxLength(1);
                pi.Property(p => p.GSTIN).HasColumnName("PatientGSTIN").HasMaxLength(15);
                pi.Property(p => p.AadharNumber).HasColumnName("PatientAadharNumber").HasMaxLength(12);
                pi.Property(p => p.PanNumber).HasColumnName("PatientPANNumber").HasMaxLength(10);
                pi.Property(p => p.DoctorName).HasColumnName("PatientDoctorName").HasMaxLength(200);
                pi.Property(p => p.MedicalNotes).HasColumnName("PatientMedicalNotes").HasColumnType("text");
                pi.Property(p => p.LastUpdatedAt).HasColumnName("PatientLastUpdatedAt");
                pi.Property(p => p.Exists).HasColumnName("PatientExists").HasDefaultValue(true).IsRequired();
            });

            // Indexes for performance
            entity.HasIndex(e => e.PhoneNumber);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.IsBlacklisted);
            entity.HasIndex(e => e.OutstandingAmount);
            entity.HasIndex(e => e.LastPurchaseDate);

            // Composite index for "customer found" workflow
            entity.HasIndex(e => new { e.PhoneNumber, e.Type });

            // Soft Delete Filter
            entity.HasQueryFilter(e => !e.IsDeleted);

            // Relationship to invoices
            entity.HasMany(c => c.Invoices)
                  .WithOne(i => i.Customer)
                  .HasForeignKey(i => i.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 6. InventoryItem Configuration
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
