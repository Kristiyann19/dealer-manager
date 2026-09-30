#nullable enable
using DealerManager.Application.Dtos.Vehicle;
namespace DealerManager.Application.IService.Vehicle;
public interface IVehicleService
{
    Task<VehicleListingDto> ListVehicle(int vehicleId, ListVehicleRequest request, CancellationToken cancellationToken);
    Task<VehicleListingDto?> GetCurrentListing(int vehicleId, CancellationToken cancellationToken);
    Task<VehicleListResultDto> GetVehicles(DealerManager.Application.FilterDtos.Vehicle.VehicleFilterDto filter, CancellationToken cancellationToken);
    Task<VehicleStatusHistoryDto> ChangeStatus(int vehicleId, ChangeVehicleStatusRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<VehicleStatusHistoryDto>> GetStatusHistory(int vehicleId, CancellationToken cancellationToken);
    Task<VehiclePaymentAccountDto> GetPaymentAccount(int vehicleId, CancellationToken cancellationToken);
    Task<VehiclePaymentPreviewDto> GetPaymentPreview(int vehicleId, int itemId, CancellationToken cancellationToken);
    Task<VehicleExpenseDto> ConfirmPayment(int vehicleId, int itemId, ConfirmVehiclePaymentRequest request, CancellationToken cancellationToken);
    Task<VehicleDetailsDto> GetDetails(int id, CancellationToken cancellationToken);
    Task<VehicleFinancialSummaryDto> GetFinancialSummary(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<VehicleCostPlanItemDto>> GetCostPlan(int vehicleId, CancellationToken cancellationToken);
    Task<VehicleCostPlanItemDto> UpdateCostPlanItem(int vehicleId, int itemId, UpdateVehicleCostPlanItemRequest request, CancellationToken cancellationToken);
    Task<VehicleExpenseDto> AddExpense(int vehicleId, AddVehicleExpenseRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<VehicleExpenseDto>> GetExpenses(int vehicleId, CancellationToken cancellationToken);
}
public class VehicleNotFoundException(int id) : Exception($"Vehicle {id} was not found.");
