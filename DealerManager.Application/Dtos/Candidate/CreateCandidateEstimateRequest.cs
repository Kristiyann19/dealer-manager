#nullable enable
using System.ComponentModel.DataAnnotations;

namespace DealerManager.Application.Dtos.Candidate
{
    public class CreateCandidateEstimateRequest
    {
        [Range(1, int.MaxValue)]
        public int CandidateId { get; set; }
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal ExpectedSellingPrice { get; set; }
        public string? Notes { get; set; }
        [Required, MinLength(1)]
        public List<CreateCandidateEstimateItemRequest> Items { get; set; } = [];
    }
}
