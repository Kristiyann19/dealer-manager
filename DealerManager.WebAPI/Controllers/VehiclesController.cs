using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.IService.Vehicle;
using Microsoft.AspNetCore.Mvc;

namespace DealerManager.Controllers;
[ApiController]
[Route("api/vehicles")]
public class VehiclesController(IVehicleService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    public Task<VehicleDetailsDto> GetDetails(int id, CancellationToken cancellationToken)
        => service.GetDetails(id, cancellationToken);
}
