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
            vehicle.Vin = Normalize(request.Vin)?.ToUpperInvariant();
            vehicle.Mileage = request.Mileage;
            vehicle.RegistrationNumber = Normalize(request.RegistrationNumber)?.ToUpperInvariant();
            vehicle.FirstRegistration = request.FirstRegistration;
            vehicle.FuelType = request.FuelType;
            vehicle.Transmission = request.Transmission;
            vehicle.EngineDisplacementCc = request.EngineDisplacementCc;
            vehicle.PowerKw = request.PowerKw;
            vehicle.PowerHp = request.PowerHp;
            vehicle.DriveType = request.DriveType;
            vehicle.EuroStandard = request.EuroStandard;
            vehicle.BodyType = request.BodyType;
            vehicle.Color = Normalize(request.Color);
            vehicle.NumberOfDoors = request.NumberOfDoors;
            vehicle.NumberOfSeats = request.NumberOfSeats;
            vehicle.NumberOfKeys = request.NumberOfKeys;
            vehicle.ImportedFrom = Normalize(request.ImportedFrom);
            vehicle.Notes = Normalize(request.Notes);
            await unitOfWork.SaveChanges(token);
            return await GetDetails(id, token);
        }, cancellationToken);
    }
}
