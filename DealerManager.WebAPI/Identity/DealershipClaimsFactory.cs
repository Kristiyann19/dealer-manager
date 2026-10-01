using System.Security.Claims;
using DealerManager.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
namespace DealerManager.WebAPI.Identity;
public sealed class DealershipClaimsFactory(UserManager<ApplicationUser> users, RoleManager<IdentityRole<int>> roles,
    IOptions<IdentityOptions> options) : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<int>>(users, roles, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(CurrentUserContext.DealershipClaim, user.DealershipId.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return identity;
    }
}
