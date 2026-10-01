using DealerManager.Domain.Common;
namespace DealerManager.Domain.Entities;
public class Dealership : Entity
{
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}
