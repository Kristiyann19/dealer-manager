using DealerManager.Infrastructure.Service.Finance;
#nullable enable
using DealerManager.Application.Dtos.Candidate;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.FilterDtos;
using DealerManager.Application.IRepository;
using DealerManager.Application.IService.Candidate;
using DealerManager.Application.IService.Finance;
using DealerManager.Application.IService.Vehicle;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using VehicleEntity = DealerManager.Domain.Entities.Vehicle;

namespace DealerManager.Infrastructure.Service.Vehicle;
public partial class VehicleService(
    IBaseRepository<VehicleEntity, FilterDto<VehicleEntity>, DealerManagerDbContext> vehicles,
    IBaseRepository<FinancialTransaction, FilterDto<FinancialTransaction>, DealerManagerDbContext> transactions,
    IBaseRepository<VehicleCostPlanItem, FilterDto<VehicleCostPlanItem>, DealerManagerDbContext> plans,
    IBaseRepository<VehicleExpense, FilterDto<VehicleExpense>, DealerManagerDbContext> expenses,
    IBaseRepository<CapitalAccount, FilterDto<CapitalAccount>, DealerManagerDbContext> accounts,
    IBaseRepository<CandidateEstimate, FilterDto<CandidateEstimate>, DealerManagerDbContext> estimates,
    IBaseRepository<VehicleStatusHistory, FilterDto<VehicleStatusHistory>, DealerManagerDbContext> statusHistory,
    IBaseRepository<VehicleListing, FilterDto<VehicleListing>, DealerManagerDbContext> listings,
    IBaseRepository<VehicleSale, FilterDto<VehicleSale>, DealerManagerDbContext> sales,
    ICapitalAccountService finance, ICandidateFinancialCalculator forecastCalculator,
    IUnitOfWork unitOfWork, TimeProvider clock) : IVehicleService
{
    public async Task<VehicleDetailsDto> GetDetails(int id, CancellationToken cancellationToken)
    {
        var vehicle = await LoadVehicle(id, cancellationToken);
        var investment = await FinancialQueries.Investments(vehicles.GetQueryByProperties(v => v.Id == id),
            transactions.GetQueryByProperties(_ => true)).SingleAsync(cancellationToken);
        var purchasePrice = investment.ActualPurchasePrice;
        var actualExpenses = investment.ActualExpenses;
        var plan = await LoadCostPlan(id, cancellationToken);
        var snapshot = vehicle.SourceCandidateId is { } candidateId
            ? await estimates.GetByProperties(e => e.CandidateId == candidateId && e.IsDecisionSnapshot,
                cancellationToken, query => query.Include(e => e.CandidateEstimateItems)) : null;
        OriginalForecastDto? original = null;
        if (snapshot != null)
        {
            var analysis = forecastCalculator.Calculate(snapshot.ExpectedSellingPrice, snapshot.CandidateEstimateItems.Select(i => i.EstimatedAmount));
            original = new OriginalForecastDto
            {
                EstimateId = snapshot.Id, Version = snapshot.Version,
                Items = snapshot.CandidateEstimateItems.OrderBy(i => i.Id).Select(i => new CandidateEstimateItemDto
                { Id = i.Id, Category = i.Category, Description = i.Description, EstimatedAmount = i.EstimatedAmount }).ToList(),
                OriginalEstimatedTotal = analysis.EstimatedTotalCost, OriginalExpectedSellingPrice = snapshot.ExpectedSellingPrice,
                OriginalExpectedProfit = analysis.ExpectedProfit, OriginalExpectedROI = analysis.ExpectedRoi
            };
        }
        var sale = await sales.GetByProperties(s => s.VehicleId == id, cancellationToken,
            query => query.Include(s => s.FinancialTransaction).ThenInclude(t => t.CapitalAccount));
        var summary = CalculateFinancialSummary(purchasePrice, actualExpenses, plan, snapshot?.ExpectedSellingPrice, sale?.SalePrice, sale?.SoldAt);
        var latestListing = await listings.GetQueryByProperties(l => l.VehicleId == id)
            .OrderByDescending(l => l.Id).Select(ListingProjection).FirstOrDefaultAsync(cancellationToken);
        return new VehicleDetailsDto
        {
            Id = vehicle.Id, Make = vehicle.Make, Model = vehicle.Model, Year = vehicle.Year,
            Mileage = vehicle.Mileage, Vin = vehicle.Vin, Status = vehicle.Status,
            SourceCandidateId = vehicle.SourceCandidateId, PurchaseDate = vehicle.PurchaseDate,
            CurrentListing = await LoadCurrentListing(id, cancellationToken),
            ListingPrice = latestListing?.ListingPrice, ListedAt = latestListing?.ListedAt,
            ActualSalePrice = summary.ActualSalePrice, SoldAt = summary.SoldAt,
            RealizedProfit = summary.RealizedProfit, RealizedROI = summary.RealizedROI,
            SaleAccount = sale == null ? null : new VehiclePaymentAccountDto {
                CapitalAccountId = sale.FinancialTransaction.CapitalAccountId,
                Currency = sale.FinancialTransaction.CapitalAccount.Currency,
                CurrentBalance = await finance.GetBalance(sale.FinancialTransaction.CapitalAccountId, cancellationToken)
            },
            ActualPurchasePrice = summary.ActualPurchasePrice, ActualExpenses = summary.ActualExpenses,
            TotalInvested = summary.TotalInvested, RemainingProjectedCosts = summary.RemainingProjectedCosts,
            ProjectedFinalCost = summary.ProjectedFinalCost, ExpectedSellingPrice = summary.ExpectedSellingPrice,
            ProjectedProfit = summary.ProjectedProfit, ProjectedROI = summary.ProjectedROI, OriginalForecast = original
        };
    }

    private static VehicleFinancialSummaryDto CalculateFinancialSummary(decimal purchase, decimal actual, IEnumerable<VehicleCostPlanItemDto> plan, decimal? selling,
        decimal? salePrice = null, DateTimeOffset? soldAt = null)
    {
        try
        {
            var remaining = plan.Sum(i => i.RemainingProjected);
            var invested = purchase + actual;
            var projected = invested + remaining;
            var profit = selling - projected;
            var realized = CalculateRealized(invested, salePrice);
            return new() { ActualPurchasePrice = purchase, ActualExpenses = actual, TotalInvested = invested,
                RemainingProjectedCosts = remaining, ProjectedFinalCost = projected, ExpectedSellingPrice = selling,
                ProjectedProfit = profit, ProjectedROI = projected == 0 ? 0 : profit / projected * 100m,
                ActualSalePrice = salePrice, SoldAt = soldAt, RealizedProfit = realized.Profit, RealizedROI = realized.ROI };
        }
        catch (OverflowException) { throw new ValidationException("Financial amounts exceed the supported decimal range."); }
    }

    public async Task<VehicleFinancialSummaryDto> GetFinancialSummary(int id, CancellationToken cancellationToken)
        => await GetDetails(id, cancellationToken);

    public async Task<IReadOnlyList<VehicleCostPlanItemDto>> GetCostPlan(int vehicleId, CancellationToken cancellationToken)
    {
        await LoadVehicle(vehicleId, cancellationToken);
        return await LoadCostPlan(vehicleId, cancellationToken);
    }

    public Task<VehicleCostPlanItemDto> UpdateCostPlanItem(int vehicleId, int itemId, UpdateVehicleCostPlanItemRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        if (request.Description != null && string.IsNullOrWhiteSpace(request.Description))
            throw new ValidationException("Description must not be blank.");
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            await LoadVehicle(vehicleId, token);
            var item = await plans.GetById(itemId, token, query => query.AsTracking());
            if (item == null || item.VehicleId != vehicleId)
                throw Conflict("planMismatch", "The cost plan item does not belong to this vehicle.");
            item.CurrentEstimatedAmount = request.CurrentEstimatedAmount;
            item.CommittedAmount = request.CommittedAmount;
            if (request.Description != null) item.Description = request.Description.Trim();
            item.IsCancelled = request.IsCancelled;
            await unitOfWork.SaveChanges(token);
            return await plans.GetQueryByProperties(i => i.Id == itemId).Select(PlanProjection).SingleAsync(token);
        }, cancellationToken);
    }

    public async Task<VehiclePaymentAccountDto> GetPaymentAccount(int vehicleId, CancellationToken cancellationToken)
    {
        await RequirePayableVehicle(vehicleId, cancellationToken);
        var account = await PurchaseAccount(vehicleId, cancellationToken);
        return new() { CapitalAccountId = account.Id, Currency = account.Currency,
            CurrentBalance = await finance.GetBalance(account.Id, cancellationToken) };
    }

    public async Task<VehiclePaymentPreviewDto> GetPaymentPreview(int vehicleId, int itemId, CancellationToken cancellationToken)
    {
        var account = await GetPaymentAccount(vehicleId, cancellationToken);
        var item = await PayablePlan(vehicleId, itemId, cancellationToken);
        return new() { CapitalAccountId = account.CapitalAccountId, Currency = account.Currency,
            CurrentBalance = account.CurrentBalance, Category = item.Category, Amount = Target(item) };
    }

    public Task<VehicleExpenseDto> ConfirmPayment(int vehicleId, int itemId, ConfirmVehiclePaymentRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            await RequirePayableVehicle(vehicleId, token);
            var item = await PayablePlan(vehicleId, itemId, token);
            var account = await PurchaseAccount(vehicleId, token);
            var amount = Target(item);
            if (request.ExpectedAmount != amount || request.ExpectedCapitalAccountId != account.Id)
                throw Conflict("paymentChanged", "The payment has changed. Review a fresh preview before confirming.");
            return await RecordExpense(vehicleId, account.Id, item.Id, item.Category, item.Description, amount,
                null, null, null, token);
        }, cancellationToken);
    }

    public Task<VehicleExpenseDto> AddExpense(int vehicleId, AddVehicleExpenseRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            await RequirePayableVehicle(vehicleId, token);
            if (request.CostPlanItemId != null)
                throw Conflict("confirmationRequired", "Use the cost plan payment confirmation for planned expenses.");
            var account = await PurchaseAccount(vehicleId, token);
            if (request.CapitalAccountId is { } requestedAccount && requestedAccount != account.Id)
                throw Conflict("purchaseAccountMismatch", "Expenses must use the vehicle purchase account.");
            return await RecordExpense(vehicleId, account.Id, null, request.Category, request.Description, request.Amount,
                request.PaidAt, request.Supplier, request.DocumentNumber, token);
        }, cancellationToken);
    }

    private async Task RequirePayableVehicle(int vehicleId, CancellationToken token)
    {
        var vehicle = await LoadVehicle(vehicleId, token);
        if (vehicle.Status == VehicleStatus.Sold) throw Conflict("sold", "Expenses cannot be added to a sold vehicle.");
    }

    private async Task<CapitalAccount> PurchaseAccount(int vehicleId, CancellationToken token)
    {
        var accountIds = await transactions.GetQueryByProperties(t => t.VehicleId == vehicleId
                && t.Type == TransactionType.VehiclePurchase && t.Direction == TransactionDirection.Out)
            .Select(t => t.CapitalAccountId).Distinct().ToListAsync(token);
        if (accountIds.Count != 1)
            throw Conflict("purchaseAccountMissing", "A unique vehicle purchase account could not be determined.");
        var account = await accounts.GetById(accountIds[0], token)
            ?? throw Conflict("purchaseAccountMissing", "The vehicle purchase account could not be found.");
        if (!account.IsActive) throw Conflict("inactive", "The capital account is inactive.");
        if (!string.Equals(account.Currency.Trim(), "EUR", StringComparison.OrdinalIgnoreCase))
            throw Conflict("currency", "Vehicle expenses require an EUR account.");
        return account;
    }

    private async Task<VehicleCostPlanItemDto> PayablePlan(int vehicleId, int itemId, CancellationToken token)
    {
        var item = await plans.GetQueryByProperties(i => i.Id == itemId && i.VehicleId == vehicleId)
            .Select(PlanProjection).SingleOrDefaultAsync(token)
            ?? throw Conflict("planMismatch", "The cost plan item does not belong to this vehicle.");
        if (item.IsCancelled) throw Conflict("cancelledPlan", "A cancelled forecast cannot be paid.");
        if (Target(item) <= 0) throw Conflict("invalidPlanAmount", "Set a positive current forecast before confirming payment.");
        if (item.ActualPaid >= Target(item)) throw Conflict("alreadyPaid", "This forecast has already been paid.");
        if (item.ActualPaid > 0) throw Conflict("partialPayment", "This forecast has historical partial payments and cannot be confirmed in full.");
        return item;
    }

    private static decimal Target(VehicleCostPlanItemDto item) => item.CommittedAmount ?? item.CurrentEstimatedAmount ?? 0m;

    // Called only inside the caller's serializable UnitOfWork transaction.
    private async Task<VehicleExpenseDto> RecordExpense(int vehicleId, int accountId, int? itemId, CostCategory category,
        string description, decimal amount, DateTimeOffset? paidAt, string? supplier, string? documentNumber, CancellationToken token)
    {
        if (await finance.GetBalance(accountId, token) < amount)
            throw Conflict("insufficientCapital", "Insufficient available capital for this expense.");
        var now = clock.GetUtcNow();
        var movement = new FinancialTransaction
        {
            CapitalAccountId = accountId, VehicleId = vehicleId, Type = TransactionType.VehicleExpense,
            Direction = TransactionDirection.Out, Amount = amount, Description = description.Trim(),
            OccurredAt = paidAt?.ToUniversalTime() ?? now, CreatedAt = now
        };
        await transactions.Create(movement);
        // Obtain the FK without passing a tracked graph through the repository's graph helper.
        await unitOfWork.SaveChanges(token);
        var expense = new VehicleExpense
        {
            VehicleId = vehicleId, CostPlanItemId = itemId, Category = category,
            Description = movement.Description, Amount = amount, PaidAt = movement.OccurredAt, CreatedAt = now,
            Supplier = Normalize(supplier), DocumentNumber = Normalize(documentNumber), FinancialTransactionId = movement.Id
        };
        await expenses.Create(expense);
        await unitOfWork.SaveChanges(token);
        return await expenses.GetQueryByProperties(e => e.Id == expense.Id).Select(ExpenseProjection).SingleAsync(token);
    }

    public async Task<IReadOnlyList<VehicleExpenseDto>> GetExpenses(int vehicleId, CancellationToken cancellationToken)
    {
        await LoadVehicle(vehicleId, cancellationToken);
        return await expenses.GetQueryByProperties(e => e.VehicleId == vehicleId)
            .OrderByDescending(e => e.PaidAt).ThenByDescending(e => e.Id).Select(ExpenseProjection).ToListAsync(cancellationToken);
    }

    private async Task<VehicleEntity> LoadVehicle(int id, CancellationToken token)
        => await vehicles.GetById(id, token) ?? throw new VehicleNotFoundException(id);
    private async Task<IReadOnlyList<VehicleCostPlanItemDto>> LoadCostPlan(int id, CancellationToken token)
        => await plans.GetQueryByProperties(i => i.VehicleId == id).OrderBy(i => i.Id).Select(PlanProjection).ToListAsync(token);
    private static readonly Expression<Func<VehicleCostPlanItem, VehicleCostPlanItemDto>> PlanProjection = i => new()
    {
        Id = i.Id, Category = i.Category, Description = i.Description, CurrentEstimatedAmount = i.CurrentEstimatedAmount,
        CommittedAmount = i.CommittedAmount, IsCancelled = i.IsCancelled,
        ActualPaid = i.VehicleExpenses.Sum(e => (decimal?)e.Amount) ?? 0m
    };
    private static readonly Expression<Func<VehicleExpense, VehicleExpenseDto>> ExpenseProjection = e => new()
    {
        Id = e.Id, Category = e.Category, Description = e.Description, Amount = e.Amount,
        Supplier = e.Supplier, DocumentNumber = e.DocumentNumber, PaidAt = e.PaidAt,
        CostPlanItemId = e.CostPlanItemId, CostPlanDescription = e.CostPlanItem == null ? null : e.CostPlanItem.Description,
        FinancialTransactionId = e.FinancialTransactionId, CapitalAccountId = e.FinancialTransaction.CapitalAccountId,
        CapitalAccountName = e.FinancialTransaction.CapitalAccount.Name, Currency = e.FinancialTransaction.CapitalAccount.Currency
    };
    private static VehicleConflictException Conflict(string code, string message) => new(code, message);
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Validate(object? request)
    {
        if (request == null) throw new ValidationException("A request is required.");
        Validator.ValidateObject(request, new ValidationContext(request), true);
    }
}
