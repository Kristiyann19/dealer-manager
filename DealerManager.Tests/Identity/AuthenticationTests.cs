using System.Net;
using System.Net.Http.Json;
using DealerManager.Application.Dtos.Identity;
using DealerManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace DealerManager.Tests.Identity;

public class AuthenticationTests
{
    [Fact]
    public async Task RegistrationCreatesAtomicOwnerAndSecureCookieAndMe()
    {
        using var factory = new AuthApiFactory(); using var client = factory.Client(); await AuthApiFactory.Csrf(client);
        var response = await client.PostAsJsonAsync("/api/auth/register", AuthApiFactory.Registration());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("stamp", body, StringComparison.OrdinalIgnoreCase);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("DealerManager.Auth="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase); Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase); Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        var me = (await client.GetFromJsonAsync<CurrentUserDto>("/api/auth/me"))!;
        Assert.Equal("Auto A", me.Dealership.Name); Assert.Contains("Owner", me.Roles); Assert.True(me.Id > 0); Assert.True(me.Dealership.Id > 0);
        using var db = factory.TenantDb(me.Dealership.Id);
        var user = await db.Users.SingleAsync(); Assert.Equal(me.Dealership.Id, user.DealershipId); Assert.NotEqual("Test!SecurePassword123", user.PasswordHash);
        Assert.Equal(1, await db.UserRoles.CountAsync()); Assert.Equal(2, await db.Dealerships.CountAsync());
    }
    [Fact]
    public async Task DuplicateEmailIsCaseInsensitiveAndDoesNotLeaveOrphanDealership()
    {
        using var factory = new AuthApiFactory(); var (first, _) = await factory.Register("owner@example.test", "First"); using var owned = first;
        using var client = factory.Client(); await AuthApiFactory.Csrf(client);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/auth/register", AuthApiFactory.Registration("OWNER@example.test"))).StatusCode);
        using var db = factory.TenantDb(null); Assert.Equal(1, await db.Users.CountAsync()); Assert.Equal(2, await db.Dealerships.CountAsync());
    }
    [Theory]
    [InlineData("short")] [InlineData("abcdefghijklmno")]
    public async Task InvalidIdentityPasswordRollsBackDealership(string password)
    {
        using var factory = new AuthApiFactory(); using var client = factory.Client(); await AuthApiFactory.Csrf(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/register", AuthApiFactory.Registration(password: password))).StatusCode);
        using var db = factory.TenantDb(null); Assert.Empty(await db.Users.ToListAsync()); Assert.Single(await db.Dealerships.ToListAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }
    [Fact]
    public async Task LoginLogoutAndCurrentUserUseRealCookies()
    {
        using var factory = new AuthApiFactory(); var (client, user) = await factory.Register("owner@example.test", "Auto"); using var owned = client;
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        await AuthApiFactory.Csrf(client);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "OWNER@example.test", password = "Test!SecurePassword123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(user.Dealership.Id, (await client.GetFromJsonAsync<CurrentUserDto>("/api/auth/me"))!.Dealership.Id);
    }
    [Theory]
    [InlineData("owner@example.test")] [InlineData("missing@example.test")]
    public async Task InvalidLoginIsGeneric401(string email)
    {
        using var factory = new AuthApiFactory(); var (owner, _) = await factory.Register("owner@example.test", "Auto"); using var owned = owner;
        using var client = factory.Client(); await AuthApiFactory.Csrf(client);
        var result = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Incorrect!Password123" });
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode); Assert.Contains("Invalid credentials.", await result.Content.ReadAsStringAsync());
    }
    [Theory]
    [InlineData("/api/auth/me")] [InlineData("/api/dashboard")] [InlineData("/api/candidates")] [InlineData("/api/vehicles")] [InlineData("/api/capital-accounts")]
    public async Task AnonymousGets401WithoutRedirect(string path)
    {
        using var factory = new AuthApiFactory(); using var client = factory.Client();
        var result = await client.GetAsync(path); Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode); Assert.Null(result.Headers.Location);
    }
    [Fact]
    public async Task MissingCsrfIsRejectedForRegisterLoginAndAuthenticatedWrites()
    {
        using var factory = new AuthApiFactory(); using var anonymous = factory.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/auth/register", AuthApiFactory.Registration())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/auth/login", new { email = "a@example.test", password = "x" })).StatusCode);
        var (owner, _) = await factory.Register("owner@example.test", "Auto"); using var owned = owner;
        owner.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/capital-accounts", new { name = "Account", currency = "EUR" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsync("/api/auth/logout", null)).StatusCode);
    }
    [Fact]
    public async Task RoleRemovalIsRevalidatedAndReturns403WithoutRedirect()
    {
        using var factory = new AuthApiFactory(); var (owner, user) = await factory.Register("owner@example.test", "Auto"); using var owned = owner;
        using var db = factory.TenantDb(null); db.UserRoles.RemoveRange(await db.UserRoles.ToListAsync()); await db.SaveChangesAsync();
        var response = await owner.GetAsync("/api/dashboard"); Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); Assert.Null(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/api/auth/me")).StatusCode);
    }
}
