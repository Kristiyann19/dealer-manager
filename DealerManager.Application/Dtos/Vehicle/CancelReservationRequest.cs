#nullable enable
using System.ComponentModel.DataAnnotations;
namespace DealerManager.Application.Dtos.Vehicle;
public class CancelReservationRequest
{
    [Required] public bool? RefundDeposit { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    [StringLength(2000)] public string? Notes { get; set; }
}
