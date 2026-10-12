using DealerManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace DealerManager.Infrastructure.Persistence;
public class VehicleReservationConfiguration : IEntityTypeConfiguration<VehicleReservation>
{
    public void Configure(EntityTypeBuilder<VehicleReservation> b)
    {
        b.Property(r => r.CustomerName).IsRequired().HasMaxLength(200);
        b.Property(r => r.CustomerPhone).HasMaxLength(50);
        b.Property(r => r.CustomerEmail).HasMaxLength(254);
        b.Property(r => r.Notes).HasMaxLength(2000);
        b.Property(r => r.CancellationNotes).HasMaxLength(2000);
        b.HasIndex(r => r.VehicleId).IsUnique().HasFilter("\"Status\" = 0");
        b.HasIndex(r => r.DepositTransactionId).IsUnique();
        b.HasIndex(r => r.RefundTransactionId).IsUnique();
        b.HasOne(r => r.Vehicle).WithMany().HasForeignKey(r => r.VehicleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.DepositTransaction).WithMany().HasForeignKey(r => r.DepositTransactionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.RefundTransaction).WithMany().HasForeignKey(r => r.RefundTransactionId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint("CK_VehicleReservations_PositiveDeposit", "\"DepositAmount\" > 0"));
    }
}
