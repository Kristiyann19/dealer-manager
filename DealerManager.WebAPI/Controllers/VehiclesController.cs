using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.IService.Vehicle;
using Microsoft.AspNetCore.Mvc;

namespace DealerManager.Controllers;
[ApiController]
[Route("api/vehicles")]
public class VehiclesController(IVehicleService service) : ControllerBase
{
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
