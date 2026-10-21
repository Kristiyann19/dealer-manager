#nullable enable
using System.ComponentModel.DataAnnotations;

namespace DealerManager.Application.Dtos.Candidate;

// Shared write/read contract. The candidate owns its one-to-one auction record.
public class CandidateAuctionDetailsDto : IValidatableObject
{
    [Required, MaxLength(120)]
    public string Platform { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    public string LotNumber { get; set; } = string.Empty;
    [MaxLength(2000)]
    public string? ListingUrl { get; set; }
    [MaxLength(100)]
    public string? Country { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? ObservedBid { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal? MaxBid { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ObservedBid is { } bid && decimal.Round(bid, 2) != bid)
            yield return new ValidationResult("Use at most two decimal places.", [nameof(ObservedBid)]);
        if (MaxBid is { } max && decimal.Round(max, 2) != max)
            yield return new ValidationResult("Use at most two decimal places.", [nameof(MaxBid)]);
        if (!string.IsNullOrWhiteSpace(ListingUrl)
            && (!Uri.TryCreate(ListingUrl.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            yield return new ValidationResult("The listing URL must use HTTP or HTTPS.", [nameof(ListingUrl)]);
    }
}
