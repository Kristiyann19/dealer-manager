using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.IService.Vehicle;
using Microsoft.AspNetCore.Mvc;

namespace DealerManager.Controllers;
[ApiController]
[Route("api/vehicles")]
public class VehiclesController(IVehicleService service) : ControllerBase
{
    [HttpPost("{id:int}/expenses/{expenseId:int}/reverse")]
    public Task<VehicleExpenseDto> ReverseExpense(int id, int expenseId, ReverseVehicleExpenseRequest request, CancellationToken cancellationToken)
        => service.ReverseExpense(id, expenseId, request, cancellationToken);
    [HttpPost("{id:int}/expenses/{expenseId:int}/correct")]
    public Task<VehicleExpenseDto> CorrectExpense(int id, int expenseId, CorrectVehicleExpenseRequest request, CancellationToken cancellationToken)
        => service.CorrectExpense(id, expenseId, request, cancellationToken);

    [HttpPost("{id:int}/reservations")]
    public async Task<ActionResult<VehicleReservationDto>> Reserve(int id, ReserveVehicleRequest request, CancellationToken cancellationToken)
        => CreatedAtAction(nameof(GetActiveReservation), new { id }, await service.ReserveVehicle(id, request, cancellationToken));
    [HttpPost("{id:int}/reservations/{reservationId:int}/cancel")]
    public Task<VehicleReservationDto> CancelReservation(int id, int reservationId, CancelReservationRequest request, CancellationToken cancellationToken)
        => service.CancelReservation(id, reservationId, request, cancellationToken);
    [HttpGet("{id:int}/reservations")]
    public Task<IReadOnlyList<VehicleReservationDto>> GetReservations(int id, CancellationToken cancellationToken)
        => service.GetReservations(id, cancellationToken);
    [HttpGet("{id:int}/reservations/active")]
    public async Task<ActionResult<VehicleReservationDto>> GetActiveReservation(int id, CancellationToken cancellationToken)
    {
        var reservation = await service.GetActiveReservation(id, cancellationToken);
        return reservation is null ? NoContent() : Ok(reservation);
    }
    [HttpPut("{id:int}/details")]
    public Task<VehicleDetailsDto> UpdateDossier(int id, UpdateVehicleDossierRequest request, CancellationToken cancellationToken)
        => service.UpdateDossier(id, request, cancellationToken);

    [HttpPost("{id:int}/sale")]
    public async Task<ActionResult<VehicleSaleResultDto>> SellVehicle(int id, SellVehicleRequest request, CancellationToken cancellationToken)
        => CreatedAtAction(nameof(GetDetails), new { id }, await service.SellVehicle(id, request, cancellationToken));
    [HttpPost("{id:int}/listing")]
    public async Task<ActionResult<VehicleListingDto>> ListVehicle(int id, ListVehicleRequest request, CancellationToken cancellationToken)
        => CreatedAtAction(nameof(GetCurrentListing), new { id }, await service.ListVehicle(id, request, cancellationToken));

    [HttpGet("{id:int}/listing")]
    public async Task<ActionResult<VehicleListingDto>> GetCurrentListing(int id, CancellationToken cancellationToken)
    {
        var listing = await service.GetCurrentListing(id, cancellationToken);
        return listing is null ? NoContent() : Ok(listing);
    }
    [HttpGet]
    public Task<VehicleListResultDto> GetVehicles([FromQuery] DealerManager.Application.FilterDtos.Vehicle.VehicleFilterDto filter, CancellationToken cancellationToken)
        => service.GetVehicles(filter, cancellationToken);

    [HttpPost("{id:int}/status")]
    public Task<VehicleStatusHistoryDto> ChangeStatus(int id, ChangeVehicleStatusRequest request, CancellationToken cancellationToken)
        => service.ChangeStatus(id, request, cancellationToken);

    [HttpGet("{id:int}/status-history")]
    public Task<IReadOnlyList<VehicleStatusHistoryDto>> GetStatusHistory(int id, CancellationToken cancellationToken)
        => service.GetStatusHistory(id, cancellationToken);

    [HttpGet("{id:int}/payment-account")]
    public Task<VehiclePaymentAccountDto> GetPaymentAccount(int id, CancellationToken cancellationToken)
        => service.GetPaymentAccount(id, cancellationToken);

    [HttpGet("{id:int}/cost-plan/{itemId:int}/payment-preview")]
    public Task<VehiclePaymentPreviewDto> GetPaymentPreview(int id, int itemId, CancellationToken cancellationToken)
        => service.GetPaymentPreview(id, itemId, cancellationToken);

    [HttpPost("{id:int}/cost-plan/{itemId:int}/confirm-payment")]
    public async Task<ActionResult<VehicleExpenseDto>> ConfirmPayment(int id, int itemId, ConfirmVehiclePaymentRequest request, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await service.ConfirmPayment(id, itemId, request, cancellationToken));

    [HttpGet("{id:int}")]
    public Task<VehicleDetailsDto> GetDetails(int id, CancellationToken cancellationToken)
        => service.GetDetails(id, cancellationToken);

    [HttpGet("{id:int}/financial-summary")]
    public Task<VehicleFinancialSummaryDto> GetFinancialSummary(int id, CancellationToken cancellationToken)
        => service.GetFinancialSummary(id, cancellationToken);

    [HttpGet("{id:int}/cost-plan")]
    public Task<IReadOnlyList<VehicleCostPlanItemDto>> GetCostPlan(int id, CancellationToken cancellationToken)
        => service.GetCostPlan(id, cancellationToken);

    [HttpPut("{id:int}/cost-plan/{itemId:int}")]
    public Task<VehicleCostPlanItemDto> UpdateCostPlanItem(int id, int itemId, UpdateVehicleCostPlanItemRequest request, CancellationToken cancellationToken)
        => service.UpdateCostPlanItem(id, itemId, request, cancellationToken);

    [HttpGet("{id:int}/expenses")]
    public Task<IReadOnlyList<VehicleExpenseDto>> GetExpenses(int id, CancellationToken cancellationToken)
        => service.GetExpenses(id, cancellationToken);

    [HttpPost("{id:int}/expenses")]
    public async Task<ActionResult<VehicleExpenseDto>> AddExpense(int id, AddVehicleExpenseRequest request, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await service.AddExpense(id, request, cancellationToken));
}
