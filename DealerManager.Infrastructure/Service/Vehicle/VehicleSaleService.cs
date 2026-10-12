using DealerManager.Infrastructure.Service.Finance;
#nullable enable
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.IService.Vehicle;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.ComponentModel.DataAnnotations;

namespace DealerManager.Infrastructure.Service.Vehicle;
public partial class VehicleService
{
    public Task<VehicleSaleResultDto> SellVehicle(int vehicleId, SellVehicleRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            var vehicle = await vehicles.GetById(vehicleId, token, q => q.AsTracking())
                ?? throw new VehicleNotFoundException(vehicleId);
            if (vehicle.Status == VehicleStatus.Sold || await sales.AnyEntity(s => s.VehicleId == vehicleId, token))
                throw Conflict("alreadySold", "The vehicle has already been sold.");
            if (vehicle.Status is not (VehicleStatus.Listed or VehicleStatus.Reserved))
                throw Conflict("notListedForSale", "Only a Listed or Reserved vehicle can be sold.");
            var listing = await listings.GetByProperties(l => l.VehicleId == vehicleId && l.IsActive, token, q => q.AsTracking())
                ?? throw Conflict("activeListingRequired", "An active listing is required for sale.");
            var reservation = await reservations.GetByProperties(r => r.VehicleId == vehicleId && r.Status == VehicleReservationStatus.Active,
                token, q => q.AsTracking().Include(r => r.DepositTransaction));
            if ((vehicle.Status == VehicleStatus.Reserved) != (reservation != null))
                throw Conflict("reservationNotActive", "Vehicle status and active reservation do not match.");
            if (reservation != null && request.ActualSalePrice < reservation.DepositAmount)
                throw Conflict("saleBelowDeposit", "Sale price cannot be less than the active deposit.");
            var finalPayment = request.ActualSalePrice - (reservation?.DepositAmount ?? 0m);
            var account = request.CapitalAccountId is { } accountId
                ? await accounts.GetById(accountId, token) ?? throw new DealerManager.Application.IService.Finance.CapitalAccountNotFoundException(accountId)
                : reservation != null
                    ? await accounts.GetById(reservation.DepositTransaction.CapitalAccountId, token)
                        ?? throw new DealerManager.Application.IService.Finance.CapitalAccountNotFoundException(reservation.DepositTransaction.CapitalAccountId)
                    : await PurchaseAccount(vehicleId, token);
            if (!account.IsActive) throw Conflict("inactive", "The capital account is inactive.");
            if (!string.Equals(account.Currency.Trim(), "EUR", StringComparison.OrdinalIgnoreCase))
                throw Conflict("currency", "Vehicle sales require an EUR account.");
            var invested = await FinancialQueries.Investments(vehicles.GetQueryByProperties(v => v.Id == vehicleId),
                transactions.GetQueryByProperties(_ => true)).Select(v => v.TotalInvested).SingleAsync(token);
            var realized = CalculateRealized(invested, request.ActualSalePrice);
            var now = clock.GetUtcNow();
            var soldAt = request.SoldAt?.ToUniversalTime() ?? now;
            if (reservation != null && soldAt.UtcDateTime.Date < reservation.ReservedAt.UtcDateTime.Date)
                throw new ValidationException("Sale cannot precede the reservation.");
            FinancialTransaction? movement = null;
            if (finalPayment > 0)
            {
                movement = new FinancialTransaction
                {
                    CapitalAccountId = account.Id, VehicleId = vehicleId, Type = TransactionType.VehicleSale,
                    Direction = TransactionDirection.In, Amount = finalPayment,
                    Description = $"Sale of {vehicle.Make} {vehicle.Model}", OccurredAt = soldAt, CreatedAt = now
                };
                await transactions.Create(movement);
                // Obtain the FK; both saves remain inside the same serializable transaction.
                await unitOfWork.SaveChanges(token);
            }
            var sale = new VehicleSale
            {
                VehicleId = vehicleId, SalePrice = request.ActualSalePrice, SoldAt = soldAt,
                FinancialTransactionId = movement?.Id, ReservationId = reservation?.Id, CustomerId = null, Notes = null
            };
            await sales.Create(sale);
            listing.IsActive = false;
            var previousStatus = vehicle.Status;
            vehicle.Status = VehicleStatus.Sold;
            if (reservation != null) { reservation.Status = VehicleReservationStatus.Completed; reservation.CompletedAt = soldAt; }
            await statusHistory.Create(new VehicleStatusHistory
            {
                VehicleId = vehicleId, FromStatus = previousStatus, ToStatus = VehicleStatus.Sold,
                ChangedAt = soldAt, ChangedByUserId = null, Notes = null
            });
            try { await unitOfWork.SaveChanges(token); }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_VehicleSales_VehicleId" })
            { throw Conflict("alreadySold", "The vehicle has already been sold."); }
            return new VehicleSaleResultDto
            {
                VehicleId = vehicleId, SaleId = sale.Id, ActualSalePrice = sale.SalePrice, SoldAt = soldAt,
                CapitalAccountId = account.Id, NewCapitalBalance = await finance.GetBalance(account.Id, token),
                TotalInvested = invested, RealizedProfit = realized.Profit!.Value, RealizedROI = realized.ROI!.Value,
                Status = VehicleStatus.Sold
            };
        }, cancellationToken);
    }
    private static (decimal? Profit, decimal? ROI) CalculateRealized(decimal invested, decimal? salePrice)
    {
        if (salePrice is null) return (null, null);
        try
        {
            var profit = salePrice.Value - invested;
            return (profit, invested == 0 ? 0 : profit / invested * 100m);
        }
        catch (OverflowException) { throw new ValidationException("Financial amounts exceed the supported decimal range."); }
    }
}
