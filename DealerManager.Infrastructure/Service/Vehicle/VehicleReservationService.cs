#nullable enable
using AutoMapper.QueryableExtensions;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.IService.Vehicle;
using DealerManager.Application.IService.Finance;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.ComponentModel.DataAnnotations;
namespace DealerManager.Infrastructure.Service.Vehicle;
public partial class VehicleService
{
    public Task<VehicleReservationDto> ReserveVehicle(int vehicleId, ReserveVehicleRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            var vehicle = await vehicles.GetById(vehicleId, token, q => q.AsTracking())
                ?? throw new VehicleNotFoundException(vehicleId);
            if (vehicle.Status != VehicleStatus.Listed)
                throw Conflict("notListedForReservation", "Only a listed vehicle can be reserved.");
            var listing = await listings.GetByProperties(l => l.VehicleId == vehicleId && l.IsActive, token)
                ?? throw Conflict("activeListingRequired", "An active listing is required.");
            if (await reservations.AnyEntity(r => r.VehicleId == vehicleId && r.Status == VehicleReservationStatus.Active, token))
                throw Conflict("activeReservationExists", "The vehicle already has an active reservation.");
            if (request.DepositAmount > listing.ListingPrice)
                throw Conflict("depositExceedsPrice", "The deposit cannot exceed the listing price.");
            var account = await accounts.GetById(request.CapitalAccountId, token)
                ?? throw new CapitalAccountNotFoundException(request.CapitalAccountId);
            if (!account.IsActive) throw Conflict("inactive", "The capital account is inactive.");
            if (!string.Equals(account.Currency.Trim(), "EUR", StringComparison.OrdinalIgnoreCase))
                throw Conflict("currency", "Deposits require an EUR account.");
            var now = clock.GetUtcNow();
            var reservedAt = request.ReservedAt?.ToUniversalTime() ?? now;
            if (request.ExpiresAt is { } expires && expires < reservedAt)
                throw new ValidationException("Expiry must not precede the reservation.");
            var deposit = new FinancialTransaction {
                CapitalAccountId = account.Id, VehicleId = vehicleId, Type = TransactionType.VehicleDeposit,
                Direction = TransactionDirection.In, Amount = request.DepositAmount,
                Description = $"Капаро за {vehicle.Make} {vehicle.Model}", OccurredAt = reservedAt, CreatedAt = now
            };
            await transactions.Create(deposit);
            await unitOfWork.SaveChanges(token);
            var reservation = new VehicleReservation {
                VehicleId = vehicleId, CustomerName = request.CustomerName.Trim(),
                CustomerPhone = Normalize(request.CustomerPhone), CustomerEmail = Normalize(request.CustomerEmail),
                DepositAmount = request.DepositAmount, DepositTransactionId = deposit.Id,
                ReservedAt = reservedAt, ExpiresAt = request.ExpiresAt?.ToUniversalTime(),
                Status = VehicleReservationStatus.Active, Notes = Normalize(request.Notes), CreatedAt = now
            };
            await reservations.Create(reservation);
            vehicle.Status = VehicleStatus.Reserved;
            await statusHistory.Create(new VehicleStatusHistory {
                VehicleId = vehicleId, FromStatus = VehicleStatus.Listed, ToStatus = VehicleStatus.Reserved,
                ChangedAt = reservedAt, Notes = Normalize(request.Notes)
            });
            try { await unitOfWork.SaveChanges(token); }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_VehicleReservations_VehicleId" })
            { throw Conflict("activeReservationExists", "The vehicle already has an active reservation."); }
            return await ReservationDto(reservation.Id, token);
        }, cancellationToken);
    }

    public Task<VehicleReservationDto> CancelReservation(int vehicleId, int reservationId, CancelReservationRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            var vehicle = await vehicles.GetById(vehicleId, token, q => q.AsTracking())
                ?? throw new VehicleNotFoundException(vehicleId);
            var reservation = await reservations.GetByProperties(r => r.Id == reservationId && r.VehicleId == vehicleId, token,
                q => q.AsTracking().Include(r => r.DepositTransaction))
                ?? throw new VehicleReservationNotFoundException(reservationId);
            if (reservation.Status != VehicleReservationStatus.Active || vehicle.Status != VehicleStatus.Reserved)
                throw Conflict("reservationNotActive", "Only an active reservation can be cancelled.");
            if (!await listings.AnyEntity(l => l.VehicleId == vehicleId && l.IsActive, token))
                throw Conflict("activeListingRequired", "An active listing is required.");
            var now = clock.GetUtcNow();
            var cancelledAt = request.CancelledAt?.ToUniversalTime() ?? now;
            if (cancelledAt < reservation.ReservedAt) throw new ValidationException("Cancellation cannot precede reservation.");
            if (request.RefundDeposit == true)
            {
                var accountId = reservation.DepositTransaction.CapitalAccountId;
                if (await finance.GetBalance(accountId, token) < reservation.DepositAmount)
                    throw Conflict("insufficientCapital", "Insufficient capital to refund the deposit.");
                var refund = new FinancialTransaction {
                    CapitalAccountId = accountId, VehicleId = vehicleId, Type = TransactionType.DepositRefund,
                    Direction = TransactionDirection.Out, Amount = reservation.DepositAmount,
                    Description = $"Връщане на капаро за {vehicle.Make} {vehicle.Model}", OccurredAt = cancelledAt, CreatedAt = now
                };
                await transactions.Create(refund);
                await unitOfWork.SaveChanges(token);
                reservation.RefundTransactionId = refund.Id;
            }
            reservation.Status = VehicleReservationStatus.Cancelled;
            reservation.CancelledAt = cancelledAt;
            reservation.CancellationNotes = Normalize(request.Notes);
            vehicle.Status = VehicleStatus.Listed;
            await statusHistory.Create(new VehicleStatusHistory {
                VehicleId = vehicleId, FromStatus = VehicleStatus.Reserved, ToStatus = VehicleStatus.Listed,
                ChangedAt = cancelledAt, Notes = Normalize(request.Notes)
            });
            await unitOfWork.SaveChanges(token);
            return await ReservationDto(reservation.Id, token);
        }, cancellationToken);
    }
    public async Task<VehicleReservationDto?> GetActiveReservation(int vehicleId, CancellationToken cancellationToken)
    {
        await LoadVehicle(vehicleId, cancellationToken);
        return await LoadActiveReservation(vehicleId, cancellationToken);
    }
    private Task<VehicleReservationDto?> LoadActiveReservation(int vehicleId, CancellationToken token)
        => reservations.GetQueryByProperties(r => r.VehicleId == vehicleId && r.Status == VehicleReservationStatus.Active)
            .ProjectTo<VehicleReservationDto>(mapper.ConfigurationProvider).SingleOrDefaultAsync(token);
    private Task<VehicleReservationDto> ReservationDto(int id, CancellationToken token)
        => reservations.GetQueryByProperties(r => r.Id == id).ProjectTo<VehicleReservationDto>(mapper.ConfigurationProvider).SingleAsync(token);
    public async Task<IReadOnlyList<VehicleReservationDto>> GetReservations(int vehicleId, CancellationToken cancellationToken)
    {
        await LoadVehicle(vehicleId, cancellationToken);
        return await reservations.GetQueryByProperties(r => r.VehicleId == vehicleId)
            .OrderByDescending(r => r.ReservedAt).ThenByDescending(r => r.Id)
            .ProjectTo<VehicleReservationDto>(mapper.ConfigurationProvider).ToListAsync(cancellationToken);
    }
}
