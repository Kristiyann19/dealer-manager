#nullable enable
using System.ComponentModel.DataAnnotations;
namespace DealerManager.Application.Dtos.Vehicle;
public class ReserveVehicleRequest
{
    [Required, StringLength(200)] public string CustomerName { get; set; } = string.Empty;
    [StringLength(50)] public string? CustomerPhone { get; set; }
    [EmailAddress, StringLength(254)] public string? CustomerEmail { get; set; }
    [Range(typeof(decimal), "0", "79228162514264337593543950335", MinimumIsExclusive = true)]
    public decimal DepositAmount { get; set; }
    [Range(1, int.MaxValue)] public int CapitalAccountId { get; set; }
    public DateTimeOffset? ReservedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    [StringLength(2000)] public string? Notes { get; set; }
}
