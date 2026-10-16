using System.ComponentModel.DataAnnotations;
namespace DealerManager.Application.Dtos.Vehicle;

public class ReverseVehicleExpenseRequest
{
    [Required, MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
public class CorrectVehicleExpenseRequest : AddVehicleExpenseRequest
{
    [Required, MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
