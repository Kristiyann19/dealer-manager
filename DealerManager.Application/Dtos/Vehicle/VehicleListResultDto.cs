#nullable enable
using DealerManager.Domain.Enums;
namespace DealerManager.Application.Dtos.Vehicle;
public class VehicleListResultDto
{
    public IReadOnlyList<VehicleListItemDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
}
public class VehicleListItemDto : VehicleFinancialSummaryDto
{
    public int Id { get; init; }
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public int? Mileage { get; init; }
    public string? Vin { get; init; }
    public VehicleStatus Status { get; init; }
    public DateTimeOffset PurchaseDate { get; init; }
}
