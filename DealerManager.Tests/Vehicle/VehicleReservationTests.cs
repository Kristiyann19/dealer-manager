using System.Net;
using System.Net.Http.Json;
using DealerManager.Application.Dtos.Vehicle;
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
public class VehicleReservationTests
{
    private static async Task<(int Vehicle, int Account)> Seed(DealerManagerDbContext db, DateTimeOffset now)
    {
        var account = new CapitalAccount { Name = "Cash", Currency = "EUR", IsActive = true };
        var vehicle = new DealerManager.Domain.Entities.Vehicle { Make = "BMW", Model = "320d", Year = 2020, Status = VehicleStatus.Listed, PurchaseDate = now.AddDays(-10), CreatedAt = now };
        db.Add(account); db.Add(vehicle); await db.SaveChangesAsync();
        db.VehicleListings.Add(new() { VehicleId = vehicle.Id, IsActive = true, ListingPrice = 12500, ListedAt = now, CreatedAt = now });
        db.FinancialTransactions.Add(new() { CapitalAccountId = account.Id, Amount = 30000, Type = TransactionType.CapitalContribution, Direction = TransactionDirection.In, OccurredAt = now, CreatedAt = now });
        db.FinancialTransactions.Add(new() { CapitalAccountId = account.Id, VehicleId = vehicle.Id, Amount = 10000, Type = TransactionType.VehiclePurchase, Direction = TransactionDirection.Out, OccurredAt = now, CreatedAt = now });
        await db.SaveChangesAsync(); return (vehicle.Id, account.Id);
    }
    private static Task<decimal> Balance(DealerManagerDbContext db, int account) => db.FinancialTransactions.Where(t => t.CapitalAccountId == account).SumAsync(t => t.Direction == TransactionDirection.In ? t.Amount : -t.Amount);
    private static ReserveVehicleRequest Request(int account, decimal amount = 500) => new() { CustomerName = "Ivan Ivanov", CapitalAccountId = account, DepositAmount = amount };
    private static async Task<VehicleReservationDto> Reserve(HttpClient client, int vehicle, int account, decimal amount = 500)
    {
        var response = await client.PostAsJsonAsync($"/api/vehicles/{vehicle}/reservations", Request(account, amount));
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<VehicleReservationDto>())!;
    }
    [Theory]
    [InlineData(12000)] [InlineData(500)]
    public async Task DepositAndFinalPaymentAreCountedExactlyOnce(decimal price)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient();
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var ids = await Seed(db, factory.Clock.Now);
        var r = await Reserve(client, ids.Vehicle, ids.Account);
        Assert.Equal(20500, await Balance(db, ids.Account)); Assert.Equal(VehicleStatus.Reserved, r.VehicleStatus);
        Assert.Equal(factory.Clock.Now, r.ReservedAt); Assert.Equal(VehicleReservationStatus.Active, r.Status);
        db.ChangeTracker.Clear(); Assert.True((await db.VehicleListings.SingleAsync()).IsActive);
        var history = await db.VehicleStatusHistories.SingleAsync(); Assert.Equal(VehicleStatus.Listed, history.FromStatus); Assert.Equal(VehicleStatus.Reserved, history.ToStatus);
        var details = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!;
        Assert.Equal(r.ReservationId, details.ActiveReservation!.ReservationId);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/reservations", Request(ids.Account))).StatusCode);
        var response = await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/sale", new { actualSalePrice = price });
        response.EnsureSuccessStatusCode(); var result = (await response.Content.ReadFromJsonAsync<VehicleSaleResultDto>())!;
        Assert.Equal(20000 + price, result.NewCapitalBalance); Assert.Equal(price - 10000, result.RealizedProfit);
        db.ChangeTracker.Clear();
        var sale = await db.VehicleSales.SingleAsync(); Assert.Equal(price, sale.SalePrice); Assert.Equal(r.ReservationId, sale.ReservationId);
        Assert.Equal(500, (await db.FinancialTransactions.SingleAsync(t => t.Type == TransactionType.VehicleDeposit)).Amount);
        if (price == 500) { Assert.Null(sale.FinancialTransactionId); Assert.False(await db.FinancialTransactions.AnyAsync(t => t.Type == TransactionType.VehicleSale)); }
        else Assert.Equal(11500, (await db.FinancialTransactions.SingleAsync(t => t.Type == TransactionType.VehicleSale)).Amount);
        Assert.False(await db.FinancialTransactions.AnyAsync(t => t.Amount <= 0));
        Assert.Equal(VehicleReservationStatus.Completed, (await db.VehicleReservations.SingleAsync()).Status);
        Assert.Equal(factory.Clock.Now, (await db.VehicleReservations.SingleAsync()).CompletedAt);
        Assert.False((await db.VehicleListings.SingleAsync()).IsActive);
        history = await db.VehicleStatusHistories.SingleAsync(h => h.ToStatus == VehicleStatus.Sold); Assert.Equal(VehicleStatus.Reserved, history.FromStatus);
        details = (await client.GetFromJsonAsync<VehicleDetailsDto>($"/api/vehicles/{ids.Vehicle}"))!;
        Assert.Null(details.ActiveReservation); Assert.Equal(price - 10000, details.RealizedProfit); Assert.Equal(20000 + price, details.SaleAccount!.CurrentBalance);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/reservations/{r.ReservationId}/cancel", new { refundDeposit = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/reservations", Request(ids.Account))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync($"/api/vehicles/{ids.Vehicle}/reservations/active")).StatusCode);
    }
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task CancellationRefundsOriginalAccountOrRetainsOnlyThatCustomersDeposit(bool refund)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient();
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>(); var ids = await Seed(db, factory.Clock.Now);
        var r = await Reserve(client, ids.Vehicle, ids.Account);
        (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/reservations/{r.ReservationId}/cancel", new { refundDeposit = refund, notes = "Cancelled" })).EnsureSuccessStatusCode();
        db.ChangeTracker.Clear(); var cancelled = await db.VehicleReservations.SingleAsync();
        Assert.Equal(VehicleReservationStatus.Cancelled, cancelled.Status); Assert.NotNull(cancelled.CancelledAt);
        Assert.Equal(refund ? 20000 : 20500, await Balance(db, ids.Account)); Assert.Empty(await db.VehicleSales.ToListAsync());
        Assert.Equal(VehicleStatus.Listed, (await db.Vehicles.SingleAsync()).Status); Assert.True((await db.VehicleListings.SingleAsync()).IsActive);
        var history = await db.VehicleStatusHistories.SingleAsync(h => h.ToStatus == VehicleStatus.Listed); Assert.Equal(VehicleStatus.Reserved, history.FromStatus);
        if (refund) { var t = await db.FinancialTransactions.SingleAsync(t => t.Type == TransactionType.DepositRefund); Assert.Equal(500, t.Amount); Assert.Equal(TransactionDirection.Out, t.Direction); Assert.Equal(ids.Account, t.CapitalAccountId); Assert.Equal(t.Id, cancelled.RefundTransactionId); }
        else { Assert.Null(cancelled.RefundTransactionId); Assert.False(await db.FinancialTransactions.AnyAsync(t => t.Type == TransactionType.DepositRefund)); }
        factory.Clock.Now = factory.Clock.Now.AddHours(1);
        var next = await Reserve(client, ids.Vehicle, ids.Account, 200);
        var historyDto = (await client.GetFromJsonAsync<List<VehicleReservationDto>>($"/api/vehicles/{ids.Vehicle}/reservations"))!;
        Assert.Equal(2, historyDto.Count); Assert.Equal(next.ReservationId, historyDto[0].ReservationId);
        (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/sale", new { actualSalePrice = 12000 })).EnsureSuccessStatusCode();
        db.ChangeTracker.Clear(); Assert.Equal(11800, (await db.FinancialTransactions.SingleAsync(t => t.Type == TransactionType.VehicleSale)).Amount);
        Assert.Equal(refund ? 32000 : 32500, await Balance(db, ids.Account));
        Assert.Equal(VehicleReservationStatus.Cancelled, (await db.VehicleReservations.SingleAsync(r => r.Id == cancelled.Id)).Status);
    }
    [Theory]
    [InlineData(0, 400)] [InlineData(-1, 400)] [InlineData(12501, 409)]
    public async Task InvalidDepositsDoNotWrite(decimal amount, int status)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>(); var ids = await Seed(db, factory.Clock.Now);
        Assert.Equal(status, (int)(await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/reservations", Request(ids.Account, amount))).StatusCode);
        Assert.Empty(await db.VehicleReservations.ToListAsync()); Assert.Equal(20000, await Balance(db, ids.Account));
    }
    [Theory]
    [InlineData(0)] [InlineData(6)] [InlineData(8)] [InlineData(9)]
    public async Task NonListedVehiclesCannotBeReserved(int status)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>(); var ids = await Seed(db, factory.Clock.Now);
        (await db.Vehicles.SingleAsync()).Status = (VehicleStatus)status; await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/reservations", Request(ids.Account))).StatusCode);
        Assert.Equal(20000, await Balance(db, ids.Account));
    }
    [Fact]
    public async Task SaleBelowDepositAndMissingOutcomeAreRejectedWithoutWrites()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>(); var ids = await Seed(db, factory.Clock.Now); var r = await Reserve(client, ids.Vehicle, ids.Account);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/sale", new { actualSalePrice = 499 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/reservations/{r.ReservationId}/cancel", new { })).StatusCode);
        Assert.Equal(20500, await Balance(db, ids.Account)); Assert.Empty(await db.VehicleSales.ToListAsync());
    }
    [Fact]
    public async Task RefundUsesDepositAccountEvenWhenDifferentFromPurchaseAccount()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>(); var ids = await Seed(db, factory.Clock.Now);
        var depositAccount = new CapitalAccount { Name = "Deposits", Currency = "EUR", IsActive = true };
        db.Add(depositAccount); await db.SaveChangesAsync();
        var r = await Reserve(client, ids.Vehicle, depositAccount.Id);
        Assert.Equal(500, await Balance(db, depositAccount.Id)); Assert.Equal(20000, await Balance(db, ids.Account));
        (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/reservations/{r.ReservationId}/cancel", new { refundDeposit = true })).EnsureSuccessStatusCode();
        var refund = await db.FinancialTransactions.SingleAsync(t => t.Type == TransactionType.DepositRefund);
        Assert.Equal(depositAccount.Id, refund.CapitalAccountId); Assert.Equal(0, await Balance(db, depositAccount.Id)); Assert.Equal(20000, await Balance(db, ids.Account));
    }
    [Fact]
    public async Task DatabaseRejectsSecondActiveReservationWithDifferentDeposit()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>(); var ids = await Seed(db, factory.Clock.Now);
        await Reserve(client, ids.Vehicle, ids.Account);
        var deposit = new FinancialTransaction { CapitalAccountId = ids.Account, VehicleId = ids.Vehicle, Amount = 100, Type = TransactionType.VehicleDeposit, Direction = TransactionDirection.In, OccurredAt = factory.Clock.Now, CreatedAt = factory.Clock.Now };
        // Simulate a competing write; use a transaction so even this test leaves no orphan deposit.
        await using var transaction = await db.Database.BeginTransactionAsync();
        db.Add(deposit); await db.SaveChangesAsync();
        db.VehicleReservations.Add(new() { VehicleId = ids.Vehicle, CustomerName = "Other customer", DepositAmount = 100, DepositTransactionId = deposit.Id, ReservedAt = factory.Clock.Now, CreatedAt = factory.Clock.Now });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        await transaction.RollbackAsync(); db.ChangeTracker.Clear();
        Assert.Single(await db.VehicleReservations.ToListAsync()); Assert.Equal(20500, await Balance(db, ids.Account));
    }
    [Fact]
    public async Task ReservationRequiresActiveListingAndGenericStatusCannotReserve()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>(); var ids = await Seed(db, factory.Clock.Now);
        (await db.VehicleListings.SingleAsync()).IsActive = false; await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/reservations", Request(ids.Account))).StatusCode);
        (await db.Vehicles.SingleAsync()).Status = VehicleStatus.ReadyForSale; await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/vehicles/{ids.Vehicle}/status", new { status = 8 })).StatusCode);
        Assert.Equal(20000, await Balance(db, ids.Account)); Assert.Empty(await db.VehicleReservations.ToListAsync());
    }
    private class FailOnSave(IUnitOfWork inner, int failAt) : IUnitOfWork
    {
        private int count;
        public Task<int> SaveChanges(CancellationToken token) => ++count == failAt ? throw new InvalidOperationException("Injected failure") : inner.SaveChanges(token);
        public Task<T> ExecuteInTransaction<T>(Func<CancellationToken, Task<T>> action, CancellationToken token) => inner.ExecuteInTransaction(action, token);
    }
    [Theory]
    [InlineData("reserve", 1)] [InlineData("reserve", 2)] [InlineData("refund", 1)] [InlineData("refund", 2)] [InlineData("sale", 1)] [InlineData("sale", 2)]
    public async Task EveryWriteRollsBackOnFailure(string action, int failAt)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>(); var ids = await Seed(db, factory.Clock.Now);
        var r = action == "reserve" ? null : await Reserve(client, ids.Vehicle, ids.Account);
        db.ChangeTracker.Clear(); var count = await db.FinancialTransactions.CountAsync();
        var service = ActivatorUtilities.CreateInstance<VehicleService>(scope.ServiceProvider, new FailOnSave(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), failAt));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            if (action == "reserve") await service.ReserveVehicle(ids.Vehicle, Request(ids.Account), default);
            else if (action == "refund") await service.CancelReservation(ids.Vehicle, r!.ReservationId, new() { RefundDeposit = true }, default);
            else await service.SellVehicle(ids.Vehicle, new() { ActualSalePrice = 12000 }, default);
        });
        Assert.Equal(count, await db.FinancialTransactions.CountAsync()); Assert.Empty(await db.VehicleSales.ToListAsync());
        Assert.Equal(action == "reserve" ? VehicleStatus.Listed : VehicleStatus.Reserved, (await db.Vehicles.SingleAsync()).Status);
        Assert.True((await db.VehicleListings.SingleAsync()).IsActive);
    }
    [Fact]
    public async Task RealTenantSessionsCannotReserveReadCancelOrSellForeignResources()
    {
        using var factory = new AuthApiFactory(); var (a, userA) = await factory.Register("reservation-a@example.test", "A"); using var clientA = a;
        var (b, userB) = await factory.Register("reservation-b@example.test", "B"); using var clientB = b;
        using var dbA = factory.TenantDb(userA.Dealership.Id); using var dbB = factory.TenantDb(userB.Dealership.Id);
        var idsA = await Seed(dbA, DateTimeOffset.UtcNow); var idsB = await Seed(dbB, DateTimeOffset.UtcNow);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/vehicles/{idsB.Vehicle}/reservations", Request(idsA.Account))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/vehicles/{idsA.Vehicle}/reservations", Request(idsB.Account))).StatusCode);
        var r = await Reserve(b, idsB.Vehicle, idsB.Account);
        foreach (var suffix in new[] { "/reservations", "/reservations/active" }) Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync($"/api/vehicles/{idsB.Vehicle}{suffix}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/vehicles/{idsB.Vehicle}/reservations/{r.ReservationId}/cancel", new { refundDeposit = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/vehicles/{idsA.Vehicle}/reservations/{r.ReservationId}/cancel", new { refundDeposit = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/vehicles/{idsB.Vehicle}/sale", new { actualSalePrice = 12000 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsJsonAsync($"/api/vehicles/{idsB.Vehicle}/sale", new { actualSalePrice = 12000, capitalAccountId = idsA.Account })).StatusCode);
        Assert.Equal(20000, await Balance(dbA, idsA.Account)); Assert.Equal(20500, await Balance(dbB, idsB.Account));
    }
}
