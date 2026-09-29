using DealerManager.Domain.Enums;

namespace DealerManager.Application.Dtos.Vehicle;

public class VehiclePaymentAccountDto
{
    public int CapitalAccountId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
}

public class VehiclePaymentPreviewDto : VehiclePaymentAccountDto
{
    public CostCategory Category { get; set; }
    public decimal Amount { get; set; }
}
