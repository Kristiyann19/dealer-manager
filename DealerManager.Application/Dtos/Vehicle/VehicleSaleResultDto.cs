using DealerManager.Domain.Enums;
namespace DealerManager.Application.Dtos.Vehicle;
public class VehicleSaleResultDto
{
    public int VehicleId { get; init; }
    public int SaleId { get; init; }
    public decimal ActualSalePrice { get; init; }
    public DateTimeOffset SoldAt { get; init; }
    public int CapitalAccountId { get; init; }
    public decimal NewCapitalBalance { get; init; }
    public decimal TotalInvested { get; init; }
    public decimal RealizedProfit { get; init; }
    public decimal RealizedROI { get; init; }
    public VehicleStatus Status { get; init; }
}
