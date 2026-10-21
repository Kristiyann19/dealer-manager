using DealerManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DealerManager.Infrastructure.Persistence.Configurations;

public class CandidateAuctionDetailsConfiguration : IEntityTypeConfiguration<CandidateAuctionDetails>
{
    public void Configure(EntityTypeBuilder<CandidateAuctionDetails> builder)
    {
        builder.HasOne(x => x.Candidate).WithOne(c => c.AuctionDetails)
            .HasForeignKey<CandidateAuctionDetails>(x => x.CandidateId);
        builder.Property(x => x.Platform).IsRequired().HasMaxLength(120);
        builder.Property(x => x.LotNumber).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ListingUrl).HasMaxLength(2000);
        builder.Property(x => x.Country).HasMaxLength(100);
        builder.Property(x => x.ObservedBid).HasPrecision(18, 2);
        builder.Property(x => x.MaxBid).HasPrecision(18, 2);
        builder.ToTable(t => {
            t.HasCheckConstraint("CK_CandidateAuctionDetails_ObservedBid", "\"ObservedBid\" IS NULL OR \"ObservedBid\" >= 0");
            t.HasCheckConstraint("CK_CandidateAuctionDetails_MaxBid", "\"MaxBid\" IS NULL OR \"MaxBid\" > 0");
        });
    }
}
