using DealerManager.Application.Dtos.Finance;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
namespace DealerManager.Infrastructure.Service.Finance;

// Composable, provider-translatable queries. No materialization or per-entity round trips.
internal static class FinancialQueries
{
    public static IQueryable<CapitalAccountDto> AccountBalances(IQueryable<CapitalAccount> accounts,
        IQueryable<FinancialTransaction> movements) => accounts.Select(account => new CapitalAccountDto
        {
            Id = account.Id, Name = account.Name, Currency = account.Currency, IsActive = account.IsActive,
            CurrentBalance = (movements.Where(t => t.CapitalAccountId == account.Id && t.Direction == TransactionDirection.In)
                .Sum(t => (decimal?)t.Amount) ?? 0m)
                - (movements.Where(t => t.CapitalAccountId == account.Id && t.Direction == TransactionDirection.Out)
                .Sum(t => (decimal?)t.Amount) ?? 0m)
        });

    public static IQueryable<decimal> RemainingCosts(IQueryable<VehicleCostPlanItem> plans) => plans
        .Select(i => new VehicleCostPlanItemDto {
            CurrentEstimatedAmount = i.CurrentEstimatedAmount, CommittedAmount = i.CommittedAmount,
            IsCancelled = i.IsCancelled, ActualPaid = i.VehicleExpenses.Sum(e => (decimal?)e.Amount) ?? 0m
        }).Select(CostPlanCalculation.RemainingExpression);

    public static IQueryable<VehicleInvestment> Investments(IQueryable<DealerManager.Domain.Entities.Vehicle> vehicles,
        IQueryable<FinancialTransaction> transactions) => vehicles.Select(v => new {
            VehicleId = v.Id,
            Purchase = transactions.Where(t => t.VehicleId == v.Id && t.Type == TransactionType.VehiclePurchase
                && t.Direction == TransactionDirection.Out).Sum(t => (decimal?)t.Amount) ?? 0m,
            Expenses = v.VehicleExpenses.Sum(e => (decimal?)e.Amount) ?? 0m
        }).Select(v => new VehicleInvestment {
            VehicleId = v.VehicleId, ActualPurchasePrice = v.Purchase, ActualExpenses = v.Expenses,
            TotalInvested = v.Purchase + v.Expenses
        });
}
internal sealed class VehicleInvestment
{
    public int VehicleId { get; init; }
    public decimal ActualPurchasePrice { get; init; }
    public decimal ActualExpenses { get; init; }
    public decimal TotalInvested { get; init; }
}
