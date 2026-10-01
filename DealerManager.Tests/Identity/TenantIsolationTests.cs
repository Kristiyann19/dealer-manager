using System.Net;
using System.Net.Http.Json;
using DealerManager.Application.Dtos.Candidate;
using DealerManager.Application.Dtos.Dashboard;
using DealerManager.Application.Dtos.Finance;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.IService.Identity;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace DealerManager.Tests.Identity;

public class TenantIsolationTests
{
    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body); response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
    private static async Task<CandidateDetailsDto> Candidate(HttpClient client, string make)
    {
        var c = await Post<CandidateDetailsDto>(client, "/api/candidates", new { make, model = "Test", year = 2020, askingPrice = 3000 });
        await Post<CandidateEstimateDto>(client, "/api/candidates/estimates", new { candidateId = c.Id, expectedSellingPrice = 8000,
            items = new[] { new { category = 0, estimatedAmount = 3000 }, new { category = 2, estimatedAmount = 500 } } });
        return await Post<CandidateDetailsDto>(client, $"/api/candidates/{c.Id}/approve", new { });
    }
    [Fact]
    public async Task TwoOwnersCannotReadOrMutateEachOthersBusinessDataOrUseForeignAccounts()
    {
        using var factory = new AuthApiFactory();
        var (a, userA) = await factory.Register("a@example.test", "Auto A"); using var ownedA = a;
        var (b, userB) = await factory.Register("b@example.test", "Auto B"); using var ownedB = b;
        var accountA = await Post<CapitalAccountDto>(a, "/api/capital-accounts", new { name = "A", currency = "EUR", dealershipId = userB.Dealership.Id });
        var accountB = await Post<CapitalAccountDto>(b, "/api/capital-accounts", new { name = "B", currency = "EUR" });
        await Post<FinancialTransactionDto>(a, $"/api/capital-accounts/{accountA.Id}/contributions", new { amount = 10000 });
        await Post<FinancialTransactionDto>(b, $"/api/capital-accounts/{accountB.Id}/contributions", new { amount = 20000 });
        var candidateA = await Candidate(a, "A"); var candidateB = await Candidate(b, "B");
        Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync($"/api/candidates/{candidateB.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync($"/api/candidates/{candidateA.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/candidates/{candidateA.Id}/purchase", new { capitalAccountId = accountB.Id, actualPurchasePrice = 3000 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/candidates/{candidateB.Id}/approve", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync("/api/candidates/estimates", new { candidateId = candidateB.Id, expectedSellingPrice = 1, items = new[] { new { category = 0, estimatedAmount = 1 } } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/candidates/{candidateB.Id}/reject", new { reason = "No" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/capital-accounts/{accountB.Id}/contributions", new { amount = 999 })).StatusCode);
        foreach (var path in new[] { $"/api/capital-accounts/{accountB.Id}", $"/api/capital-accounts/{accountB.Id}/transactions", $"/api/candidates/{candidateB.Id}/estimates" })
            Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync(path)).StatusCode);
        var boughtA = await Post<PurchaseCandidateResultDto>(a, $"/api/candidates/{candidateA.Id}/purchase", new { capitalAccountId = accountA.Id, actualPurchasePrice = 3000, dealershipId = userB.Dealership.Id });
        var boughtB = await Post<PurchaseCandidateResultDto>(b, $"/api/candidates/{candidateB.Id}/purchase", new { capitalAccountId = accountB.Id, actualPurchasePrice = 5000 });
        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync($"/api/vehicles/{boughtA.VehicleId}")).StatusCode);
        foreach (var suffix in new[] { "", "/financial-summary", "/cost-plan", "/expenses", "/status-history", "/listing", "/payment-account" })
            Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync($"/api/vehicles/{boughtB.VehicleId}{suffix}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/vehicles/{boughtB.VehicleId}/expenses", new { category = 2, description = "Attack", amount = 100 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/vehicles/{boughtB.VehicleId}/status", new { status = "ReadyForSale" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/vehicles/{boughtB.VehicleId}/listing", new { listingPrice = 9000 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/vehicles/{boughtB.VehicleId}/sale", new { actualSalePrice = 9000 })).StatusCode);
        var plansB = (await b.GetFromJsonAsync<List<VehicleCostPlanItemDto>>($"/api/vehicles/{boughtB.VehicleId}/cost-plan"))!;
        var planB = plansB.Single();
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/vehicles/{boughtB.VehicleId}/cost-plan/{planB.Id}/confirm-payment", new { expectedAmount = 500, expectedCapitalAccountId = accountB.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PutAsJsonAsync($"/api/vehicles/{boughtB.VehicleId}/cost-plan/{planB.Id}", new { currentEstimatedAmount = 1 })).StatusCode);
        Assert.False((await a.PostAsJsonAsync($"/api/vehicles/{boughtA.VehicleId}/expenses", new { capitalAccountId = accountB.Id, category = 2, description = "Attack", amount = 100 })).IsSuccessStatusCode);
        await Post<VehicleStatusHistoryDto>(a, $"/api/vehicles/{boughtA.VehicleId}/status", new { status = "ReadyForSale" });
        await Post<VehicleListingDto>(a, $"/api/vehicles/{boughtA.VehicleId}/listing", new { listingPrice = 9000 });
        Assert.False((await a.PostAsJsonAsync($"/api/vehicles/{boughtA.VehicleId}/sale", new { actualSalePrice = 9000, capitalAccountId = accountB.Id })).IsSuccessStatusCode);
        var dashA = (await a.GetFromJsonAsync<DashboardDto>("/api/dashboard"))!;
        var dashB = (await b.GetFromJsonAsync<DashboardDto>("/api/dashboard"))!;
        Assert.Equal(7000, dashA.Financial.AvailableCash); Assert.Equal(3000, dashA.Financial.CapitalInvested); Assert.Equal(10000, dashA.Financial.NetWorthAtCost);
        Assert.Equal(15000, dashB.Financial.AvailableCash); Assert.Equal(5000, dashB.Financial.CapitalInvested); Assert.Equal(20000, dashB.Financial.NetWorthAtCost);
        Assert.Equal(1, dashA.Monthly.CarsInStock); Assert.Equal(500, dashA.Financial.UpcomingProjectedCosts);
        Assert.Equal(boughtA.VehicleId, Assert.Single(dashA.ActiveVehicles).Id);
        Assert.Equal(boughtB.VehicleId, Assert.Single(dashB.ActiveVehicles).Id);
        Assert.Equal(1, (await a.GetFromJsonAsync<CandidateListResultDto>("/api/candidates"))!.TotalCount);
        Assert.Equal(accountA.Id, Assert.Single((await a.GetFromJsonAsync<List<CapitalAccountDto>>("/api/capital-accounts"))!).Id);
        Assert.Equal(boughtA.VehicleId, Assert.Single((await a.GetFromJsonAsync<VehicleListResultDto>("/api/vehicles"))!.Items).Id);
        using var dbA = factory.TenantDb(userA.Dealership.Id); using var dbB = factory.TenantDb(userB.Dealership.Id);
        Assert.All(await dbA.FinancialTransactions.ToListAsync(), t => Assert.Equal(userA.Dealership.Id, t.DealershipId));
        Assert.Equal(userA.Dealership.Id, (await dbA.CapitalAccounts.SingleAsync()).DealershipId);
        Assert.Equal(candidateA.Id, (await dbA.CandidateEstimates.SingleAsync()).CandidateId);
        Assert.Equal(candidateB.Id, (await dbB.CandidateEstimates.SingleAsync()).CandidateId);
        Assert.Empty(await dbA.VehicleSales.ToListAsync()); Assert.Empty(await dbA.VehicleExpenses.ToListAsync());
        // A real sale affects only A's current-month/dashboard totals and B cannot read its listing or sale details.
        await Post<VehicleSaleResultDto>(a, $"/api/vehicles/{boughtA.VehicleId}/sale", new { actualSalePrice = 9000 });
        dashA = (await a.GetFromJsonAsync<DashboardDto>("/api/dashboard"))!;
        dashB = (await b.GetFromJsonAsync<DashboardDto>("/api/dashboard"))!;
        Assert.Equal(6000, dashA.Monthly.ProfitThisMonth); Assert.Equal(1, dashA.Monthly.SoldThisMonth);
        Assert.Equal(0, dashB.Monthly.ProfitThisMonth); Assert.Equal(0, dashB.Monthly.SoldThisMonth);
        Assert.Empty(await dbB.VehicleSales.ToListAsync()); Assert.Empty(await dbB.VehicleListings.ToListAsync());
    }

    [Fact]
    public async Task SaveGuardBlocksDetachedWritesCrossTenantForeignKeysAndMissingTenant()
    {
        using var factory = new AuthApiFactory();
        var (a, userA) = await factory.Register("a@example.test", "A"); using var ownedA = a;
        var (b, userB) = await factory.Register("b@example.test", "B"); using var ownedB = b;
        var c = await Candidate(b, "B");
        var accountB = await Post<CapitalAccountDto>(b, "/api/capital-accounts", new { name = "B", currency = "EUR" });
        using (var db = factory.TenantDb(userA.Dealership.Id)) {
            db.Candidates.Update(new DealerManager.Domain.Entities.Candidate { Id = c.Id, DealershipId = userA.Dealership.Id, Make = "Attack" });
            await Assert.ThrowsAsync<TenantAccessException>(() => db.SaveChangesAsync());
        }
        using (var db = factory.TenantDb(userA.Dealership.Id)) {
            db.Candidates.Remove(new DealerManager.Domain.Entities.Candidate { Id = c.Id, DealershipId = userA.Dealership.Id });
            await Assert.ThrowsAsync<TenantAccessException>(() => db.SaveChangesAsync());
        }
        using (var db = factory.TenantDb(userA.Dealership.Id)) {
            db.CandidateEstimates.Add(new() { CandidateId = c.Id, ExpectedSellingPrice = 999 });
            await Assert.ThrowsAsync<TenantAccessException>(() => db.SaveChangesAsync());
        }
        using (var db = factory.TenantDb(userA.Dealership.Id)) {
            db.FinancialTransactions.Add(new() { CapitalAccountId = accountB.Id, Amount = 100 });
            await Assert.ThrowsAsync<TenantAccessException>(() => db.SaveChangesAsync());
        }
        using (var db = factory.TenantDb(null)) {
            Assert.Empty(await db.Candidates.ToListAsync()); Assert.Empty(await db.CapitalAccounts.ToListAsync());
            db.Candidates.Add(new() { Make = "No tenant" });
            await Assert.ThrowsAsync<TenantAccessException>(() => db.SaveChangesAsync());
        }
    }
}
