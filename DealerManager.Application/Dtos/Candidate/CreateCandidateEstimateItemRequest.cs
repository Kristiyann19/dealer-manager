using DealerManager.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace DealerManager.Application.Dtos.Candidate
{
    public class CreateCandidateEstimateItemRequest
    {
        [EnumDataType(typeof(CostCategory))]
        public CostCategory Category { get; set; }
        [Required]
        public string Description { get; set; } = string.Empty;
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal EstimatedAmount { get; set; }
    }
}
