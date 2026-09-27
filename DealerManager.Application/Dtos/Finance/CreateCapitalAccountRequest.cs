#nullable enable
using System.ComponentModel.DataAnnotations;

namespace DealerManager.Application.Dtos.Finance
{
    public class CreateCapitalAccountRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Currency { get; set; } = string.Empty;
    }
}
