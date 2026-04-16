using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shsmg.Pharma.Domain.Models;
using Shsmg.Pharma.Infra.Auth;
using Shsmg.Pharma.Application.Common;

namespace Shsmg.Pharma.Infra.Persistence;

public class PharmacyDbContext(DbContextOptions<PharmacyDbContext> options) : IdentityDbContext<AppUser>(options), IPharmacyDbContext
{
    public DbSet<Company> Companies { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }

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

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.ContactNumber);
            entity.HasIndex(e => e.LicenseNumber).IsUnique();

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

            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.InvoiceDate);
            entity.HasIndex(e => e.PatientName);

            // Soft Delete Filter
            entity.HasQueryFilter(e => !e.IsDeleted);

            // Relationship
            entity.HasMany(e => e.Items)
                  .WithOne()
                  .HasForeignKey("InvoiceId") // Shadow property
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 4. Invoice Items Configuration
        builder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Package).HasMaxLength(50);
            entity.Property(e => e.Mfg).HasMaxLength(100);
            entity.Property(e => e.Batch).HasMaxLength(50);
            entity.Property(e => e.ExpiryDate).HasMaxLength(20); // String as requested

            entity.HasIndex("InvoiceId");
            entity.HasIndex(e => e.Batch);

            // Financials
            entity.Property(e => e.Rate).HasPrecision(12, 2);
            entity.Property(e => e.GstPercentage).HasPrecision(5, 2);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModified = DateTime.UtcNow;
                    break;
                case EntityState.Deleted:
                    // Intercept hard delete and turn it into a soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    break;
            }
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}
