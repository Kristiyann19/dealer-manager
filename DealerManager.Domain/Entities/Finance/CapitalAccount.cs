using DealerManager.Domain.Common;

namespace DealerManager.Domain.Entities;

// Balance is derived from transactions; it is not stored on the account.
public class CapitalAccount : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ICollection<FinancialTransaction> FinancialTransactions { get; set; } = new List<FinancialTransaction>();
}
