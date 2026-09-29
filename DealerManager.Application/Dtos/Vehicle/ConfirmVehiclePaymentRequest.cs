using System.ComponentModel.DataAnnotations;

namespace DealerManager.Application.Dtos.Vehicle;

// These values guard the reviewed preview; the server still derives the payment from the plan/purchase.
public class ConfirmVehiclePaymentRequest
{
    [Range(typeof(decimal), "0", "79228162514264337593543950335", MinimumIsExclusive = true)]
    public decimal ExpectedAmount { get; set; }
    [Range(1, int.MaxValue)]
    public int ExpectedCapitalAccountId { get; set; }
}
