using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shsmg.Pharma.Domain.Models;

namespace Shsmg.Pharma.Infra.Persistence;

public sealed class RowVersionInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateRowVersions(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateRowVersions(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateRowVersions(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker
            .Entries<IHasRowVersion>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.RowVersion = Generate();
                continue;
            }

            if (entry.State != EntityState.Modified)
                continue;

            var hasRealChanges = entry.Properties.Any(p =>
                p.Metadata.Name != nameof(IHasRowVersion.RowVersion) &&
                p.Metadata.Name != "LastModified" &&
                !Equals(p.OriginalValue, p.CurrentValue));

            if (hasRealChanges)
            {
                entry.Entity.RowVersion = Generate();
            }
        }
    }

    private static byte[] Generate()
        => Guid.NewGuid().ToByteArray();
}