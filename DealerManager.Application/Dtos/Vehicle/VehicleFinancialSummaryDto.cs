#nullable enable
namespace DealerManager.Application.Dtos.Vehicle;

public class VehicleFinancialSummaryDto
{
    public decimal ActualPurchasePrice { get; init; }
    public decimal ActualExpenses { get; init; }
    public decimal TotalInvested { get; init; }
    public decimal RemainingProjectedCosts { get; init; }
    public decimal ProjectedFinalCost { get; init; }
    public decimal? ExpectedSellingPrice { get; init; }
    public decimal? ProjectedProfit { get; init; }
    public decimal? ProjectedROI { get; init; }
}
