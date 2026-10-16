using DealerManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace DealerManager.Infrastructure.Persistence;

public sealed class VehicleExpenseConfiguration : IEntityTypeConfiguration<VehicleExpense>
{
    public void Configure(EntityTypeBuilder<VehicleExpense> b)
    {
        b.Property(e => e.IsReversed).IsConcurrencyToken();
        b.Property(e => e.ReversalReason).HasMaxLength(1000);
        b.HasIndex(e => e.ReversalTransactionId).IsUnique();
        b.HasIndex(e => e.ReplacementExpenseId).IsUnique();
        b.HasOne(e => e.ReversalTransaction).WithMany().HasForeignKey(e => e.ReversalTransactionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.ReplacementExpense).WithMany().HasForeignKey(e => e.ReplacementExpenseId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint("CK_VehicleExpenses_ReversalAudit",
            "(NOT \"IsReversed\" AND \"ReversedAt\" IS NULL AND \"ReversalTransactionId\" IS NULL AND \"ReplacementExpenseId\" IS NULL) OR (\"IsReversed\" AND \"ReversedAt\" IS NOT NULL AND \"ReversalTransactionId\" IS NOT NULL AND \"ReversalReason\" IS NOT NULL AND length(trim(\"ReversalReason\")) > 0)"));
    }
}
