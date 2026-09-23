#nullable enable
using System.ComponentModel.DataAnnotations;

namespace DealerManager.Application.Dtos.Candidate
{
    public class CreateCandidateRequest
    {
        [Required]
        public string Make { get; set; } = string.Empty;
        [Required]
        public string Model { get; set; } = string.Empty;
        public int? Year { get; set; }
        [Range(0, int.MaxValue)]
        public int? Mileage { get; set; }
        public string? Vin { get; set; }
        public string? Source { get; set; }
        public string? Location { get; set; }
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal ExpectedSellingPrice { get; set; }
        public string? Notes { get; set; }
    }
}
