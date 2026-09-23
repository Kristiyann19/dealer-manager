using DealerManager.Domain.Common;
using DealerManager.Domain.Enums;

namespace DealerManager.Domain.Entities;

// Source of truth for actual capital movements.
public class FinancialTransaction : Entity
{
    private decimal _amount;

    public int CapitalAccountId { get; set; }
    public TransactionType Type { get; set; }
    public TransactionDirection Direction { get; set; }

    /// <summary>
    /// Positive monetary amount. Direction determines whether capital increases or decreases.
    /// </summary>
    public required decimal Amount
    {
        get => _amount;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Amount), value, "Transaction amount must be positive.");
            }

            _amount = value;
        }
    }

    public int? VehicleId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public CapitalAccount CapitalAccount { get; set; } = null!;
    public Vehicle? Vehicle { get; set; }
}
