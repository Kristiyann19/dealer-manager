#nullable enable
using System.ComponentModel.DataAnnotations;
using DealerManager.Domain.Enums;
namespace DealerManager.Application.Dtos.Vehicle;

public class AddVehicleExpenseRequest
{
    [Range(1, int.MaxValue)]
    public int? CapitalAccountId { get; set; }
    [Range(1, int.MaxValue)]
    public int? CostPlanItemId { get; set; }
    [EnumDataType(typeof(CostCategory))]
    public CostCategory Category { get; set; }
    [Required]
    public string Description { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "79228162514264337593543950335", MinimumIsExclusive = true)]
    public decimal Amount { get; set; }
    public string? Supplier { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}
