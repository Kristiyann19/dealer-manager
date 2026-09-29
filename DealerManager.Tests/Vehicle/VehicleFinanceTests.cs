using System.Net;
using System.Net.Http.Json;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.IRepository;
using DealerManager.Application.IService.Finance;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using DealerManager.Infrastructure.Service.Vehicle;
using DealerManager.Tests.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VehicleEntity = DealerManager.Domain.Entities.Vehicle;
using CandidateEntity = DealerManager.Domain.Entities.Candidate;

namespace DealerManager.Tests.VehicleFinance;

public partial class VehicleFinanceTests
{
    private sealed record SeedData(int Vehicle, int Account, int Transport, int Repair);
    private static async Task<SeedData> Seed(FinanceApiFactory factory)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var candidate = new CandidateEntity { Make = "BMW", Model = "320d", Year = 2018, Status = CandidateStatus.Purchased, AskingPrice = 6500,
            CandidateEstimates = [new CandidateEstimate { Version = 1, ExpectedSellingPrice = 12000, IsDecisionSnapshot = true,
                CandidateEstimateItems = [new() { Category = CostCategory.Purchase, Description = "Purchase", EstimatedAmount = 6500 },
                    new() { Category = CostCategory.Transport, Description = "Transport", EstimatedAmount = 1000 },
                    new() { Category = CostCategory.Repair, Description = "Repair", EstimatedAmount = 500 },
                    new() { Category = CostCategory.Cleaning, Description = "Cleaning", EstimatedAmount = 100 }] }] };
        var vehicle = new VehicleEntity { Make = "BMW", Model = "320d", Year = 2018, Mileage = 50000, Vin = "VIN", Status = VehicleStatus.Purchased,
            SourceCandidate = candidate, PurchaseDate = factory.Clock.Now,
            VehicleCostPlanItems = [new() { Category = CostCategory.Transport, Description = "Transport", CurrentEstimatedAmount = 1000 },
                new() { Category = CostCategory.Repair, Description = "Repair", CurrentEstimatedAmount = 500 },
                new() { Category = CostCategory.Cleaning, Description = "Cleaning", CurrentEstimatedAmount = 100 }] };
        var account = new CapitalAccount { Name = "Main EUR", Currency = "EUR", IsActive = true };
        db.Vehicles.Add(vehicle); db.CapitalAccounts.Add(account);
        await db.SaveChangesAsync();
        db.FinancialTransactions.AddRange(new FinancialTransaction { CapitalAccountId = account.Id, Amount = 20000, Type = TransactionType.CapitalContribution, Direction = TransactionDirection.In, Description = "Capital", OccurredAt = factory.Clock.Now },
            new FinancialTransaction { CapitalAccountId = account.Id, VehicleId = vehicle.Id, Amount = 6300, Type = TransactionType.VehiclePurchase, Direction = TransactionDirection.Out, Description = "Purchase", OccurredAt = factory.Clock.Now });
        await db.SaveChangesAsync();
        return new(vehicle.Id, account.Id, vehicle.VehicleCostPlanItems.Single(i => i.Category == CostCategory.Transport).Id, vehicle.VehicleCostPlanItems.Single(i => i.Category == CostCategory.Repair).Id);
    }
    private static AddVehicleExpenseRequest Expense(SeedData ids, decimal amount = 950, int? plan = null) => new()
    { CapitalAccountId = ids.Account, CostPlanItemId = plan, Category = CostCategory.Transport, Description = " Transport Italy ", Amount = amount, Supplier = " Carrier ", DocumentNumber = " Invoice 1 " };
    private static async Task<decimal> Balance(FinanceApiFactory factory, int id)
    {
        using var scope = factory.Services.CreateScope(); return await scope.ServiceProvider.GetRequiredService<ICapitalAccountService>().GetBalance(id, default);
    }

    [Fact]
    public async Task TransportFlowUpdatesPlanWithoutSpendingThenRecordsActualExpenseAndPreservesHistory()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        var before = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!;
        Assert.Equal(6300, before.ActualPurchasePrice); Assert.Equal(50000, before.Mileage); Assert.Equal("VIN", before.Vin);
        Assert.Equal(8100, before.OriginalForecast!.OriginalEstimatedTotal); Assert.Equal(6500, before.OriginalForecast.Items.Single(i => i.Category == CostCategory.Purchase).EstimatedAmount);
        var updated = await client.PutAsJsonAsync($"/api/vehicles/{ids.Vehicle}/cost-plan/{ids.Transport}", new { currentEstimatedAmount = 950, committedAmount = 950, description = " Transport agreed " });
        updated.EnsureSuccessStatusCode(); Assert.Equal(13700, await Balance(factory, ids.Account));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>(); Assert.Equal(2, await db.FinancialTransactions.CountAsync()); Assert.Empty(await db.VehicleExpenses.ToListAsync());
        }
        var response = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/cost-plan/{ids.Transport}/confirm-payment", new ConfirmVehiclePaymentRequest { ExpectedAmount = 950, ExpectedCapitalAccountId = ids.Account });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode); var expense = (await response.Content.ReadFromJsonAsync<VehicleExpenseDto>())!;
        Assert.Equal(950, expense.Amount); Assert.Equal("Transport agreed", expense.Description); Assert.Null(expense.Supplier); Assert.Null(expense.DocumentNumber); Assert.Equal(factory.Clock.Now, expense.PaidAt);
        Assert.Equal("Main EUR", expense.CapitalAccountName); Assert.Equal(12750, await Balance(factory, ids.Account));
        var plan = (await client.GetFromJsonAsync<List<VehicleCostPlanItemDto>>($"/api/vehicles/{ids.Vehicle}/cost-plan"))!;
        var transport = plan.Single(i => i.Id == ids.Transport); Assert.Equal(950, transport.ActualPaid); Assert.Equal(0, transport.RemainingProjected);
        var after = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!;
        Assert.Equal(7250, after.TotalInvested); Assert.Equal(950, after.ActualExpenses); Assert.Equal(600, after.RemainingProjectedCosts); Assert.Equal(7850, after.ProjectedFinalCost);
        Assert.Equal(4150, after.ProjectedProfit); Assert.Equal(4150m / 7850m * 100m, after.ProjectedROI);
        Assert.Equal(before.OriginalForecast.OriginalEstimatedTotal, after.OriginalForecast!.OriginalEstimatedTotal);
        var summary = (await client.GetFromJsonAsync<VehicleFinancialSummaryDto>($"/api/vehicles/{ids.Vehicle}/financial-summary"))!; Assert.Equal(after.ProjectedFinalCost, summary.ProjectedFinalCost);
        using var verify = factory.Services.CreateScope(); var context = verify.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var movement = await context.FinancialTransactions.SingleAsync(t => t.Id == expense.FinancialTransactionId);
        Assert.Equal(TransactionDirection.Out, movement.Direction); Assert.Equal(TransactionType.VehicleExpense, movement.Type); Assert.Equal(expense.Amount, movement.Amount); Assert.Equal(ids.Vehicle, movement.VehicleId);
        Assert.Single(await context.VehicleExpenses.ToListAsync());
    }

    [Fact]
    public async Task PartialPaymentsOverrunsUnexpectedAndCancelledPlansNeverDoubleCount()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        await SeedHistoricalExpense(factory, ids, 300, ids.Repair);
        var first = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!; Assert.Equal(7900, first.ProjectedFinalCost);
        var plan = (await client.GetFromJsonAsync<List<VehicleCostPlanItemDto>>($"/api/vehicles/{ids.Vehicle}/cost-plan"))!; Assert.Equal(200, plan.Single(p => p.Id == ids.Repair).RemainingProjected);
        await SeedHistoricalExpense(factory, ids, 350, ids.Repair);
        var unexpected = Expense(ids, 180); unexpected.Category = CostCategory.Parts; unexpected.PaidAt = factory.Clock.Now.AddDays(1);
        (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/expenses", unexpected)).EnsureSuccessStatusCode();
        plan = (await client.GetFromJsonAsync<List<VehicleCostPlanItemDto>>($"/api/vehicles/{ids.Vehicle}/cost-plan"))!;
        Assert.Equal(650, plan.Single(p => p.Id == ids.Repair).ActualPaid); Assert.Equal(0, plan.Single(p => p.Id == ids.Repair).RemainingProjected);
        var history = (await client.GetFromJsonAsync<List<VehicleExpenseDto>>($"/api/vehicles/{ids.Vehicle}/expenses"))!;
        Assert.Equal(3, history.Count); Assert.Null(history[0].CostPlanItemId); Assert.Equal(180, history[0].Amount); Assert.Equal(350, history[1].Amount);
        var after = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!; Assert.Equal(7130, after.TotalInvested); Assert.Equal(8230, after.ProjectedFinalCost);
        (await client.PutAsJsonAsync($"/api/vehicles/{ids.Vehicle}/cost-plan/{ids.Transport}", new { currentEstimatedAmount = 1000, isCancelled = true })).EnsureSuccessStatusCode();
        var cancelled = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!; Assert.Equal(100, cancelled.RemainingProjectedCosts); Assert.Equal(7230, cancelled.ProjectedFinalCost);
        // Cancelling a paid plan preserves its actual payments in total invested.
        (await client.PutAsJsonAsync($"/api/vehicles/{ids.Vehicle}/cost-plan/{ids.Repair}", new { currentEstimatedAmount = 500, isCancelled = true })).EnsureSuccessStatusCode();
        Assert.Equal(12870, await Balance(factory, ids.Account));
    }

    [Theory]
    [InlineData("funds", 409)] [InlineData("sold", 409)] [InlineData("inactive", 409)] [InlineData("currency", 409)]
    [InlineData("wrongPlan", 409)] [InlineData("missingPlan", 409)] [InlineData("missingAccount", 409)]
    [InlineData("negative", 400)] [InlineData("zero", 400)] [InlineData("description", 400)] [InlineData("category", 400)]
    public async Task InvalidExpenseCreatesNeitherRecord(string scenario, int status)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        var request = Expense(ids);
        if (scenario == "funds") request.Amount = 14000;
        if (scenario == "negative") request.Amount = -1;
        if (scenario == "zero") request.Amount = 0;
        if (scenario == "description") request.Description = "   ";
        if (scenario == "category") request.Category = (CostCategory)999;
        if (scenario == "missingAccount") request.CapitalAccountId = 999;
        if (scenario == "missingPlan") request.CostPlanItemId = 999;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
            if (scenario == "sold") (await db.Vehicles.SingleAsync()).Status = VehicleStatus.Sold;
            if (scenario == "inactive") (await db.CapitalAccounts.SingleAsync()).IsActive = false;
            if (scenario == "currency") (await db.CapitalAccounts.SingleAsync()).Currency = "USD";
            if (scenario == "wrongPlan")
            {
                var other = new VehicleEntity { Make = "Audi", Model = "A3", VehicleCostPlanItems = [new() { Description = "Other" }] };
                db.Vehicles.Add(other); await db.SaveChangesAsync(); request.CostPlanItemId = other.VehicleCostPlanItems.Single().Id;
            }
            await db.SaveChangesAsync();
        }
        Assert.Equal(status, (int)(await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/expenses", request)).StatusCode);
        using var verify = factory.Services.CreateScope(); var context = verify.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        Assert.Equal(2, await context.FinancialTransactions.CountAsync()); Assert.Empty(await context.VehicleExpenses.ToListAsync());
    }

    [Fact]
    public async Task InvalidPlanUpdateAndUnknownVehicleAreRejected()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        foreach (var request in new[] { new UpdateVehicleCostPlanItemRequest { CurrentEstimatedAmount = -1 }, new UpdateVehicleCostPlanItemRequest { CommittedAmount = -1 }, new UpdateVehicleCostPlanItemRequest { Description = " " } })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/vehicles/{ids.Vehicle}/cost-plan/{ids.Transport}", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/vehicles/{ids.Vehicle}/cost-plan/999", new { currentEstimatedAmount = 0 })).StatusCode);
        foreach (var suffix in new[] { "", "/cost-plan", "/expenses", "/financial-summary" })
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/vehicles/999" + suffix)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/vehicles/999/expenses", Expense(ids))).StatusCode);
    }

    [Fact]
    public async Task NoSnapshotHasNoExpectedProfitAndZeroCostHasZeroRoi()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient();
        int id;
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>(); var vehicle = new VehicleEntity { Make = "BMW", Model = "X1", Year = 2020 }; db.Vehicles.Add(vehicle); await db.SaveChangesAsync(); id = vehicle.Id; }
        var details = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{id}"))!;
        Assert.Null(details.OriginalForecast); Assert.Null(details.ExpectedSellingPrice); Assert.Null(details.ProjectedProfit); Assert.Equal(0, details.ProjectedROI);
    }

    [Theory]
    [InlineData(1)] [InlineData(2)]
    public async Task FailureAfterEitherSaveRollsBackBothRecords(int failAt)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        using var scope = factory.Services.CreateScope(); var services = scope.ServiceProvider;
        var service = ActivatorUtilities.CreateInstance<VehicleService>(services, new FailOnSave(services.GetRequiredService<IUnitOfWork>(), failAt));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmPayment(ids.Vehicle, ids.Transport, new() { ExpectedAmount = 1000, ExpectedCapitalAccountId = ids.Account }, default));
        var db = services.GetRequiredService<DealerManagerDbContext>(); Assert.Equal(2, await db.FinancialTransactions.CountAsync()); Assert.Empty(await db.VehicleExpenses.ToListAsync());
        Assert.Equal(13700, await Balance(factory, ids.Account));
    }
    private sealed class FailOnSave(IUnitOfWork inner, int failAt) : IUnitOfWork
    {
        private int saves;
        public async Task<int> SaveChanges(CancellationToken token) { var result = await inner.SaveChanges(token); if (++saves == failAt) throw new InvalidOperationException("Simulated failure"); return result; }
        public Task<T> ExecuteInTransaction<T>(Func<CancellationToken, Task<T>> action, CancellationToken token) => inner.ExecuteInTransaction(action, token);
    }
}

