using System.Net;
using System.Net.Http.Json;
using DealerManager.Application.Dtos.Candidate;
using DealerManager.Application.Dtos.Finance;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.FilterDtos;
using DealerManager.Application.FilterDtos.Candidate;
using DealerManager.Application.IRepository;
using DealerManager.Application.IService.Candidate;
using DealerManager.Application.IService.Finance;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using DealerManager.Infrastructure.Repository;
using DealerManager.Infrastructure.Service.Candidate;
using DealerManager.Tests.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CandidateEntity = DealerManager.Domain.Entities.Candidate;

namespace DealerManager.Tests.Candidate;

public class PurchaseCandidateTests
{
    private static async Task<(int candidate, int account)> Seed(HttpClient client)
    {
        var accountResponse = await client.PostAsJsonAsync("/api/capital-accounts", new { name = "Main", currency = "EUR" });
        accountResponse.EnsureSuccessStatusCode();
        var account = (await accountResponse.Content.ReadFromJsonAsync<CapitalAccountDto>())!;
        (await client.PostAsJsonAsync($"/api/capital-accounts/{account.Id}/contributions", new { amount = 20000, description = "Initial capital" })).EnsureSuccessStatusCode();
        var response = await client.PostAsJsonAsync("/api/candidates", new { make = "BMW", model = "320d", year = 2018, mileage = 123456, askingPrice = 6500 });
        response.EnsureSuccessStatusCode();
        var candidate = (await response.Content.ReadFromJsonAsync<CandidateDetailsDto>())!;
        for (var version = 1; version <= 2; version++)
            (await client.PostAsJsonAsync("/api/candidates/estimates", new { candidateId = candidate.Id, expectedSellingPrice = 10000, items = new[] {
                new { category = 0, description = "Purchase", estimatedAmount = 6500 },
                new { category = 1, description = "Transport", estimatedAmount = 1000 },
                new { category = 2, description = "Repair", estimatedAmount = 500 },
                new { category = 6, description = "Detailing", estimatedAmount = 100 }
            }})).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/candidates/{candidate.Id}/approve", null)).EnsureSuccessStatusCode();
        return (candidate.Id, account.Id);
    }

