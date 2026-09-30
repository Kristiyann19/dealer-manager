using System.Net;
using System.Net.Http.Json;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.IRepository;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using DealerManager.Infrastructure.Service.Vehicle;
using DealerManager.Tests.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DealerManager.Tests.VehicleFinance;
public partial class VehicleFinanceTests
{
    private static async Task<SeedData> SeedListed(FinanceApiFactory factory, HttpClient client)
    {
        var ids = await Seed(factory);
        await SeedHistoricalExpense(factory, ids, 169, ids.Repair);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        (await db.Vehicles.SingleAsync()).Status = VehicleStatus.ReadyForSale;
        (await db.CandidateEstimates.SingleAsync()).ExpectedSellingPrice = 6700;
        (await db.FinancialTransactions.SingleAsync(t => t.Type == TransactionType.CapitalContribution)).Amount = 13550;
        await db.SaveChangesAsync();
        (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/listing", new { listingPrice = 7000 })).EnsureSuccessStatusCode();
        return ids;
    }

    [Theory]
    [InlineData(false, 6800, 331)] [InlineData(true, 6800, 331)] [InlineData(false, 6169, -300)]
    public async Task SaleCreditsOnlyActualPriceAndPreservesListingAndForecast(bool dateProvided, int price, int profit)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await SeedListed(factory, client);
        Assert.Equal(7081, await Balance(factory, ids.Account));
        var date = new DateTimeOffset(2026, 9, 30, 15, 0, 0, TimeSpan.FromHours(3));
        var response = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/sale", new SellVehicleRequest { ActualSalePrice = price, SoldAt = dateProvided ? date : null });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<VehicleSaleResultDto>())!;
        Assert.Equal(ids.Vehicle, result.VehicleId); Assert.Equal(ids.Account, result.CapitalAccountId);
        Assert.Equal(price, result.ActualSalePrice); Assert.Equal(6469, result.TotalInvested);
        Assert.Equal(profit, result.RealizedProfit); Assert.Equal(profit / 6469m * 100m, result.RealizedROI);
        Assert.Equal(7081 + price, result.NewCapitalBalance); Assert.Equal(VehicleStatus.Sold, result.Status);
        Assert.Equal(dateProvided ? date.ToUniversalTime() : factory.Clock.Now, result.SoldAt); Assert.Equal(TimeSpan.Zero, result.SoldAt.Offset);
        Assert.Equal(7081 + price, await Balance(factory, ids.Account));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var sale = await db.VehicleSales.SingleAsync(); Assert.Equal(result.SaleId, sale.Id); Assert.Null(sale.CustomerId); Assert.Null(sale.Notes);
        var movement = await db.FinancialTransactions.SingleAsync(t => t.Id == sale.FinancialTransactionId);
        Assert.Equal(TransactionType.VehicleSale, movement.Type); Assert.Equal(TransactionDirection.In, movement.Direction);
        Assert.Equal(price, movement.Amount); Assert.Equal(ids.Vehicle, movement.VehicleId); Assert.Equal(ids.Account, movement.CapitalAccountId);
        Assert.Equal("Sale of BMW 320d", movement.Description); Assert.Equal(result.SoldAt, movement.OccurredAt); Assert.Equal(factory.Clock.Now, movement.CreatedAt);
        Assert.Equal(4, await db.FinancialTransactions.CountAsync());
        Assert.False((await db.VehicleListings.SingleAsync()).IsActive); Assert.Equal(7000, (await db.VehicleListings.SingleAsync()).ListingPrice);
        Assert.Equal(VehicleStatus.Sold, (await db.Vehicles.SingleAsync()).Status);
        var history = await db.VehicleStatusHistories.SingleAsync(h => h.ToStatus == VehicleStatus.Sold);
        Assert.Equal(VehicleStatus.Listed, history.FromStatus); Assert.Equal(result.SoldAt, history.ChangedAt);
        var details = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!;
        Assert.Null(details.CurrentListing); Assert.Equal(7000, details.ListingPrice); Assert.NotNull(details.ListedAt);
        Assert.Equal(6700, details.ExpectedSellingPrice); Assert.Equal(price, details.ActualSalePrice); Assert.Equal(profit, details.RealizedProfit);
        Assert.Equal(result.RealizedROI, details.RealizedROI); Assert.Equal(result.NewCapitalBalance, details.SaleAccount!.CurrentBalance);
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync($"/api/vehicles/{ids.Vehicle}/listing")).StatusCode);
        foreach (var url in new[] { "/api/vehicles", "/api/vehicles?Status=Sold" })
        {
            var inventory = (await client.GetFromJsonAsync<VehicleListResultDto>(url))!;
            var item = Assert.Single(inventory.Items); Assert.Equal(price, item.ActualSalePrice); Assert.Equal(profit, item.RealizedProfit); Assert.Equal(7000, item.ListingPrice);
        }
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/sale", new { actualSalePrice = price })).StatusCode);
        Assert.Equal(4, await db.FinancialTransactions.CountAsync()); Assert.Single(await db.VehicleSales.ToListAsync());
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(8)] [InlineData(9)]
    public async Task SaleRejectsNonListedStatus(int status)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await SeedListed(factory, client);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        (await db.Vehicles.SingleAsync()).Status = (VehicleStatus)status; await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/sale", new { actualSalePrice = 6800 })).StatusCode);
        Assert.Empty(await db.VehicleSales.ToListAsync()); Assert.Equal(3, await db.FinancialTransactions.CountAsync()); Assert.True((await db.VehicleListings.SingleAsync()).IsActive);
    }

    [Theory]
    [InlineData("{}")] [InlineData("{\"actualSalePrice\":0}")] [InlineData("{\"actualSalePrice\":-1}")]
    [InlineData("{\"actualSalePrice\":6800,\"capitalAccountId\":0}")]
    [InlineData("{\"actualSalePrice\":6800,\"soldAt\":\"invalid\"}")]
    public async Task SaleValidatesRequestWithoutWrites(string body)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await SeedListed(factory, client);
        var response = await client.PostAsync($"/api/vehicles/{ids.Vehicle}/sale", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(7081, await Balance(factory, ids.Account));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        Assert.Empty(await db.VehicleSales.ToListAsync()); Assert.Equal(VehicleStatus.Listed, (await db.Vehicles.SingleAsync()).Status);
    }

    [Theory]
    [InlineData("listing")] [InlineData("missingAccount")] [InlineData("inactive")] [InlineData("currency")] [InlineData("purchaseMissing")]
    public async Task SaleRejectsMissingListingAndInvalidAccounts(string problem)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await SeedListed(factory, client);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        if (problem == "listing") (await db.VehicleListings.SingleAsync()).IsActive = false;
        if (problem == "inactive") (await db.CapitalAccounts.SingleAsync()).IsActive = false;
        if (problem == "currency") (await db.CapitalAccounts.SingleAsync()).Currency = "BGN";
        if (problem == "purchaseMissing") (await db.FinancialTransactions.SingleAsync(t => t.Type == TransactionType.VehiclePurchase)).VehicleId = null;
        await db.SaveChangesAsync();
        var response = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/sale", new SellVehicleRequest { ActualSalePrice = 6800, CapitalAccountId = problem == "missingAccount" ? 999 : null });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); Assert.Empty(await db.VehicleSales.ToListAsync()); Assert.Equal(3, await db.FinancialTransactions.CountAsync());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SaleSupportsExplicitAccountAndZeroInvestment(bool zeroInvestment)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await SeedListed(factory, client);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var account = new CapitalAccount { Name = "Sale account", Currency = "EUR", IsActive = true }; db.CapitalAccounts.Add(account);
        if (zeroInvestment)
        {
            db.VehicleExpenses.RemoveRange(await db.VehicleExpenses.ToListAsync());
            db.FinancialTransactions.RemoveRange(await db.FinancialTransactions.Where(t => t.Type != TransactionType.CapitalContribution).ToListAsync());
        }
        await db.SaveChangesAsync();
        var before = await Balance(factory, ids.Account);
        var response = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/sale", new { actualSalePrice = 6800, capitalAccountId = account.Id });
        response.EnsureSuccessStatusCode(); var sale = (await response.Content.ReadFromJsonAsync<VehicleSaleResultDto>())!;
        Assert.Equal(account.Id, sale.CapitalAccountId); Assert.Equal(6800, sale.NewCapitalBalance); Assert.Equal(before, await Balance(factory, ids.Account));
        if (zeroInvestment) { Assert.Equal(0, sale.TotalInvested); Assert.Equal(0, sale.RealizedROI); Assert.Equal(6800, sale.RealizedProfit); }
    }

    [Theory]
    [InlineData(1)] [InlineData(2)]
    public async Task SaleRollsBackBothSaves(int failureAt)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await SeedListed(factory, client);
        using var scope = factory.Services.CreateScope(); var services = scope.ServiceProvider;
        var service = ActivatorUtilities.CreateInstance<VehicleService>(services, new FailOnSave(services.GetRequiredService<IUnitOfWork>(), failureAt));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SellVehicle(ids.Vehicle, new() { ActualSalePrice = 6800 }, default));
        var db = services.GetRequiredService<DealerManagerDbContext>();
        Assert.Empty(await db.VehicleSales.ToListAsync()); Assert.Equal(3, await db.FinancialTransactions.CountAsync());
        Assert.True((await db.VehicleListings.SingleAsync()).IsActive); Assert.Equal(VehicleStatus.Listed, (await db.Vehicles.SingleAsync()).Status);
        Assert.Single(await db.VehicleStatusHistories.ToListAsync()); Assert.Equal(7081, await Balance(factory, ids.Account));
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/vehicles/999/sale", new { actualSalePrice = 6800 })).StatusCode);
    }
}
