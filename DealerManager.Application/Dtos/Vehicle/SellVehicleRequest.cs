#nullable enable
using System.ComponentModel.DataAnnotations;
namespace DealerManager.Application.Dtos.Vehicle;
public class SellVehicleRequest
{
    [Range(typeof(decimal), "0", "79228162514264337593543950335", MinimumIsExclusive = true)]
    public decimal ActualSalePrice { get; set; }
    public DateTimeOffset? SoldAt { get; set; }
    [Range(1, int.MaxValue)]
    public int? CapitalAccountId { get; set; }
}
