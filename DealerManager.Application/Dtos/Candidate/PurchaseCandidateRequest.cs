#nullable enable
using System.ComponentModel.DataAnnotations;

namespace DealerManager.Application.Dtos.Candidate;

public class PurchaseCandidateRequest
{
    [Range(1, int.MaxValue)]
    public int CapitalAccountId { get; set; }
    [Range(typeof(decimal), "0", "79228162514264337593543950335", MinimumIsExclusive = true)]
    public decimal ActualPurchasePrice { get; set; }
    public DateTimeOffset? PurchaseDate { get; set; }
}
