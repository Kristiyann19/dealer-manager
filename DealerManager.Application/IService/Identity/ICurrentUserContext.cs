namespace DealerManager.Application.IService.Identity;
public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    int? UserId { get; }
    int? DealershipId { get; }
}
public sealed class TenantAccessException() : Exception("Resource not found.");
