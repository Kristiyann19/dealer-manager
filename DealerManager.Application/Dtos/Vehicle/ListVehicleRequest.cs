#nullable enable
using System.ComponentModel.DataAnnotations;

namespace DealerManager.Application.Dtos.Vehicle;

public class ListVehicleRequest
{
    [Range(typeof(decimal), "0", "79228162514264337593543950335", MinimumIsExclusive = true)]
    public decimal ListingPrice { get; set; }
    public DateTimeOffset? ListedAt { get; set; }
}
