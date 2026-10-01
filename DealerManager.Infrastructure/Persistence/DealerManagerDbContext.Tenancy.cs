using DealerManager.Application.IService.Identity;
using DealerManager.Domain.Common;
using DealerManager.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace DealerManager.Infrastructure.Persistence;
public partial class DealerManagerDbContext
{
    private void ConfigureTenancy(ModelBuilder builder)
    {
        builder.Entity<Dealership>().Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Entity<Dealership>().HasData(new Dealership { Id = -1, Name = "Legacy — unassigned", CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) });
        builder.Entity<IdentityRole<int>>().HasData(new IdentityRole<int> { Id = 1, Name = "Owner", NormalizedName = "OWNER", ConcurrencyStamp = "owner-v1" });
        builder.Entity<ApplicationUser>().HasOne(u => u.Dealership).WithMany(d => d.Users).HasForeignKey(u => u.DealershipId);
        builder.Entity<ApplicationUser>().HasIndex(u => u.NormalizedEmail).IsUnique();

        ConfigureRoot<Candidate>(builder);
        ConfigureRoot<Vehicle>(builder);
        ConfigureRoot<CapitalAccount>(builder);
        ConfigureRoot<FinancialTransaction>(builder);
        builder.Entity<Candidate>().HasQueryFilter(c => c.DealershipId == CurrentTenantId);
        builder.Entity<Vehicle>().HasQueryFilter(v => v.DealershipId == CurrentTenantId);
        builder.Entity<CapitalAccount>().HasQueryFilter(a => a.DealershipId == CurrentTenantId);
        builder.Entity<FinancialTransaction>().HasQueryFilter(t => t.DealershipId == CurrentTenantId);
        builder.Entity<CandidatePhoto>().HasQueryFilter(p => p.Candidate.DealershipId == CurrentTenantId);
        builder.Entity<CandidateEstimate>().HasQueryFilter(e => e.Candidate.DealershipId == CurrentTenantId);
        builder.Entity<CandidateEstimateItem>().HasQueryFilter(i => i.CandidateEstimate.Candidate.DealershipId == CurrentTenantId);
        builder.Entity<VehicleCostPlanItem>().HasQueryFilter(p => p.Vehicle.DealershipId == CurrentTenantId);
        builder.Entity<VehicleExpense>().HasQueryFilter(e => e.Vehicle.DealershipId == CurrentTenantId);
        builder.Entity<VehicleListing>().HasQueryFilter(l => l.Vehicle.DealershipId == CurrentTenantId);
        builder.Entity<VehicleSale>().HasQueryFilter(s => s.Vehicle.DealershipId == CurrentTenantId);
        builder.Entity<VehicleStatusHistory>().HasQueryFilter(h => h.Vehicle.DealershipId == CurrentTenantId);

        // Database constraints reinforce same-tenant links between roots.
        builder.Entity<Vehicle>().HasOne(v => v.SourceCandidate).WithOne(c => c.Vehicle)
            .HasForeignKey<Vehicle>(v => new { v.SourceCandidateId, v.DealershipId })
            .HasPrincipalKey<Candidate>(c => new { c.Id, c.DealershipId });
        builder.Entity<FinancialTransaction>().HasOne(t => t.CapitalAccount).WithMany(a => a.FinancialTransactions)
            .HasForeignKey(t => new { t.CapitalAccountId, t.DealershipId })
            .HasPrincipalKey(a => new { a.Id, a.DealershipId });
        builder.Entity<FinancialTransaction>().HasOne(t => t.Vehicle).WithMany()
            .HasForeignKey(t => new { t.VehicleId, t.DealershipId })
            .HasPrincipalKey(v => new { v.Id, v.DealershipId });
    }
    private static void ConfigureRoot<T>(ModelBuilder builder) where T : Entity, ITenantEntity
    {
        builder.Entity<T>().HasOne<Dealership>().WithMany().HasForeignKey(e => e.DealershipId);
        builder.Entity<T>().HasAlternateKey(e => new { e.Id, e.DealershipId });
        builder.Entity<T>().Property(e => e.DealershipId).IsConcurrencyToken();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateTenantWrites(default).GetAwaiter().GetResult();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await ValidateTenantWrites(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private static bool IsBusiness(Type type) => typeof(Entity).IsAssignableFrom(type) && type != typeof(Dealership);
    private static readonly MethodInfo VisibleMethod = typeof(DealerManagerDbContext).GetMethod(nameof(IsVisibleEntity), BindingFlags.Instance | BindingFlags.NonPublic)!;
    private Task<bool> IsVisibleEntity<T>(int id, CancellationToken token) where T : Entity
        => Set<T>().AsNoTracking().AnyAsync(e => e.Id == id, token);
    private Task<bool> IsVisible(Type type, int id, CancellationToken token)
        => (Task<bool>)VisibleMethod.MakeGenericMethod(type).Invoke(this, [id, token])!;

    private async Task ValidateTenantWrites(CancellationToken token)
    {
        var entries = ChangeTracker.Entries().Where(e => IsBusiness(e.Metadata.ClrType)
            && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
        if (entries.Count == 0) return; // Identity registration does not create business data.
        var tenant = CurrentTenantId;
        if (tenant is null or <= 0) throw new TenantAccessException();
        foreach (var entry in entries)
        {
            if (entry.Entity is ITenantEntity root)
            {
                if (entry.State == EntityState.Added) root.DealershipId = tenant.Value;
                else if (root.DealershipId != tenant || entry.OriginalValues.GetValue<int>(nameof(ITenantEntity.DealershipId)) != tenant)
                    throw new TenantAccessException();
            }
        }
        // Reads use global filters. Detached Update/Delete and changed child foreign keys must also be checked.
        foreach (var entry in entries)
        {
            if (entry.State != EntityState.Added && !await IsVisible(entry.Metadata.ClrType, ((Entity)entry.Entity).Id, token))
                throw new TenantAccessException();
            foreach (var fk in entry.Metadata.GetForeignKeys().Where(f => IsBusiness(f.PrincipalEntityType.ClrType)))
            {
                var keyIndex = fk.PrincipalKey.Properties.ToList().FindIndex(p => p.Name == "Id");
                if (keyIndex < 0) continue;
                var key = entry.Property(fk.Properties[keyIndex].Name).CurrentValue;
                if (key is not int id) continue; // Optional relationship.
                var newParent = ChangeTracker.Entries().Any(e => e.Metadata.ClrType == fk.PrincipalEntityType.ClrType
                    && e.State == EntityState.Added && Equals(e.Property("Id").CurrentValue, id));
                if (!newParent && !await IsVisible(fk.PrincipalEntityType.ClrType, id, token)) throw new TenantAccessException();
            }
        }
    }
}
