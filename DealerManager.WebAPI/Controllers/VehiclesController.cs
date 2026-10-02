using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.IService.Vehicle;
using Microsoft.AspNetCore.Mvc;

namespace DealerManager.Controllers;
[ApiController]
[Route("api/vehicles")]
public class VehiclesController(IVehicleService service) : ControllerBase
{
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
