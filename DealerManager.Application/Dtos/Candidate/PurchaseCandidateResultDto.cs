namespace DealerManager.Application.Dtos.Candidate;

public class PurchaseCandidateResultDto
{
    public int CandidateId { get; init; }
    public int VehicleId { get; init; }
    public int CapitalAccountId { get; init; }
    public decimal ActualPurchasePrice { get; init; }
    public DateTimeOffset PurchaseDate { get; init; }
    public decimal PreviousBalance { get; init; }
    public decimal RemainingBalance { get; init; }
}
