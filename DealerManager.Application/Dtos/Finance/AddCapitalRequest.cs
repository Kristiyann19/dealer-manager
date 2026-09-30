#nullable enable
using System.ComponentModel.DataAnnotations;

namespace DealerManager.Application.Dtos.Finance
{
    public class AddCapitalRequest
    {
        // The HTTP route supplies this value; direct service callers must supply it.
        [Range(1, int.MaxValue)]
        public int? CapitalAccountId { get; set; }
        [Range(typeof(decimal), "0", "79228162514264337593543950335", MinimumIsExclusive = true)]
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset? OccurredAt { get; set; }
    }
}
