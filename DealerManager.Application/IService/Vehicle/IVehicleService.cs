using DealerManager.Application.Dtos.Vehicle;
namespace DealerManager.Application.IService.Vehicle;
public interface IVehicleService
{
    Task<VehicleDetailsDto> GetDetails(int id, CancellationToken cancellationToken);
}
public class VehicleNotFoundException(int id) : Exception($"Vehicle {id} was not found.");
