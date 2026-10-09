#nullable enable
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.IService.Vehicle;
using Microsoft.EntityFrameworkCore;
namespace DealerManager.Infrastructure.Service.Vehicle;
public partial class VehicleService
{
    public Task<VehicleDetailsDto> UpdateDossier(int id, UpdateVehicleDossierRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            var vehicle = await vehicles.GetById(id, token, query => query.AsTracking())
                ?? throw new VehicleNotFoundException(id);
            mapper.Map(request, vehicle);
            await unitOfWork.SaveChanges(token);
            return await GetDetails(id, token);
        }, cancellationToken);
    }
}
