using Microsoft.AspNetCore.Identity;
namespace DealerManager.Domain.Entities;
public class ApplicationUser : IdentityUser<int>
{
    public int DealershipId { get; set; }
    public Dealership Dealership { get; set; } = null!;
}
