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
    [Fact]
    public async Task StatusFlowUpdatesInventoryAndRecordsUtcHistoryWithoutFinancialChanges()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        var before = await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}");
        var response = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/status", new { status = "Repairing", notes = "  In workshop  " });
        response.EnsureSuccessStatusCode();
        var change = (await response.Content.ReadFromJsonAsync<VehicleStatusHistoryDto>())!;
        Assert.Equal(VehicleStatus.Purchased, change.FromStatus); Assert.Equal(VehicleStatus.Repairing, change.ToStatus);
        Assert.Equal(factory.Clock.Now, change.ChangedAt); Assert.Equal("In workshop", change.Notes);
        var after = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!;
        Assert.Equal(VehicleStatus.Repairing, after.Status); Assert.Equal(before!.TotalInvested, after.TotalInvested);
        Assert.Equal(13700, await Balance(factory, ids.Account));
        var list = (await client.GetFromJsonAsync<VehicleListResultDto>("/api/vehicles?Status=Repairing"))!;
        Assert.Equal(VehicleStatus.Repairing, Assert.Single(list.Items).Status);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        Assert.Null((await db.Set<VehicleStatusHistory>().SingleAsync()).ChangedByUserId);
        Assert.Equal(2, await db.FinancialTransactions.CountAsync());
        (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/status", new { status = VehicleStatus.Preparing })).EnsureSuccessStatusCode();
        var history = (await client.GetFromJsonAsync<List<VehicleStatusHistoryDto>>($"/api/vehicles/{ids.Vehicle}/status-history"))!;
        Assert.Equal(2, history.Count); Assert.Equal(VehicleStatus.Preparing, history[0].ToStatus); Assert.True(history[0].Id > history[1].Id);
        // Chronology wins over Id when timestamps differ.
        var old = await db.Set<VehicleStatusHistory>().SingleAsync(h => h.Id == change.Id);
        old.ChangedAt = factory.Clock.Now.AddMinutes(1); await db.SaveChangesAsync();
        history = (await client.GetFromJsonAsync<List<VehicleStatusHistoryDto>>($"/api/vehicles/{ids.Vehicle}/status-history"))!;
        Assert.Equal(change.Id, history[0].Id);
    }

    [Theory]
    [InlineData(0, 0, 409)] [InlineData(0, 7, 409)] [InlineData(0, 8, 409)] [InlineData(0, 9, 409)]
    [InlineData(9, 4, 409)] [InlineData(8, 4, 409)] [InlineData(7, 4, 409)]
    [InlineData(0, 999, 400)] [InlineData(0, -1, 400)]
    public async Task InvalidStatusDoesNotWriteHistory(int from, int to, int expectedStatus)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var vehicle = await db.Vehicles.SingleAsync(); vehicle.Status = (VehicleStatus)from; await db.SaveChangesAsync();
        var response = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/status", new { status = to });
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        Assert.Empty(await db.Set<VehicleStatusHistory>().ToListAsync());
        await db.Entry(vehicle).ReloadAsync(); Assert.Equal((VehicleStatus)from, vehicle.Status);
    }

    [Theory]
    [InlineData(VehicleStatus.Preparing)] [InlineData(VehicleStatus.ReadyForSale)] [InlineData(VehicleStatus.Arrived)]
    public async Task OperationalStagesCanBeSkipped(VehicleStatus next)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        var response = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/status", new { status = next, notes = " " });
        response.EnsureSuccessStatusCode(); Assert.Null((await response.Content.ReadFromJsonAsync<VehicleStatusHistoryDto>())!.Notes);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/status", new { notes = "Missing status" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/vehicles/999/status", new { status = 4 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/vehicles/999/status-history")).StatusCode);
    }

    [Fact]
    public async Task StatusAndHistoryRollBackTogether()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        using var scope = factory.Services.CreateScope(); var services = scope.ServiceProvider;
        var service = ActivatorUtilities.CreateInstance<VehicleService>(services, new FailOnSave(services.GetRequiredService<IUnitOfWork>(), 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChangeStatus(ids.Vehicle, new() { Status = VehicleStatus.Preparing }, default));
        var db = services.GetRequiredService<DealerManagerDbContext>();
        Assert.Empty(await db.Set<VehicleStatusHistory>().ToListAsync()); Assert.Equal(VehicleStatus.Purchased, (await db.Vehicles.SingleAsync()).Status);
    }

    [Fact]
    public async Task InventoryUsesSharedFinancialsAndBoundedQueriesWithSearchStatusAndPagination()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); var ids = await Seed(factory);
        await SeedHistoricalExpense(factory, ids, 300, ids.Repair);
        var expected = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
            for (var i = 0; i < 25; i++) db.Vehicles.Add(new() { Make = "Audi", Model = "A3", Vin = "UNIQUE" + i, Year = 2020, Status = VehicleStatus.Preparing });
            await db.SaveChangesAsync();
        }
        factory.Queries.Commands.Clear();
        var all = (await client.GetFromJsonAsync<VehicleListResultDto>("/api/vehicles?Limit=30"))!;
        Assert.Equal(26, all.TotalCount); Assert.Equal(26, all.Items.Count); Assert.InRange(factory.Queries.Commands.Count, 1, 6);
        var bmw = all.Items.Single(v => v.Id == ids.Vehicle);
        Assert.Equal(expected.TotalInvested, bmw.TotalInvested); Assert.Equal(expected.RemainingProjectedCosts, bmw.RemainingProjectedCosts);
        Assert.Equal(expected.ProjectedFinalCost, bmw.ProjectedFinalCost); Assert.Equal(expected.ProjectedProfit, bmw.ProjectedProfit);
        Assert.Null(all.Items.First().ExpectedSellingPrice);
        foreach (var term in new[] { "bMw", "320d", "vin" })
        {
            var found = (await client.GetFromJsonAsync<VehicleListResultDto>($"/api/vehicles?TextFilter={term}"))!;
            Assert.Equal(ids.Vehicle, Assert.Single(found.Items).Id);
        }
        var filtered = (await client.GetFromJsonAsync<VehicleListResultDto>("/api/vehicles?Status=5&Limit=5&Offset=5"))!;
        Assert.Equal(25, filtered.TotalCount); Assert.Equal(5, filtered.Items.Count);
        Assert.All(filtered.Items, v => Assert.Equal(VehicleStatus.Preparing, v.Status));
        Assert.True(filtered.Items[0].Id > filtered.Items[1].Id);
        var empty = (await client.GetFromJsonAsync<VehicleListResultDto>("/api/vehicles?TextFilter=BMW&Status=Sold"))!;
        Assert.Empty(empty.Items); Assert.Equal(0, empty.TotalCount);
        foreach (var query in new[] { "Limit=0", "Offset=-1", "Limit=501", "Status=999" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/vehicles?" + query)).StatusCode);
    }
}
