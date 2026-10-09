using System.Net.Http.Json;
using DealerManager.Application.Dtos.Dashboard;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using DealerManager.Tests.Finance;
using Microsoft.Extensions.DependencyInjection;

namespace DealerManager.Tests.Dashboard;

public class AgedInventoryTests
{
    [Fact]
    public async Task Boundaries_active_listing_sort_limit_and_filter_are_consistent()
    {
        using var factory = new FinanceApiFactory();
        using var client = factory.CreateInitializedClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var now = factory.Clock.Now;
        var account = new CapitalAccount { Name = "EUR", Currency = "EUR", IsActive = true };
        db.Add(account); await db.SaveChangesAsync();
        async Task<DealerManager.Domain.Entities.Vehicle> Add(int days, VehicleStatus status, decimal amount)
        {
            var vehicle = new DealerManager.Domain.Entities.Vehicle {
                Make = "Aged", Model = days.ToString(), Year = 2020, Status = status,
                PurchaseDate = new DateTimeOffset(now.UtcDateTime.Date.AddDays(-days).AddHours(23), TimeSpan.Zero), CreatedAt = now
            };
            db.Add(vehicle); await db.SaveChangesAsync();
            db.FinancialTransactions.Add(new() { CapitalAccountId = account.Id, VehicleId = vehicle.Id,
                Amount = amount, Type = TransactionType.VehiclePurchase, Direction = TransactionDirection.Out,
                OccurredAt = now, CreatedAt = now });
            await db.SaveChangesAsync(); return vehicle;
        }
        await Add(30, VehicleStatus.Listed, 1000);
        var warning = await Add(31, VehicleStatus.Repairing, 2000);
        await Add(60, VehicleStatus.ReadyForSale, 3000);
        var critical = await Add(61, VehicleStatus.Listed, 4000);
        await Add(100, VehicleStatus.Sold, 9000);
        db.VehicleListings.Add(new() { VehicleId = critical.Id, ListingPrice = 7000, ListedAt = now.AddDays(-10), IsActive = true });
        db.VehicleListings.Add(new() { VehicleId = critical.Id, ListingPrice = 8000, ListedAt = now.AddDays(-40), IsActive = false });
        await db.SaveChangesAsync();
        var dashboard = (await client.GetFromJsonAsync<DashboardDto>("/api/dashboard"))!;
        Assert.Equal(new[] {61, 60, 31}, dashboard.AgedVehicles.Select(v => v.DaysInStock));
        Assert.Equal(new[] {"Critical", "Warning", "Warning"}, dashboard.AgedVehicles.Select(v => v.Severity));
        Assert.Equal(10, dashboard.AgedVehicles[0].DaysListed);
        Assert.Equal(7000, dashboard.AgedVehicles[0].ListingPrice);
        Assert.Null(dashboard.AgedVehicles[2].DaysListed);
        Assert.Null(dashboard.AgedVehicles[2].ListingPrice);
        var filtered = (await client.GetFromJsonAsync<VehicleListResultDto>("/api/vehicles?AgedStock=true"))!;
        Assert.Equal(3, filtered.TotalCount);
        Assert.Contains(filtered.Items, v => v.Id == warning.Id);
        Assert.DoesNotContain(filtered.Items, v => v.Status == VehicleStatus.Sold);
        await Add(70, VehicleStatus.Purchased, 5000);
        var expensive = await Add(70, VehicleStatus.Purchased, 6000);
        await Add(90, VehicleStatus.Purchased, 1000);
        dashboard = (await client.GetFromJsonAsync<DashboardDto>("/api/dashboard"))!;
        Assert.Equal(5, dashboard.AgedVehicles.Count);
        Assert.Equal(new[] {90, 70, 70, 61, 60}, dashboard.AgedVehicles.Select(v => v.DaysInStock));
        Assert.Equal(expensive.Id, dashboard.AgedVehicles[1].Id);
        factory.Clock.Now = now.AddDays(1);
        filtered = (await client.GetFromJsonAsync<VehicleListResultDto>("/api/vehicles?AgedStock=true"))!;
        Assert.Equal(7, filtered.TotalCount);
    }
}
