using System.Security.Claims;
using DealerManager.Application.IService.Identity;
namespace DealerManager.WebAPI.Identity;
public sealed class CurrentUserContext(IHttpContextAccessor accessor) : ICurrentUserContext
{
    public const string DealershipClaim = "dealer:dealership_id";
    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;
    public int? UserId => Read(ClaimTypes.NameIdentifier);
    public int? DealershipId => Read(DealershipClaim);
    private int? Read(string name) => IsAuthenticated && int.TryParse(accessor.HttpContext?.User.FindFirstValue(name), out var id) && id > 0 ? id : null;
}
