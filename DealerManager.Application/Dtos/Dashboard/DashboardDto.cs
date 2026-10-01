#nullable enable
using DealerManager.Application.Dtos.Candidate;
using DealerManager.Domain.Enums;
namespace DealerManager.Application.Dtos.Dashboard;

public sealed class DashboardDto
{
    public DateTimeOffset AsOf { get; init; }
    public OperationalSummaryDto Monthly { get; init; } = new();
    public FinancialSummaryDto Financial { get; init; } = new();
    public PipelineDto Pipeline { get; init; } = new();
    public IReadOnlyList<CandidateListDto> Candidates { get; init; } = [];
    public IReadOnlyList<DashboardVehicleDto> ActiveVehicles { get; init; } = [];
    public IReadOnlyList<MonthlyFinancialDto> MonthlyFinancials { get; init; } = [];
}
public sealed class OperationalSummaryDto
{
    public int CarsInStock { get; init; }
    public int CarsInRepair { get; init; }
    public int SoldThisMonth { get; init; }
    public decimal ProfitThisMonth { get; init; }
}
public sealed class FinancialSummaryDto
{
    public decimal AvailableCash { get; init; }
    public decimal CapitalInvested { get; init; }
    public decimal UpcomingProjectedCosts { get; init; }
    public decimal NetWorthAtCost => AvailableCash + CapitalInvested;
    public string Currency { get; init; } = "EUR";
}
public sealed class PipelineDto
{
    public int Candidates { get; init; }
    public int Transporting { get; init; }
    public int Repairing { get; init; }
    public int ReadyForSale { get; init; }
    public int Listed { get; init; }
}
public sealed class DashboardVehicleDto
{
    public int Id { get; init; }
    public string Make { get; init; } = "";
    public string Model { get; init; } = "";
    public int Year { get; init; }
    public VehicleStatus Status { get; init; }
    public decimal TotalInvested { get; init; }
    public decimal RemainingProjectedCosts { get; init; }
    public decimal ProjectedFinalCost => TotalInvested + RemainingProjectedCosts;
    public decimal? ExpectedSellingPrice { get; init; }
    public decimal? ListingPrice { get; init; }
    // Match Vehicle financials: the decision forecast is the expected price, not the listing.
    public decimal? ProjectedProfit => ExpectedSellingPrice - ProjectedFinalCost;
}
public sealed class MonthlyFinancialDto
{
    public DateTimeOffset Month { get; init; }
    public decimal CapitalInvested { get; init; }
    public decimal SalesRevenue { get; init; }
    public decimal RealizedProfit { get; init; }
    public int SalesCount { get; init; }
}
