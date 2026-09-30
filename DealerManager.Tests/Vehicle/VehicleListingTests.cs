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
    private static async Task ReadyForListing(FinanceApiFactory factory, SeedData ids)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        (await db.Vehicles.SingleAsync(v => v.Id == ids.Vehicle)).Status = VehicleStatus.ReadyForSale;
        (await db.CandidateEstimates.SingleAsync()).ExpectedSellingPrice = 6000;
        (await db.FinancialTransactions.SingleAsync(t => t.Type == TransactionType.VehiclePurchase)).Amount = 5210;
        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ListingCreatesAtomicHistoryAndSeparatePriceWithoutChangingCapital(bool provideDate)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient();
        var ids = await Seed(factory); await ReadyForListing(factory, ids);
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync($"/api/vehicles/{ids.Vehicle}/listing")).StatusCode);
        var balance = await Balance(factory, ids.Account);
        var before = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!;
        Assert.Null(before.CurrentListing);
        var date = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.FromHours(3));
        var response = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/listing",
            new ListVehicleRequest { ListingPrice = 6200, ListedAt = provideDate ? date : null });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.EndsWith($"/api/vehicles/{ids.Vehicle}/listing", response.Headers.Location!.ToString());
        var result = (await response.Content.ReadFromJsonAsync<VehicleListingDto>())!;
        Assert.Equal(ids.Vehicle, result.VehicleId); Assert.True(result.ListingId > 0);
        Assert.Equal(6200, result.ListingPrice); Assert.Equal(VehicleStatus.Listed, result.Status);
        Assert.Equal(provideDate ? date.ToUniversalTime() : factory.Clock.Now, result.ListedAt);
        Assert.Equal(TimeSpan.Zero, result.ListedAt.Offset);
        var current = (await client.GetFromJsonAsync<VehicleListingDto>($"/api/vehicles/{ids.Vehicle}/listing"))!;
        Assert.Equal(result.ListingId, current.ListingId);
        var details = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!;
        Assert.Equal(VehicleStatus.Listed, details.Status); Assert.Equal(6200, details.CurrentListing!.ListingPrice);
        Assert.Equal(6000, details.ExpectedSellingPrice); Assert.Equal(6000, details.OriginalForecast!.OriginalExpectedSellingPrice);
        Assert.Equal(990, details.CurrentListing.ListingPrice - details.TotalInvested);
        Assert.Equal(before.ProjectedProfit, details.ProjectedProfit); Assert.Equal(before.TotalInvested, details.TotalInvested);
        var history = Assert.Single((await client.GetFromJsonAsync<List<VehicleStatusHistoryDto>>($"/api/vehicles/{ids.Vehicle}/status-history"))!);
        Assert.Equal(VehicleStatus.ReadyForSale, history.FromStatus); Assert.Equal(VehicleStatus.Listed, history.ToStatus);
        Assert.Equal(factory.Clock.Now, history.ChangedAt);
        var inventory = (await client.GetFromJsonAsync<VehicleListResultDto>("/api/vehicles?Status=Listed"))!;
        Assert.Equal(6200, Assert.Single(inventory.Items).ListingPrice); Assert.Equal(6000, inventory.Items[0].ExpectedSellingPrice);
        Assert.Equal(balance, await Balance(factory, ids.Account));
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var entity = await db.VehicleListings.SingleAsync(); Assert.True(entity.IsActive); Assert.Equal(factory.Clock.Now, entity.CreatedAt);
        Assert.Equal(2, await db.FinancialTransactions.CountAsync()); Assert.Empty(await db.VehicleExpenses.ToListAsync());
        Assert.Null((await db.VehicleStatusHistories.SingleAsync()).ChangedByUserId);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/listing", new { listingPrice = 6100 })).StatusCode);
        Assert.Single(await db.VehicleListings.ToListAsync()); Assert.Single(await db.VehicleStatusHistories.ToListAsync());
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    [InlineData(5)] [InlineData(7)] [InlineData(8)] [InlineData(9)]
    public async Task OnlyReadyForSaleCanBeListed(int status)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var vehicle = await db.Vehicles.SingleAsync(); vehicle.Status = (VehicleStatus)status; await db.SaveChangesAsync();
        var response = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/listing", new { listingPrice = 6200 });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("notReadyForListing", await response.Content.ReadAsStringAsync());
        Assert.Empty(await db.VehicleListings.ToListAsync()); Assert.Empty(await db.VehicleStatusHistories.ToListAsync());
        await db.Entry(vehicle).ReloadAsync(); Assert.Equal((VehicleStatus)status, vehicle.Status);
    }

    [Theory]
    [InlineData("{}")] [InlineData("{\"listingPrice\":0}")] [InlineData("{\"listingPrice\":-1}")]
    [InlineData("{\"listingPrice\":6200,\"listedAt\":\"invalid\"}")]
    public async Task InvalidListingIsRejected(string body)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        await ReadyForListing(factory, ids);
        var response = await client.PostAsync($"/api/vehicles/{ids.Vehicle}/listing", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        Assert.Empty(await db.VehicleListings.ToListAsync()); Assert.Empty(await db.VehicleStatusHistories.ToListAsync());
        Assert.Equal(VehicleStatus.ReadyForSale, (await db.Vehicles.SingleAsync()).Status);
    }

    [Fact]
    public async Task ActiveListingGuardAndDatabaseUniquenessAllowOnlyOneActiveListing()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        await ReadyForListing(factory, ids);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        db.VehicleListings.Add(new() { VehicleId = ids.Vehicle, ListingPrice = 5900, IsActive = false });
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync($"/api/vehicles/{ids.Vehicle}/listing")).StatusCode);
        (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/listing", new { listingPrice = 6200 })).EnsureSuccessStatusCode();
        // A legacy/inconsistent operational status must not bypass the active listing guard.
        (await db.Vehicles.SingleAsync()).Status = VehicleStatus.ReadyForSale; await db.SaveChangesAsync();
        var response = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/listing", new { listingPrice = 6100 });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); Assert.Contains("alreadyListed", await response.Content.ReadAsStringAsync());
        db.VehicleListings.Add(new() { VehicleId = ids.Vehicle, ListingPrice = 6100, IsActive = true });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task ListingRollsBackTogetherAndMissingVehicleReturns404()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        await ReadyForListing(factory, ids);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/vehicles/999/listing")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/vehicles/999/listing", new { listingPrice = 6200 })).StatusCode);
        using var scope = factory.Services.CreateScope(); var services = scope.ServiceProvider;
        var service = ActivatorUtilities.CreateInstance<VehicleService>(services, new FailOnSave(services.GetRequiredService<IUnitOfWork>(), 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ListVehicle(ids.Vehicle, new() { ListingPrice = 6200 }, default));
        var db = services.GetRequiredService<DealerManagerDbContext>();
        Assert.Empty(await db.VehicleListings.ToListAsync()); Assert.Empty(await db.VehicleStatusHistories.ToListAsync());
        Assert.Equal(VehicleStatus.ReadyForSale, (await db.Vehicles.SingleAsync()).Status);
        Assert.Equal(14790, await Balance(factory, ids.Account)); Assert.Equal(2, await db.FinancialTransactions.CountAsync());
    }
}
