using System.ComponentModel.DataAnnotations;
namespace DealerManager.Application.Dtos.Identity;
public class LoginRequest
{
    [Required, EmailAddress, MaxLength(256)] public string Email { get; set; } = "";
    [Required, MaxLength(256)] public string Password { get; set; } = "";
}
public sealed class RegisterRequest : LoginRequest
{
    [Required, Compare(nameof(Password))] public string ConfirmPassword { get; set; } = "";
    [Required, MaxLength(200)] public string DealershipName { get; set; } = "";
}
public sealed record CurrentUserDto(int Id, string Email, DealershipDto Dealership, IList<string> Roles);
public sealed record DealershipDto(int Id, string Name);
