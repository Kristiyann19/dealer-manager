using DealerManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace DealerManager.Infrastructure.Persistence;
public sealed class VehiclePhotoConfiguration : IEntityTypeConfiguration<VehiclePhoto>
{
    public void Configure(EntityTypeBuilder<VehiclePhoto> b)
    {
        b.HasOne(p => p.Vehicle).WithMany(v => v.VehiclePhotos).HasForeignKey(p => p.VehicleId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(p => p.VehicleId).IsUnique().HasFilter("\"IsPrimary\" = TRUE");
        b.HasIndex(p => new { p.VehicleId, p.SortOrder });
        b.Property(p => p.OriginalFileName).IsRequired().HasMaxLength(255);
        b.Property(p => p.StorageKey).IsRequired().HasMaxLength(500);
        b.Property(p => p.ContentType).IsRequired().HasMaxLength(100);
        b.Property(p => p.Caption).HasMaxLength(1000);
    }
}
public sealed class VehicleDocumentConfiguration : IEntityTypeConfiguration<VehicleDocument>
{
    public void Configure(EntityTypeBuilder<VehicleDocument> b)
    {
        b.HasOne(d => d.Vehicle).WithMany(v => v.VehicleDocuments).HasForeignKey(d => d.VehicleId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(d => d.VehicleId);
        b.Property(d => d.Title).IsRequired().HasMaxLength(200);
        b.Property(d => d.OriginalFileName).IsRequired().HasMaxLength(255);
        b.Property(d => d.StorageKey).IsRequired().HasMaxLength(500);
        b.Property(d => d.ContentType).IsRequired().HasMaxLength(100);
        b.Property(d => d.Notes).HasMaxLength(2000);
    }
}
