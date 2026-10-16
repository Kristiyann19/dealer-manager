#nullable enable
using DealerManager.Application.Dtos.Vehicle;
namespace DealerManager.Application.IService.Vehicle;
public interface IVehicleService
{
    Task<VehicleExpenseDto> ReverseExpense(int vehicleId, int expenseId, ReverseVehicleExpenseRequest request, CancellationToken cancellationToken);
    Task<VehicleExpenseDto> CorrectExpense(int vehicleId, int expenseId, CorrectVehicleExpenseRequest request, CancellationToken cancellationToken);
    Task<VehicleReservationDto> ReserveVehicle(int vehicleId, ReserveVehicleRequest request, CancellationToken cancellationToken);
    Task<VehicleReservationDto> CancelReservation(int vehicleId, int reservationId, CancelReservationRequest request, CancellationToken cancellationToken);
    Task<VehicleReservationDto?> GetActiveReservation(int vehicleId, CancellationToken cancellationToken);
    Task<IReadOnlyList<VehicleReservationDto>> GetReservations(int vehicleId, CancellationToken cancellationToken);
    Task<VehicleDetailsDto> UpdateDossier(int id, UpdateVehicleDossierRequest request, CancellationToken cancellationToken);
    Task<VehicleSaleResultDto> SellVehicle(int vehicleId, SellVehicleRequest request, CancellationToken cancellationToken);
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

public class VehicleReservationNotFoundException(int id) : Exception($"Reservation {id} was not found.");