    [Fact]
    public async Task FullHttpFlowPreservesForecastCreatesPlanAndMovesActualCapital()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient();
        var ids = await Seed(client);
        var date = new DateTimeOffset(2026, 9, 25, 0, 0, 0, TimeSpan.FromHours(3));
        var response = await client.PostAsJsonAsync($"/api/candidates/{ids.candidate}/purchase", new { capitalAccountId = ids.account, actualPurchasePrice = 6300, purchaseDate = date, vehicleStatus = 9, transactionDirection = 0 });
        response.EnsureSuccessStatusCode();
        var purchase = (await response.Content.ReadFromJsonAsync<PurchaseCandidateResultDto>())!;
        Assert.Equal(20000, purchase.PreviousBalance); Assert.Equal(13700, purchase.RemainingBalance);
        Assert.Equal(date.ToUniversalTime(), purchase.PurchaseDate);
        var candidate = (await client.GetFromJsonAsync<CandidateDetailsDto>($"/api/candidates/{ids.candidate}"))!;
        Assert.Equal(CandidateStatus.Purchased, candidate.Status); Assert.Equal(purchase.VehicleId, candidate.VehicleId);
        Assert.Equal(6500, candidate.AskingPrice); Assert.Equal(date.ToUniversalTime(), candidate.PurchasedAt);
        Assert.Equal(2, candidate.EstimateHistory.Count);
        Assert.True(candidate.LatestEstimate!.IsDecisionSnapshot); Assert.False(candidate.EstimateHistory.Single(e => e.Version == 1).IsDecisionSnapshot);
        Assert.All(candidate.EstimateHistory, e => Assert.Equal(6500, e.Items.Single(i => i.Category == CostCategory.Purchase).EstimatedAmount));
        var vehicle = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{purchase.VehicleId}"))!;
        Assert.Equal("BMW", vehicle.Make); Assert.Equal(2018, vehicle.Year); Assert.Equal(VehicleStatus.Purchased, vehicle.Status); Assert.Equal(6300, vehicle.ActualPurchasePrice);
        var account = (await client.GetFromJsonAsync<CapitalAccountDetailsDto>($"/api/capital-accounts/{ids.account}"))!;
        Assert.Equal(13700, account.CurrentBalance);
        var movement = Assert.Single(account.LatestTransactions, t => t.Type == TransactionType.VehiclePurchase);
        Assert.Equal(TransactionDirection.Out, movement.Direction); Assert.Equal(6300, movement.Amount); Assert.Equal(vehicle.Id, movement.VehicleId);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var plan = await db.VehicleCostPlanItems.ToListAsync(); Assert.Equal(3, plan.Count); Assert.Equal(1600, plan.Sum(i => i.CurrentEstimatedAmount));
        Assert.All(plan, i => { Assert.NotEqual(CostCategory.Purchase, i.Category); Assert.Null(i.CommittedAmount); Assert.False(i.IsCancelled); });
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/candidates/{ids.candidate}/purchase", new { capitalAccountId = ids.account, actualPurchasePrice = 6300 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/candidates/{ids.candidate}/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/candidates/{ids.candidate}/reject", new { })).StatusCode);
    }

    [Theory]
    [InlineData("funds", "insufficientCapital")]
    [InlineData("year", "year")]
    [InlineData("currency", "currency")]
    [InlineData("inactive", "inactive")]
    [InlineData("review", "status")]
    [InlineData("rejected", "status")]
    [InlineData("snapshot", "snapshot")]
    [InlineData("vehicle", "alreadyPurchased")]
    [InlineData("estimate", "estimate")]
    public async Task RefusesInvalidPurchasesWithoutSideEffects(string scenario, string code)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(client);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
            var candidate = await db.Candidates.Include(c => c.CandidateEstimates).ThenInclude(e => e.CandidateEstimateItems).SingleAsync();
            var account = await db.CapitalAccounts.SingleAsync();
            if (scenario == "year") candidate.Year = null;
            if (scenario == "currency") account.Currency = "USD";
            if (scenario == "inactive") account.IsActive = false;
            if (scenario == "review") candidate.Status = CandidateStatus.UnderReview;
            if (scenario == "rejected") candidate.Status = CandidateStatus.Rejected;
            if (scenario == "snapshot") candidate.CandidateEstimates.First().IsDecisionSnapshot = true;
            if (scenario == "vehicle") db.Vehicles.Add(new Vehicle { SourceCandidateId = candidate.Id, Make = "BMW", Model = "320d", Year = 2018 });
            if (scenario == "estimate") { db.CandidateEstimateItems.RemoveRange(candidate.CandidateEstimates.SelectMany(e => e.CandidateEstimateItems)); db.CandidateEstimates.RemoveRange(candidate.CandidateEstimates); }
            await db.SaveChangesAsync();
        }
        var response = await client.PostAsJsonAsync($"/api/candidates/{ids.candidate}/purchase", new { capitalAccountId = ids.account, actualPurchasePrice = scenario == "funds" ? 25000 : 6300 });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains($"\"code\":\"{code}\"", await response.Content.ReadAsStringAsync());
        using var verification = factory.Services.CreateScope(); var context = verification.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        Assert.False(await context.Candidates.AnyAsync(c => c.Status == CandidateStatus.Purchased));
        Assert.Empty(await context.VehicleCostPlanItems.ToListAsync());
        Assert.Single(await context.FinancialTransactions.ToListAsync());
        Assert.Equal(scenario == "vehicle" ? 1 : 0, await context.Vehicles.CountAsync());
    }

    [Theory]
    [InlineData(0, 1, 400)]
    [InlineData(-1, 1, 400)]
    [InlineData(1, 0, 400)]
    [InlineData(1, 999, 404)]
    public async Task ValidatesRequest(decimal price, int accountId, int status)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(client);
        Assert.Equal(status, (int)(await client.PostAsJsonAsync($"/api/candidates/{ids.candidate}/purchase", new { capitalAccountId = accountId, actualPurchasePrice = price })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/candidates/999/purchase", new { capitalAccountId = ids.account, actualPurchasePrice = 1 })).StatusCode);
    }

    [Fact]
    public async Task FailureAfterSavingPaymentRollsBackEntirePurchase()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(client);
        using var scope = factory.Services.CreateScope(); var services = scope.ServiceProvider;
        var failing = new FailAfterPayment(services.GetRequiredService<IUnitOfWork>());
        var service = new PurchaseCandidateService(
            services.GetRequiredService<IBaseRepository<CandidateEntity, CandidateFilterDto, DealerManagerDbContext>>(),
            services.GetRequiredService<IBaseRepository<Vehicle, FilterDto<Vehicle>, DealerManagerDbContext>>(),
            services.GetRequiredService<IBaseRepository<CapitalAccount, FilterDto<CapitalAccount>, DealerManagerDbContext>>(),
            services.GetRequiredService<IBaseRepository<FinancialTransaction, FilterDto<FinancialTransaction>, DealerManagerDbContext>>(),
            services.GetRequiredService<ICapitalAccountService>(), failing, factory.Clock);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PurchaseCandidate(ids.candidate, new() { CapitalAccountId = ids.account, ActualPurchasePrice = 6300 }, default));
        var db = services.GetRequiredService<DealerManagerDbContext>();
        Assert.Equal(CandidateStatus.Approved, (await db.Candidates.SingleAsync()).Status);
        Assert.Empty(await db.Vehicles.ToListAsync()); Assert.Empty(await db.VehicleCostPlanItems.ToListAsync());
        Assert.False(await db.CandidateEstimates.AnyAsync(e => e.IsDecisionSnapshot));
        Assert.Single(await db.FinancialTransactions.ToListAsync());
        Assert.Equal(20000, await services.GetRequiredService<ICapitalAccountService>().GetBalance(ids.account, default));
    }
    private sealed class FailAfterPayment(IUnitOfWork inner) : IUnitOfWork
    {
        private int saves;
        public async Task<int> SaveChanges(CancellationToken token)
        {
            var result = await inner.SaveChanges(token);
            if (++saves == 2) throw new InvalidOperationException("Simulated failure after payment save");
            return result;
        }
        public Task<T> ExecuteInTransaction<T>(Func<CancellationToken, Task<T>> action, CancellationToken token) => inner.ExecuteInTransaction(action, token);
    }
}
