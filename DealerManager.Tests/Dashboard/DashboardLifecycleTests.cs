using System.Net.Http.Json;
using DealerManager.Application.Dtos.Candidate;
using DealerManager.Application.Dtos.Dashboard;
using DealerManager.Application.Dtos.Finance;
using DealerManager.Application.IRepository;
using DealerManager.Infrastructure.Persistence;
using DealerManager.Tests.Finance;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace DealerManager.Tests.Dashboard;

public class DashboardLifecycleTests
{
    [Fact]
    public async Task CapitalPurchaseExpenseAndSaleUpdateDashboardThroughRealEndpoints()
    {
        // Optional production-provider check: all test records roll back, no migrations or schema changes.
        var connectionString = Environment.GetEnvironmentVariable("DEALER_DASHBOARD_POSTGRES");
        using WebApplicationFactory<Program> factory = string.IsNullOrEmpty(connectionString)
            ? new FinanceApiFactory() : new RollbackPostgresFactory(connectionString);
        using var client = factory is FinanceApiFactory sqlite ? sqlite.CreateInitializedClient()
            : factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        async Task<DashboardDto> Dashboard() => (await client.GetFromJsonAsync<DashboardDto>("/api/dashboard"))!;
        async Task<T> Post<T>(string url, object body) {
            var response = await client.PostAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }
        async Task Send(string url, object body) => (await client.PostAsJsonAsync(url, body)).EnsureSuccessStatusCode();
        var baseline = await Dashboard();
        var account = await Post<CapitalAccountDto>("/api/capital-accounts", new { name = "Dashboard verification", currency = "EUR" });
        await Send($"/api/capital-accounts/{account.Id}/contributions", new { amount = 20000 });
        var funded = await Dashboard(); Assert.Equal(baseline.Financial.AvailableCash + 20000, funded.Financial.AvailableCash);
        var candidate = await Post<CandidateDetailsDto>("/api/candidates", new { make = "DashboardTest", model = "Lifecycle", year = 2020, askingPrice = 6000 });
        await Send("/api/candidates/estimates", new { candidateId = candidate.Id, expectedSellingPrice = 15000,
            items = new[] { new { category = 0, estimatedAmount = 6000, description = "Purchase" }, new { category = 2, estimatedAmount = 1000, description = "Repair" } } });
        await Send($"/api/candidates/{candidate.Id}/approve", new { });
        var bought = await Post<PurchaseCandidateResultDto>($"/api/candidates/{candidate.Id}/purchase", new { capitalAccountId = account.Id, actualPurchasePrice = 6000 });
        var afterPurchase = await Dashboard();
        Assert.Equal(baseline.Financial.AvailableCash + 14000, afterPurchase.Financial.AvailableCash);
        Assert.Equal(baseline.Financial.CapitalInvested + 6000, afterPurchase.Financial.CapitalInvested);
        Assert.Equal(baseline.Financial.UpcomingProjectedCosts + 1000, afterPurchase.Financial.UpcomingProjectedCosts);
        Assert.Equal(baseline.Monthly.CarsInStock + 1, afterPurchase.Monthly.CarsInStock);
        var plans = (await client.GetFromJsonAsync<List<DealerManager.Application.Dtos.Vehicle.VehicleCostPlanItemDto>>($"/api/vehicles/{bought.VehicleId}/cost-plan"))!;
        await Send($"/api/vehicles/{bought.VehicleId}/cost-plan/{plans.Single().Id}/confirm-payment", new { expectedAmount = 1000, expectedCapitalAccountId = account.Id });
        var spent = await Dashboard();
        Assert.Equal(baseline.Financial.AvailableCash + 13000, spent.Financial.AvailableCash);
        Assert.Equal(baseline.Financial.CapitalInvested + 7000, spent.Financial.CapitalInvested);
        Assert.Equal(baseline.Financial.NetWorthAtCost + 20000, spent.Financial.NetWorthAtCost);
        Assert.Equal(baseline.Financial.UpcomingProjectedCosts, spent.Financial.UpcomingProjectedCosts);
        await Send($"/api/vehicles/{bought.VehicleId}/status", new { status = "ReadyForSale" });
        await Send($"/api/vehicles/{bought.VehicleId}/listing", new { listingPrice = 11000 });
        var listed = await Dashboard();
        var row = Assert.Single(listed.ActiveVehicles, v => v.Id == bought.VehicleId);
        Assert.Equal(11000, row.ListingPrice); Assert.Equal(15000, row.ExpectedSellingPrice); Assert.Equal(8000, row.ProjectedProfit);
        await Send($"/api/vehicles/{bought.VehicleId}/sale", new { actualSalePrice = 9000 });
        var sold = await Dashboard();
        Assert.Equal(baseline.Financial.AvailableCash + 22000, sold.Financial.AvailableCash);
        Assert.Equal(baseline.Financial.CapitalInvested, sold.Financial.CapitalInvested);
        Assert.Equal(baseline.Financial.NetWorthAtCost + 22000, sold.Financial.NetWorthAtCost);
        Assert.Equal(baseline.Monthly.ProfitThisMonth + 2000, sold.Monthly.ProfitThisMonth);
        Assert.Equal(baseline.Monthly.SoldThisMonth + 1, sold.Monthly.SoldThisMonth);
        Assert.Equal(baseline.Monthly.CarsInStock, sold.Monthly.CarsInStock);
        Assert.DoesNotContain(sold.ActiveVehicles, v => v.Id == bought.VehicleId);
        Assert.Equal(baseline.MonthlyFinancials[^1].CapitalInvested + 7000, sold.MonthlyFinancials[^1].CapitalInvested);
        Assert.Equal(baseline.MonthlyFinancials[^1].SalesRevenue + 9000, sold.MonthlyFinancials[^1].SalesRevenue);
        Assert.Equal(baseline.MonthlyFinancials[^1].RealizedProfit + 2000, sold.MonthlyFinancials[^1].RealizedProfit);
    }
}

// A single rollback transaction wraps the whole HTTP workflow against existing PostgreSQL data.
// Request-scoped DbContexts enlist in it; the test UnitOfWork must not start nested transactions.
internal sealed class RollbackPostgresFactory : WebApplicationFactory<Program>
{
    private readonly NpgsqlConnection connection;
    private readonly NpgsqlTransaction transaction;
    public RollbackPostgresFactory(string connectionString)
    {
        connection = new NpgsqlConnection(connectionString); connection.Open();
        transaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead);
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services => {
            services.RemoveAll<DealerManagerDbContext>();
            services.RemoveAll<DbContextOptions<DealerManagerDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<DealerManagerDbContext>>();
            services.AddScoped(_ => {
                var db = new DealerManagerDbContext(new DbContextOptionsBuilder<DealerManagerDbContext>().UseNpgsql(connection).Options);
                db.Database.UseTransaction(transaction); return db;
            });
            services.RemoveAll<IUnitOfWork>();
            services.AddScoped<IUnitOfWork, EnlistedUnitOfWork>();
        });
    }
    private bool disposed;
    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            disposed = true;
            transaction.Rollback();
            base.Dispose(disposing);
            transaction.Dispose(); connection.Dispose();
        }
        else base.Dispose(disposing);
    }
    private sealed class EnlistedUnitOfWork(DealerManagerDbContext db) : IUnitOfWork
    {
        public Task<int> SaveChanges(CancellationToken token) => db.SaveChangesAsync(token);
        public Task<T> ExecuteInTransaction<T>(Func<CancellationToken, Task<T>> operation, CancellationToken token) => operation(token);
    }
}
