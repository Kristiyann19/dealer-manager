#nullable enable
using DealerManager.Application.Dtos.Identity;
namespace DealerManager.Application.IService.Identity;
public interface IAuthenticationService
{
    Task<CurrentUserDto> Register(RegisterRequest request, CancellationToken token);
    Task<CurrentUserDto?> Login(LoginRequest request, CancellationToken token);
    Task<CurrentUserDto?> Me(CancellationToken token);
    Task Logout();
}
public sealed class AuthRequestException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
