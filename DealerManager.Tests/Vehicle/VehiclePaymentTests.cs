using System.Net;
using System.Net.Http.Json;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using DealerManager.Tests.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DealerManager.Tests.VehicleFinance;

public partial class VehicleFinanceTests
{
    private static async Task SeedHistoricalExpense(FinanceApiFactory factory, SeedData ids, decimal amount, int plan)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        db.VehicleExpenses.Add(new VehicleExpense
        {
            VehicleId = ids.Vehicle, CostPlanItemId = plan, Category = CostCategory.Repair,
            Description = "Historical repair", Amount = amount, PaidAt = factory.Clock.Now,
            FinancialTransaction = new FinancialTransaction { CapitalAccountId = ids.Account, VehicleId = ids.Vehicle,
                Type = TransactionType.VehicleExpense, Direction = TransactionDirection.Out, Amount = amount,
                Description = "Historical repair", OccurredAt = factory.Clock.Now }
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Confirm670From5000RefreshesHistoryTotalsAndBalanceWithoutChangingSnapshot()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient();
        var ids = await Seed(factory);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
            (await db.VehicleCostPlanItems.SingleAsync(i => i.Id == ids.Transport)).CurrentEstimatedAmount = 670;
            (await db.FinancialTransactions.SingleAsync(t => t.Type == TransactionType.CapitalContribution)).Amount = 11300;
            // A second available account must never be selected instead of the purchase account.
            db.CapitalAccounts.Add(new CapitalAccount { Name = "Other", Currency = "EUR", IsActive = true });
            await db.SaveChangesAsync();
        }
        var url = $"/api/vehicles/{ids.Vehicle}/cost-plan/{ids.Transport}";
        var before = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!;
        var preview = (await client.GetFromJsonAsync<VehiclePaymentPreviewDto>(url + "/payment-preview"))!;
        Assert.Equal(670, preview.Amount); Assert.Equal(5000, preview.CurrentBalance); Assert.Equal(ids.Account, preview.CapitalAccountId);
        var request = new ConfirmVehiclePaymentRequest { ExpectedAmount = preview.Amount, ExpectedCapitalAccountId = preview.CapitalAccountId };
        var response = await client.PostAsJsonAsync(url + "/confirm-payment", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var expense = (await response.Content.ReadFromJsonAsync<VehicleExpenseDto>())!;
        Assert.Equal(670, expense.Amount); Assert.Equal(ids.Transport, expense.CostPlanItemId); Assert.Equal(ids.Account, expense.CapitalAccountId);
        Assert.Equal(CostCategory.Transport, expense.Category); Assert.Equal("Transport", expense.Description);
        var balance = (await client.GetFromJsonAsync<VehiclePaymentAccountDto>($"/api/vehicles/{ids.Vehicle}/payment-account"))!;
        Assert.Equal(4330, balance.CurrentBalance);
        var plans = (await client.GetFromJsonAsync<List<VehicleCostPlanItemDto>>($"/api/vehicles/{ids.Vehicle}/cost-plan"))!;
        var paid = plans.Single(i => i.Id == ids.Transport);
        Assert.Equal(670, paid.ActualPaid); Assert.Equal(0, paid.RemainingProjected); Assert.False(paid.IsCancelled);
        Assert.Equal(3, plans.Count);
        var history = (await client.GetFromJsonAsync<List<VehicleExpenseDto>>($"/api/vehicles/{ids.Vehicle}/expenses"))!;
        Assert.Equal(expense.Id, Assert.Single(history).Id);
        var after = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!;
        Assert.Equal(6970, after.TotalInvested); Assert.Equal(600, after.RemainingProjectedCosts);
        Assert.Equal(before.ProjectedFinalCost, after.ProjectedFinalCost);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(before.OriginalForecast), System.Text.Json.JsonSerializer.Serialize(after.OriginalForecast));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(url + "/confirm-payment", request)).StatusCode);
        Assert.Equal(4330, await Balance(factory, ids.Account));
        using var verify = factory.Services.CreateScope(); var context = verify.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var movement = await context.FinancialTransactions.SingleAsync(t => t.Id == expense.FinancialTransactionId);
        Assert.Equal(TransactionType.VehicleExpense, movement.Type); Assert.Equal(TransactionDirection.Out, movement.Direction);
        Assert.Equal(670, movement.Amount); Assert.Equal(ids.Account, movement.CapitalAccountId); Assert.Equal(ids.Vehicle, movement.VehicleId);
        Assert.Single(await context.VehicleExpenses.ToListAsync());
    }

    [Theory]
    [InlineData("balance", "insufficientCapital")]
    [InlineData("missingPurchase", "purchaseAccountMissing")]
    [InlineData("ambiguousAccount", "purchaseAccountMissing")]
    [InlineData("cancelled", "cancelledPlan")]
    [InlineData("zero", "invalidPlanAmount")]
    [InlineData("partial", "partialPayment")]
    [InlineData("wrongPlan", "planMismatch")]
    [InlineData("changedAmount", "paymentChanged")]
    [InlineData("changedAccount", "paymentChanged")]
    [InlineData("sold", "sold")]
    [InlineData("inactive", "inactive")]
    public async Task RejectedConfirmationDoesNotWrite(string scenario, string code)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        var request = new ConfirmVehiclePaymentRequest { ExpectedAmount = 1000, ExpectedCapitalAccountId = ids.Account };
        if (scenario == "partial") await SeedHistoricalExpense(factory, ids, 200, ids.Transport);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        if (scenario == "balance") (await db.FinancialTransactions.SingleAsync(t => t.Type == TransactionType.CapitalContribution)).Amount = 6400;
        if (scenario == "missingPurchase") db.FinancialTransactions.Remove(await db.FinancialTransactions.SingleAsync(t => t.Type == TransactionType.VehiclePurchase));
        if (scenario == "ambiguousAccount") db.FinancialTransactions.Add(new FinancialTransaction { VehicleId = ids.Vehicle,
            CapitalAccount = new CapitalAccount { Name = "Other", Currency = "EUR", IsActive = true },
            Type = TransactionType.VehiclePurchase, Direction = TransactionDirection.Out, Amount = 1, Description = "Other purchase" });
        if (scenario == "cancelled") (await db.VehicleCostPlanItems.SingleAsync(i => i.Id == ids.Transport)).IsCancelled = true;
        if (scenario == "zero") (await db.VehicleCostPlanItems.SingleAsync(i => i.Id == ids.Transport)).CurrentEstimatedAmount = 0;
        if (scenario == "wrongPlan")
        {
            var other = new DealerManager.Domain.Entities.Vehicle { Make = "Other", Model = "Car" };
            db.Vehicles.Add(other); await db.SaveChangesAsync();
            (await db.VehicleCostPlanItems.SingleAsync(i => i.Id == ids.Transport)).VehicleId = other.Id;
        }
        if (scenario == "changedAmount") request.ExpectedAmount = 670;
        if (scenario == "changedAccount") request.ExpectedCapitalAccountId = 999;
        if (scenario == "sold") (await db.Vehicles.SingleAsync()).Status = VehicleStatus.Sold;
        if (scenario == "inactive") (await db.CapitalAccounts.SingleAsync()).IsActive = false;
        await db.SaveChangesAsync();
        var transactionCount = await db.FinancialTransactions.CountAsync(); var expenseCount = await db.VehicleExpenses.CountAsync();
        var balance = await Balance(factory, ids.Account);
        var response = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/cost-plan/{ids.Transport}/confirm-payment", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains($"\"code\":\"{code}\"", await response.Content.ReadAsStringAsync());
        Assert.Equal(transactionCount, await db.FinancialTransactions.CountAsync()); Assert.Equal(expenseCount, await db.VehicleExpenses.CountAsync());
        Assert.Equal(balance, await Balance(factory, ids.Account));
    }

    [Fact]
    public async Task CommitmentIsCurrentTargetAndUnexpectedExpenseUsesPurchaseAccountWithoutAnAccountInput()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        (await client.PutAsJsonAsync($"/api/vehicles/{ids.Vehicle}/cost-plan/{ids.Transport}", new { currentEstimatedAmount = 1000, committedAmount = 670 })).EnsureSuccessStatusCode();
        var preview = (await client.GetFromJsonAsync<VehiclePaymentPreviewDto>($"/api/vehicles/{ids.Vehicle}/cost-plan/{ids.Transport}/payment-preview"))!;
        Assert.Equal(670, preview.Amount);
        (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/cost-plan/{ids.Transport}/confirm-payment", new ConfirmVehiclePaymentRequest { ExpectedAmount = 670, ExpectedCapitalAccountId = ids.Account })).EnsureSuccessStatusCode();
        var response = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/expenses", new { category = CostCategory.Parts, description = "Battery", amount = 180 });
        response.EnsureSuccessStatusCode(); var expense = (await response.Content.ReadFromJsonAsync<VehicleExpenseDto>())!;
        Assert.Null(expense.CostPlanItemId); Assert.Equal(ids.Account, expense.CapitalAccountId); Assert.Equal(12850, await Balance(factory, ids.Account));
    }
}
