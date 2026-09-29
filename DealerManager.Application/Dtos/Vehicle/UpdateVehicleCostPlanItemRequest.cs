#nullable enable
using System.ComponentModel.DataAnnotations;
namespace DealerManager.Application.Dtos.Vehicle;

public class UpdateVehicleCostPlanItemRequest
{
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? CurrentEstimatedAmount { get; set; }
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? CommittedAmount { get; set; }
    public string? Description { get; set; }
    public bool IsCancelled { get; set; }
}
