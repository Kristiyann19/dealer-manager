#nullable enable
using System.ComponentModel.DataAnnotations;
using DealerManager.Domain.Enums;

namespace DealerManager.Application.Dtos.Candidate
{
    public class CreateCandidateRequest : IValidatableObject
    {
        [Required]
        public string Make { get; set; } = string.Empty;
        [Required]
        public string Model { get; set; } = string.Empty;
        public int? Year { get; set; }
        [Range(0, int.MaxValue)]
        public int? Mileage { get; set; }
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal? AskingPrice { get; set; }
        [MaxLength(32)]
        public string? Vin { get; set; }
        public string? Notes { get; set; }
        [EnumDataType(typeof(CandidateAcquisitionType))]
        public CandidateAcquisitionType AcquisitionType { get; set; }
        public CandidateAuctionDetailsDto? AuctionDetails { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (AcquisitionType == CandidateAcquisitionType.DirectPurchase)
            {
                if (AskingPrice == null)
                    yield return new ValidationResult("An asking price is required for direct purchases.", [nameof(AskingPrice)]);
                if (AuctionDetails != null)
                    yield return new ValidationResult("Direct purchases cannot have auction details.", [nameof(AuctionDetails)]);
            }
            if (AcquisitionType == CandidateAcquisitionType.Auction)
            {
                if (AskingPrice != null)
                    yield return new ValidationResult("Auction candidates use bids, not an asking price.", [nameof(AskingPrice)]);
                if (AuctionDetails == null)
                    yield return new ValidationResult("Auction details are required.", [nameof(AuctionDetails)]);
            }
        }
    }
}
