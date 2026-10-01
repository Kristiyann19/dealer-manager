using Microsoft.AspNetCore.DataProtection;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DealerManager.Application.IService.Identity;
using DealerManager.Domain.Entities;
using DealerManager.Infrastructure.Persistence;
using DealerManager.WebAPI.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DealerManager.Tests.Identity;
// Only legacy business regression fixtures use this identity; auth/isolation tests use real cookies.
internal sealed class TestCurrentUser : ICurrentUserContext
{
    public bool IsAuthenticated => true;
    public int? UserId => 1;
    public int? DealershipId => 1;
}
internal sealed class TestOwnerHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.Success(
        new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Role, "Owner"),
            new Claim(CurrentUserContext.DealershipClaim, "1")], Scheme.Name)), Scheme.Name)));
}
internal static class TestOwnerSession
{
    public static void Configure(IServiceCollection services)
    {
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.RemoveAll<ICurrentUserContext>(); services.AddScoped<ICurrentUserContext, TestCurrentUser>();
        services.AddAuthentication(o => { o.DefaultAuthenticateScheme = "TestOwner"; o.DefaultChallengeScheme = "TestOwner"; o.DefaultForbidScheme = "TestOwner"; })
            .AddScheme<AuthenticationSchemeOptions, TestOwnerHandler>("TestOwner", _ => { });
    }
    public static void Initialize(DealerManagerDbContext db)
    {
        db.Database.EnsureCreated();
        if (!db.Dealerships.Any(d => d.Id == 1)) { db.Dealerships.Add(new Dealership { Id = 1, Name = "Regression test dealership" }); db.SaveChanges(); }
    }
    public static void Csrf(HttpClient client)
    {
        var csrf = client.GetFromJsonAsync<CsrfDto>("/api/auth/csrf").GetAwaiter().GetResult()!;
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN"); client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", csrf.Token);
    }
    private sealed record CsrfDto(string Token);
}
