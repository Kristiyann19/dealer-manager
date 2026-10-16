using System.Net;
using System.Net.Http.Json;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.Dtos.Dashboard;
using DealerManager.Application.IRepository;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using DealerManager.Infrastructure.Service.Vehicle;
using DealerManager.Tests.Finance;
using DealerManager.Tests.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DealerManager.Tests.VehicleFinance;
public partial class VehicleFinanceTests
{
    private static async Task<VehicleExpenseDto> PayTransport(HttpClient client, SeedData ids) {
        var response = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/cost-plan/{ids.Transport}/confirm-payment",
            new ConfirmVehiclePaymentRequest { ExpectedAmount = 1000, ExpectedCapitalAccountId = ids.Account });
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<VehicleExpenseDto>())!;
    }
    private static CorrectVehicleExpenseRequest Correction(decimal amount = 800) => new() {
        Category = CostCategory.Transport, Description = "Correct transport", Amount = amount,
        Supplier = "Updated supplier", DocumentNumber = "INV-2", Reason = "Wrong amount"
    };

    [Fact]
    public async Task ReverseExpenseRestoresCapitalAndPlanKeepsAuditAndRejectsRepeat()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        var expense = await PayTransport(client, ids);
        var url = $"/api/vehicles/{ids.Vehicle}/expenses/{expense.Id}/reverse";
        var response = await client.PostAsJsonAsync(url, new { reason = " Duplicate payment " }); response.EnsureSuccessStatusCode();
        var reversed = (await response.Content.ReadFromJsonAsync<VehicleExpenseDto>())!;
        Assert.True(reversed.IsReversed); Assert.Equal(1000, reversed.Amount); Assert.Equal(expense.FinancialTransactionId, reversed.FinancialTransactionId);
        Assert.Equal("Duplicate payment", reversed.ReversalReason); Assert.Equal(1, reversed.ReversedByUserId);
        Assert.Equal(factory.Clock.Now, reversed.ReversedAt); Assert.Null(reversed.ReplacementExpenseId);
        Assert.Equal(13700, await Balance(factory, ids.Account));
        var details = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!;
        Assert.Equal(6300, details.TotalInvested); Assert.Equal(1600, details.RemainingProjectedCosts);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(url, new { reason = "Again" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/expenses/{expense.Id}/correct", Correction())).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var movement = await db.FinancialTransactions.SingleAsync(t => t.Id == reversed.ReversalTransactionId);
        Assert.Equal(TransactionDirection.In, movement.Direction); Assert.Equal(TransactionType.VehicleExpense, movement.Type);
        Assert.Equal(ids.Account, movement.CapitalAccountId); Assert.Equal(1000, movement.Amount);
        Assert.Single(await db.VehicleExpenses.ToListAsync()); Assert.Equal(4, await db.FinancialTransactions.CountAsync());
    }

    [Theory]
    [InlineData(800)] [InlineData(1200)]
    public async Task CorrectionIsAppendOnlyAndAllFinancialSummariesUseOnlyEffectiveExpenses(int amount)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        var expense = await PayTransport(client, ids);
        var response = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/expenses/{expense.Id}/correct", Correction(amount)); response.EnsureSuccessStatusCode();
        var original = (await response.Content.ReadFromJsonAsync<VehicleExpenseDto>())!;
        Assert.True(original.IsReversed); Assert.NotNull(original.ReplacementExpenseId); Assert.Equal(1000, original.Amount);
        var history = (await client.GetFromJsonAsync<List<VehicleExpenseDto>>($"/api/vehicles/{ids.Vehicle}/expenses"))!;
        Assert.Equal(2, history.Count); var replacement = history.Single(e => !e.IsReversed);
        Assert.Equal(original.ReplacementExpenseId, replacement.Id); Assert.Equal(amount, replacement.Amount);
        Assert.Equal(ids.Transport, replacement.CostPlanItemId); Assert.Equal("Updated supplier", replacement.Supplier);
        Assert.Equal("INV-2", replacement.DocumentNumber); Assert.Equal(13700 - amount, await Balance(factory, ids.Account));
        var details = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!;
        Assert.Equal(6300 + amount, details.TotalInvested); Assert.Equal(amount, details.ActualExpenses);
        Assert.Equal(600 + Math.Max(0,1000 - amount), details.RemainingProjectedCosts);
        var dashboard = (await client.GetFromJsonAsync<DashboardDto>("/api/dashboard"))!;
        Assert.Equal(6300 + amount, dashboard.Financial.CapitalInvested);
        Assert.Equal(6300 + amount, dashboard.MonthlyFinancials.Last().CapitalInvested);
        Assert.Equal(13700 - amount, dashboard.Financial.AvailableCash);
        if (amount < 1000) {
            var preview = (await client.GetFromJsonAsync<VehiclePaymentPreviewDto>($"/api/vehicles/{ids.Vehicle}/cost-plan/{ids.Transport}/payment-preview"))!;
            Assert.Equal(200, preview.Amount);
            var payment = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/cost-plan/{ids.Transport}/confirm-payment",
                new { expectedAmount = 200, expectedCapitalAccountId = ids.Account }); payment.EnsureSuccessStatusCode();
            Assert.Equal(12700, await Balance(factory, ids.Account));
        }
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public async Task CorrectionFailureAtAnySaveRollsBackEntireLedgerAndAudit(int failAt)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        var expense = await PayTransport(client, ids);
        using var scope = factory.Services.CreateScope(); var services = scope.ServiceProvider;
        var service = ActivatorUtilities.CreateInstance<VehicleService>(services, new FailOnSave(services.GetRequiredService<IUnitOfWork>(), failAt));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CorrectExpense(ids.Vehicle, expense.Id, Correction(), default));
        var db = services.GetRequiredService<DealerManagerDbContext>();
        Assert.False((await db.VehicleExpenses.SingleAsync()).IsReversed); Assert.Equal(3, await db.FinancialTransactions.CountAsync());
        Assert.Equal(12700, await Balance(factory, ids.Account));
    }

    [Fact]
    public async Task InvalidReasonsAmountsOrInsufficientFundsDoNotChangeMoney()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        var expense = await PayTransport(client, ids); var url = $"/api/vehicles/{ids.Vehicle}/expenses/{expense.Id}";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(url + "/reverse", new { reason = "   " })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(url + "/correct", Correction(0))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(url + "/correct", Correction(20000))).StatusCode);
        Assert.Equal(12700, await Balance(factory, ids.Account));
        var history = (await client.GetFromJsonAsync<List<VehicleExpenseDto>>($"/api/vehicles/{ids.Vehicle}/expenses"))!;
        Assert.False(Assert.Single(history).IsReversed);
    }

    [Fact]
    public async Task CorrectionOfSoldVehicleRecalculatesRealizedProfitAndRetainsSale()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        var expense = await PayTransport(client, ids);
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
            (await db.Vehicles.SingleAsync()).Status = VehicleStatus.Sold;
            db.VehicleSales.Add(new() { VehicleId = ids.Vehicle, SalePrice = 10000, SoldAt = factory.Clock.Now,
                FinancialTransaction = new() { Amount = 10000, CapitalAccountId = ids.Account, VehicleId = ids.Vehicle,
                    Type = TransactionType.VehicleSale, Direction = TransactionDirection.In, OccurredAt = factory.Clock.Now } });
            await db.SaveChangesAsync();
        }
        (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/expenses/{expense.Id}/correct", Correction())).EnsureSuccessStatusCode();
        var details = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!;
        Assert.Equal(2900, details.RealizedProfit); Assert.Equal(10000, details.ActualSalePrice);
        var dash = (await client.GetFromJsonAsync<DashboardDto>("/api/dashboard"))!;
        Assert.Equal(2900, dash.Monthly.ProfitThisMonth); Assert.Equal(0, dash.Financial.CapitalInvested);
    }

    [Fact]
    public async Task OtherTenantCannotReadOrCorrectAnExpense()
    {
        using var factory = new AuthApiFactory(); var (a, ownerA) = await factory.Register("correction-a@example.test", "A"); using var ca = a;
        var (b, ownerB) = await factory.Register("correction-b@example.test", "B"); using var cb = b;
        int carId, expenseId;
        using (var db = factory.TenantDb(ownerB.Dealership.Id)) {
            var car = new DealerManager.Domain.Entities.Vehicle { Make = "B", Model = "Private" };
            var account = new CapitalAccount { Name = "B", Currency = "EUR", IsActive = true };
            db.AddRange(car, account); await db.SaveChangesAsync();
            var transaction = new FinancialTransaction { Amount = 100, CapitalAccountId = account.Id, VehicleId = car.Id,
                Type = TransactionType.VehicleExpense, Direction = TransactionDirection.Out };
            db.Add(transaction); await db.SaveChangesAsync();
            var expense = new VehicleExpense { VehicleId = car.Id, FinancialTransactionId = transaction.Id, Amount = 100 };
            db.Add(expense); await db.SaveChangesAsync(); carId = car.Id; expenseId = expense.Id;
        }
        foreach (var suffix in new[] { "/reverse", "/correct" }) {
            var response = suffix == "/reverse" ? await a.PostAsJsonAsync($"/api/vehicles/{carId}/expenses/{expenseId}" + suffix, new { reason = "Attack" })
                : await a.PostAsJsonAsync($"/api/vehicles/{carId}/expenses/{expenseId}" + suffix, Correction());
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        using var verify = factory.TenantDb(ownerB.Dealership.Id);
        Assert.False((await verify.VehicleExpenses.SingleAsync()).IsReversed); Assert.Single(await verify.FinancialTransactions.ToListAsync());
    }
}
