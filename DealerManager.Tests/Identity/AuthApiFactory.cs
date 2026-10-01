using System.Net.Http.Json;
using DealerManager.Application.Dtos.Identity;
using DealerManager.Application.IService.Identity;
using DealerManager.Infrastructure.Persistence;
using DealerManager.Tests.Candidate;
using DealerManager.Tests.Finance;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace DealerManager.Tests.Identity;

internal sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public AuthApiFactory() => connection.Open();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services => {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<DealerManagerDbContext>();
            services.RemoveAll<DbContextOptions<DealerManagerDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<DealerManagerDbContext>>();
            services.AddScoped<DealerManagerDbContext>(provider => new FinanceTestDbContext(
                new DbContextOptionsBuilder<DealerManagerDbContext>().UseSqlite(connection).Options,
                provider.GetRequiredService<ICurrentUserContext>()));
        });
    }
    public HttpClient Client()
    {
        var client = CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>().Database.EnsureCreated();
        return client;
    }
    public static async Task Csrf(HttpClient client)
    {
        var token = (await client.GetFromJsonAsync<Token>("/api/auth/csrf"))!.TokenValue;
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN"); client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", token);
    }
    private sealed record Token([property: System.Text.Json.Serialization.JsonPropertyName("token")] string TokenValue);
    public static object Registration(string email = "owner@example.test", string name = "Auto A", string password = "Test!SecurePassword123")
        => new { email, password, confirmPassword = password, dealershipName = name };
    public async Task<(HttpClient Client, CurrentUserDto User)> Register(string email, string name)
    {
        var client = Client(); await Csrf(client);
        var response = await client.PostAsJsonAsync("/api/auth/register", Registration(email, name)); response.EnsureSuccessStatusCode();
        var user = (await response.Content.ReadFromJsonAsync<CurrentUserDto>())!;
        await Csrf(client); return (client, user);
    }
    public DealerManagerDbContext TenantDb(int? dealershipId) => new FinanceTestDbContext(
        new DbContextOptionsBuilder<DealerManagerDbContext>().UseSqlite(connection).Options, new Tenant(dealershipId));
    private sealed class Tenant(int? id) : ICurrentUserContext
    {
        public bool IsAuthenticated => id.HasValue;
        public int? UserId => 1;
        public int? DealershipId => id;
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
}
