using DealerManager.Application.IService.Identity;
using DealerManager.Domain.Entities;
using DealerManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace DealerManager.WebAPI.Identity;
public static class IdentityConfiguration
{
    public static void AddDealerIdentity(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthenticationService, DealerManager.Infrastructure.Service.Identity.AuthenticationService>();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddIdentity<ApplicationUser, IdentityRole<int>>(options => {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddEntityFrameworkStores<DealerManagerDbContext>().AddDefaultTokenProviders();
        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, DealershipClaimsFactory>();
        // Revalidate membership/security stamp on each request, including after an administrative tenant change.
        services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
        services.ConfigureApplicationCookie(options => {
            options.Cookie.Name = "DealerManager.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment() || environment.IsEnvironment("Testing")
                ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
            options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
        });
        services.AddAuthorization(options => {
            options.AddPolicy("AuthenticatedUser", p => p.RequireAuthenticatedUser());
            var owner = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole("Owner")
                .RequireAssertion(c => int.TryParse(c.User.FindFirst(CurrentUserContext.DealershipClaim)?.Value, out var tenant) && tenant > 0).Build();
            options.DefaultPolicy = owner;
            options.FallbackPolicy = owner;
        });
        services.AddAntiforgery(options => {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "DealerManager.Antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = environment.IsDevelopment() || environment.IsEnvironment("Testing")
                ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        services.AddScoped<ApiAntiforgeryFilter>();
        services.Configure<MvcOptions>(o => o.Filters.AddService<ApiAntiforgeryFilter>());
    }
}
