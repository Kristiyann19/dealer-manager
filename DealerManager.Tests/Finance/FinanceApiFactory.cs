using DealerManager.Tests.Identity;
using DealerManager.Application.IService.Identity;
using DealerManager.Infrastructure.Persistence;
using DealerManager.Tests.Candidate;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Data.Common;

namespace DealerManager.Tests.Finance;

internal sealed class FinanceApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public FixedTimeProvider Clock { get; } = new();
    public QueryRecorder Queries { get; } = new();
    public FinanceApiFactory() => connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            TestOwnerSession.Configure(services);
            services.RemoveAll<DealerManagerDbContext>();
            services.RemoveAll<DbContextOptions<DealerManagerDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<DealerManagerDbContext>>();
            services.AddScoped<DealerManagerDbContext>(provider => new FinanceTestDbContext(
                new DbContextOptionsBuilder<DealerManagerDbContext>().UseSqlite(connection).AddInterceptors(Queries).Options, provider.GetRequiredService<ICurrentUserContext>()));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    public HttpClient CreateInitializedClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var scope = Services.CreateScope();
        TestOwnerSession.Initialize(scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>());
        TestOwnerSession.Csrf(client);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }
}

// SQLite cannot order DateTimeOffset natively. UTC ticks preserve chronological ordering
// in this isolated test model; production PostgreSQL mappings remain unchanged.
internal sealed class FinanceTestDbContext(DbContextOptions<DealerManagerDbContext> options, ICurrentUserContext currentUser) : DealerManagerDbContext(options, currentUser)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<DealerManager.Domain.Entities.VehicleSale>().Property(e => e.SoldAt)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));
        builder.Entity<DealerManager.Domain.Entities.Candidate>().Property(e => e.CreatedAt)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));
        builder.Entity<DealerManager.Domain.Entities.Vehicle>().Property(e => e.CreatedAt)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));

        builder.Entity<DealerManager.Domain.Entities.FinancialTransaction>().Property(t => t.OccurredAt)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));
        builder.Entity<DealerManager.Domain.Entities.FinancialTransaction>().Property(t => t.CreatedAt)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));
        builder.Entity<DealerManager.Domain.Entities.VehicleExpense>().Property(e => e.PaidAt)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));
        builder.Entity<DealerManager.Domain.Entities.VehicleStatusHistory>().Property(h => h.ChangedAt)
            .HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));
    }
}

internal sealed class QueryRecorder : DbCommandInterceptor
{
    public List<string> Commands { get; } = [];
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        Commands.Add(command.CommandText);
        return ValueTask.FromResult(result);
    }
}
