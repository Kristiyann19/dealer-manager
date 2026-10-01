using DealerManager.Tests.Identity;
using DealerManager.Application.IService.Identity;
using DealerManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DealerManager.Tests.Candidate;

internal sealed class CandidateApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public CandidateApiFactory() => connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            TestOwnerSession.Configure(services);
            services.RemoveAll<DbContextOptions<DealerManagerDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<DealerManagerDbContext>>();
            services.AddDbContext<DealerManagerDbContext>(options => options.UseSqlite(connection));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedTimeProvider());
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
        if (disposing)
            connection.Dispose();
    }
}
