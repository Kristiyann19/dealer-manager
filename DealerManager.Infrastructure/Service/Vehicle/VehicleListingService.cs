#nullable enable
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.IService.Vehicle;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Linq.Expressions;

namespace DealerManager.Infrastructure.Service.Vehicle;

public partial class VehicleService
{
    public Task<VehicleListingDto> ListVehicle(int vehicleId, ListVehicleRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            var vehicle = await vehicles.GetById(vehicleId, token, query => query.AsTracking())
                ?? throw new VehicleNotFoundException(vehicleId);
            if (await listings.GetQueryByProperties(l => l.VehicleId == vehicleId && l.IsActive).AnyAsync(token))
                throw Conflict("alreadyListed", "The vehicle already has an active listing.");
            if (vehicle.Status != VehicleStatus.ReadyForSale)
                throw Conflict("notReadyForListing", "Only a ReadyForSale vehicle can be listed.");

            var now = clock.GetUtcNow();
            var listing = new VehicleListing
            {
                VehicleId = vehicleId, ListingPrice = request.ListingPrice,
                ListedAt = request.ListedAt?.ToUniversalTime() ?? now, IsActive = true, CreatedAt = now
            };
            await listings.Create(listing);
            await statusHistory.Create(new VehicleStatusHistory
            {
                VehicleId = vehicleId, FromStatus = VehicleStatus.ReadyForSale, ToStatus = VehicleStatus.Listed,
                ChangedAt = now, ChangedByUserId = null
            });
            vehicle.Status = VehicleStatus.Listed;
            try
            {
                await unitOfWork.SaveChanges(token);
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_VehicleListings_VehicleId" })
            {
                throw Conflict("alreadyListed", "The vehicle already has an active listing.");
            }
            return (await LoadCurrentListing(vehicleId, token))!;
        }, cancellationToken);
    }

    public async Task<VehicleListingDto?> GetCurrentListing(int vehicleId, CancellationToken cancellationToken)
    {
        await LoadVehicle(vehicleId, cancellationToken);
        return await LoadCurrentListing(vehicleId, cancellationToken);
    }

    private Task<VehicleListingDto?> LoadCurrentListing(int vehicleId, CancellationToken token)
        => listings.GetQueryByProperties(l => l.VehicleId == vehicleId && l.IsActive)
            .Select(ListingProjection).SingleOrDefaultAsync(token);

    private static readonly Expression<Func<VehicleListing, VehicleListingDto>> ListingProjection = l => new()
    {
        VehicleId = l.VehicleId, ListingId = l.Id, ListingPrice = l.ListingPrice,
        ListedAt = l.ListedAt, Status = l.Vehicle.Status
    };
}
