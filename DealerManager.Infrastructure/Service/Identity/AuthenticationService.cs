#nullable enable
using DealerManager.Application.Dtos.Identity;
using DealerManager.Application.IRepository;
using DealerManager.Application.IService.Identity;
using DealerManager.Domain.Entities;
using DealerManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.ComponentModel.DataAnnotations;
namespace DealerManager.Infrastructure.Service.Identity;

public sealed class AuthenticationService(DealerManagerDbContext db, UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn, IUnitOfWork unitOfWork, TimeProvider clock, ICurrentUserContext currentUser) : IAuthenticationService
{
    public async Task<CurrentUserDto> Register(RegisterRequest request, CancellationToken token)
    {
        Validator.ValidateObject(request, new ValidationContext(request), true);
        if (string.IsNullOrWhiteSpace(request.DealershipName)) throw new AuthRequestException("Dealership name is required.");
        var email = request.Email.Trim();
        if (await users.FindByEmailAsync(email) != null) throw new AuthRequestException("Registration could not be completed with this email.", 409);
        ApplicationUser user;
        try
        {
            user = await unitOfWork.ExecuteInTransaction(async cancellation => {
                var dealership = new Dealership { Name = request.DealershipName.Trim(), CreatedAt = clock.GetUtcNow() };
                db.Dealerships.Add(dealership);
                await unitOfWork.SaveChanges(cancellation);
                var createdUser = new ApplicationUser { UserName = email, Email = email, DealershipId = dealership.Id };
                var created = await users.CreateAsync(createdUser, request.Password);
                if (!created.Succeeded) throw new AuthRequestException(string.Join(" ", created.Errors.Select(e => e.Description)));
                var assigned = await users.AddToRoleAsync(createdUser, "Owner");
                if (!assigned.Succeeded) throw new InvalidOperationException("Could not assign the Owner role.");
                return createdUser;
            }, token);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new AuthRequestException("Registration could not be completed with this email.", 409);
        }
        // Cookies are issued only after the shared Identity/dealership transaction commits.
        await signIn.SignInAsync(user, isPersistent: false);
        return await Describe(user, token);
    }
    public async Task<CurrentUserDto?> Login(LoginRequest request, CancellationToken token)
    {
        Validator.ValidateObject(request, new ValidationContext(request), true);
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user == null) return null;
        var result = await signIn.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: true);
        return result.Succeeded ? await Describe(user, token) : null;
    }
    public async Task<CurrentUserDto?> Me(CancellationToken token)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } id) return null;
        var user = await users.FindByIdAsync(id.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return user == null ? null : await Describe(user, token);
    }
    public Task Logout() => signIn.SignOutAsync();
    private async Task<CurrentUserDto> Describe(ApplicationUser user, CancellationToken token)
    {
        var dealership = await db.Dealerships.AsNoTracking().SingleAsync(d => d.Id == user.DealershipId, token);
        return new(user.Id, user.Email!, new(dealership.Id, dealership.Name), await users.GetRolesAsync(user));
    }
}
