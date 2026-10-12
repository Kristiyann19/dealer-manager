using System.Net.Http.Json;
using DealerManager.Application.Dtos.Dashboard;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using DealerManager.Infrastructure.Service.Dashboard;
using DealerManager.Infrastructure.Service.Candidate;
using DealerManager.Tests.Finance;
using DealerManager.Tests.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DealerManager.Tests.Dashboard;

public class DashboardAttentionTests
{
    private static async Task<DashboardDto> Read(HttpClient client) => (await client.GetFromJsonAsync<DashboardDto>("/api/dashboard"))!;
    private static DealerManager.Domain.Entities.Vehicle Car(VehicleStatus status, DateTimeOffset now) => new() {
        Make = "BMW", Model = "X3", Status = status, PurchaseDate = now.AddDays(-100), CreatedAt = now.AddDays(-100)
    };
    private static async Task Reservation(DealerManagerDbContext db, DealerManager.Domain.Entities.Vehicle car,
        DateTimeOffset now, DateTimeOffset? expiry, VehicleReservationStatus status = VehicleReservationStatus.Active)
    {
        var account = new CapitalAccount { Name = "EUR", Currency = "EUR", IsActive = true };
        db.Add(account); await db.SaveChangesAsync();
        var transaction = new FinancialTransaction { CapitalAccountId = account.Id, VehicleId = car.Id,
            Amount = 500, Direction = TransactionDirection.In, Type = TransactionType.CapitalContribution, OccurredAt = now, CreatedAt = now };
        db.Add(transaction); await db.SaveChangesAsync();
        db.VehicleReservations.Add(new() { VehicleId = car.Id, DepositAmount = 500, DepositTransactionId = transaction.Id,
            Status = status, ReservedAt = now.AddDays(-7), CreatedAt = now.AddDays(-7), ExpiresAt = expiry });
        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData(-24, VehicleReservationStatus.Active, false, DashboardAttentionType.ReservationExpired)]
    [InlineData(24, VehicleReservationStatus.Active, false, DashboardAttentionType.ReservationExpiringSoon)]
    [InlineData(0, VehicleReservationStatus.Active, false, DashboardAttentionType.ReservationExpiringSoon)]
    [InlineData(48, VehicleReservationStatus.Active, false, DashboardAttentionType.ReservationExpiringSoon)]
    [InlineData(49, VehicleReservationStatus.Active, false, null)]
    [InlineData(120, VehicleReservationStatus.Active, false, null)]
    [InlineData(null, VehicleReservationStatus.Active, false, null)]
    [InlineData(-24, VehicleReservationStatus.Cancelled, false, null)]
    [InlineData(-24, VehicleReservationStatus.Completed, false, null)]
    [InlineData(-24, VehicleReservationStatus.Active, true, null)]
    public async Task ReservationExpiryUsesExactClockAndOnlyActiveUnsoldReservations(int? hours,
        VehicleReservationStatus status, bool sold, DashboardAttentionType? expected)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient();
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var now = factory.Clock.Now; var car = Car(sold ? VehicleStatus.Sold : VehicleStatus.Purchased, now);
        db.Add(car); await db.SaveChangesAsync();
        await Reservation(db, car, now, hours.HasValue ? now.AddHours(hours.Value) : null, status);
        var data = await Read(client);
        if (expected is null) { Assert.Empty(data.AttentionItems); return; }
        var item = Assert.Single(data.AttentionItems);
        Assert.Equal(expected, item.Type); Assert.Equal(500, item.DepositAmount); Assert.Equal(car.Id, item.VehicleId);
        Assert.Equal(expected == DashboardAttentionType.ReservationExpired ? DashboardAttentionSeverity.Critical : DashboardAttentionSeverity.Warning, item.Severity);
    }

    [Theory]
    [InlineData(CandidateStatus.UnderReview, 6, true)]
    [InlineData(CandidateStatus.UnderReview, 5, true)]
    [InlineData(CandidateStatus.UnderReview, 4, false)]
    [InlineData(CandidateStatus.Approved, 6, false)]
    [InlineData(CandidateStatus.Rejected, 6, false)]
    [InlineData(CandidateStatus.Purchased, 6, false)]
    public async Task CandidateWaitingOnlyIncludesUnderReview(CandidateStatus status, int days, bool expected)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient();
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        db.Candidates.Add(new() { Make = "Toyota", Model = "RAV4", Status = status, CreatedAt = factory.Clock.Now.AddDays(-days) });
        await db.SaveChangesAsync(); var data = await Read(client);
        if (!expected) { Assert.Empty(data.AttentionItems); return; }
        var item = Assert.Single(data.AttentionItems); Assert.Equal(DashboardAttentionType.CandidateWaiting, item.Type);
        Assert.Equal(DashboardAttentionSeverity.Info, item.Severity); Assert.Equal(days, item.Days); Assert.NotNull(item.CandidateId);
    }

    [Theory]
    [InlineData(VehicleStatus.Repairing, 15, false, DashboardAttentionType.RepairTakingTooLong)]
    [InlineData(VehicleStatus.Repairing, 14, false, DashboardAttentionType.RepairTakingTooLong)]
    [InlineData(VehicleStatus.Repairing, 5, false, null)]
    [InlineData(VehicleStatus.Transporting, 11, false, DashboardAttentionType.TransportTakingTooLong)]
    [InlineData(VehicleStatus.Transporting, 10, false, DashboardAttentionType.TransportTakingTooLong)]
    [InlineData(VehicleStatus.Transporting, 5, false, null)]
    [InlineData(VehicleStatus.ReadyForSale, 4, false, DashboardAttentionType.ReadyNotListed)]
    [InlineData(VehicleStatus.ReadyForSale, 3, false, DashboardAttentionType.ReadyNotListed)]
    [InlineData(VehicleStatus.ReadyForSale, 2, false, null)]
    [InlineData(VehicleStatus.ReadyForSale, 4, true, null)]
    [InlineData(VehicleStatus.Reserved, 4, false, DashboardAttentionType.ReservedWithoutReservation)]
    [InlineData(VehicleStatus.Listed, 4, false, DashboardAttentionType.ListedWithoutListing)]
    [InlineData(VehicleStatus.Listed, 4, true, null)]
    [InlineData(VehicleStatus.Sold, 40, false, null)]
    public async Task StatusRulesUseLatestEntryNotCreationOrPreviousEntry(VehicleStatus status, int days, bool listing, DashboardAttentionType? expected)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient();
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var now = factory.Clock.Now; var car = Car(status, now); db.Add(car); await db.SaveChangesAsync();
        db.VehicleStatusHistories.AddRange(
            new() { VehicleId = car.Id, FromStatus = VehicleStatus.Purchased, ToStatus = status, ChangedAt = now.AddDays(-90) },
            new() { VehicleId = car.Id, FromStatus = status, ToStatus = VehicleStatus.Arrived, ChangedAt = now.AddDays(-80) },
            new() { VehicleId = car.Id, FromStatus = VehicleStatus.Arrived, ToStatus = status, ChangedAt = now.AddDays(-days) });
        if (listing) db.VehicleListings.Add(new() { VehicleId = car.Id, IsActive = true, ListingPrice = 7000, ListedAt = now, CreatedAt = now });
        await db.SaveChangesAsync(); var data = await Read(client);
        if (expected is null) { Assert.Empty(data.AttentionItems); return; }
        var item = Assert.Single(data.AttentionItems); Assert.Equal(expected, item.Type); Assert.Equal(days, item.Days);
        Assert.Equal(now.AddDays(-days), item.ReferenceDate);
        Assert.Equal(status is VehicleStatus.Listed or VehicleStatus.Reserved ? DashboardAttentionSeverity.Critical
            : status == VehicleStatus.ReadyForSale ? DashboardAttentionSeverity.Info : DashboardAttentionSeverity.Warning, item.Severity);
    }

    [Fact]
    public async Task MissingHistoryDoesNotFallBackToOldCreationDate()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient();
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        foreach (var status in new[] { VehicleStatus.Repairing, VehicleStatus.Transporting, VehicleStatus.ReadyForSale }) db.Add(Car(status, factory.Clock.Now));
        await db.SaveChangesAsync(); Assert.Empty((await Read(client)).AttentionItems);
    }

    [Fact]
    public async Task SortsBySeverityThenReferenceDateWithDeterministicTiesAndCountsBeforeTopFive()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient();
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var now = factory.Clock.Now;
        var expired = Car(VehicleStatus.Reserved, now); var upcoming = Car(VehicleStatus.Reserved, now); var repair = Car(VehicleStatus.Repairing, now);
        db.AddRange(expired, upcoming, repair); await db.SaveChangesAsync();
        await Reservation(db, expired, now, now.AddDays(-1)); await Reservation(db, upcoming, now, now.AddHours(24));
        db.VehicleStatusHistories.Add(new() { VehicleId = repair.Id, FromStatus = VehicleStatus.Arrived, ToStatus = VehicleStatus.Repairing, ChangedAt = now.AddDays(-17) });
        for (var i = 0; i < 5; i++) db.Candidates.Add(new() { Make = "Toyota", Model = i.ToString(), Status = CandidateStatus.UnderReview, CreatedAt = now.AddDays(-8) });
        await db.SaveChangesAsync(); factory.Queries.Commands.Clear(); var data = await Read(client);
        Assert.Equal(8, data.AttentionCount); Assert.Equal(5, data.AttentionItems.Count);
        Assert.Equal(new[] { DashboardAttentionSeverity.Critical, DashboardAttentionSeverity.Warning, DashboardAttentionSeverity.Warning, DashboardAttentionSeverity.Info, DashboardAttentionSeverity.Info }, data.AttentionItems.Select(i => i.Severity));
        Assert.Equal(new[] { DashboardAttentionType.ReservationExpired, DashboardAttentionType.RepairTakingTooLong, DashboardAttentionType.ReservationExpiringSoon }, data.AttentionItems.Take(3).Select(i => i.Type));
        Assert.Equal(new[] { "0", "1" }, data.AttentionItems.Skip(3).Select(i => i.Model));
        Assert.Equal(25, factory.Queries.Commands.Count);
    }

    [Fact]
    public async Task TenantFiltersIsolateCandidatesReservationsListingsAndStatusHistory()
    {
        using var factory = new AuthApiFactory();
        var (a, userA) = await factory.Register("attention-a@example.test", "A"); using var clientA = a;
        var (b, userB) = await factory.Register("attention-b@example.test", "B"); using var clientB = b;
        // Resolve the service against tenant contexts with the same fixed clock, avoiding real time.
        var clock = new DealerManager.Tests.Candidate.FixedTimeProvider();
        foreach (var tenant in new[] { userA.Dealership.Id, userB.Dealership.Id }) {
            using var db = factory.TenantDb(tenant);
            db.Candidates.Add(new() { Make = tenant.ToString(), Status = CandidateStatus.UnderReview, CreatedAt = clock.Now.AddDays(-8) });
            var car = Car(VehicleStatus.Reserved, clock.Now); car.Make = tenant.ToString(); db.Add(car); await db.SaveChangesAsync();
            await Reservation(db, car, clock.Now, clock.Now.AddDays(-1));
            var ready = Car(VehicleStatus.ReadyForSale, clock.Now); ready.Make = tenant.ToString(); db.Add(ready); await db.SaveChangesAsync();
            db.VehicleStatusHistories.Add(new() { VehicleId = ready.Id, FromStatus = VehicleStatus.Preparing, ToStatus = VehicleStatus.ReadyForSale, ChangedAt = clock.Now.AddDays(-4) });
            if (tenant == userB.Dealership.Id) db.VehicleListings.Add(new() { VehicleId = ready.Id, IsActive = true, ListingPrice = 7000, ListedAt = clock.Now, CreatedAt = clock.Now });
            await db.SaveChangesAsync();
        }
        using var dbA = factory.TenantDb(userA.Dealership.Id); using var dbB = factory.TenantDb(userB.Dealership.Id);
        var dataA = await new DashboardService(dbA, clock, new CandidateFinancialCalculator()).GetDashboard(default);
        var dataB = await new DashboardService(dbB, clock, new CandidateFinancialCalculator()).GetDashboard(default);
        Assert.Equal(3, dataA.AttentionCount); Assert.Equal(2, dataB.AttentionCount);
        Assert.All(dataA.AttentionItems, i => Assert.Equal(userA.Dealership.Id.ToString(), i.Make));
        Assert.All(dataB.AttentionItems, i => Assert.Equal(userB.Dealership.Id.ToString(), i.Make));
    }
}
