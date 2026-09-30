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
            if (vehicle.Status != VehicleStatus.Listed)
                throw Conflict("notListedForSale", "Only a Listed vehicle can be sold.");
            var listing = await listings.GetByProperties(l => l.VehicleId == vehicleId && l.IsActive, token, q => q.AsTracking())
                ?? throw Conflict("activeListingRequired", "An active listing is required for sale.");
            var account = request.CapitalAccountId is { } accountId
                ? await accounts.GetById(accountId, token) ?? throw Conflict("saleAccountMissing", "The sale account does not exist.")
                : await PurchaseAccount(vehicleId, token);
            if (!account.IsActive) throw Conflict("inactive", "The capital account is inactive.");
            if (!string.Equals(account.Currency.Trim(), "EUR", StringComparison.OrdinalIgnoreCase))
                throw Conflict("currency", "Vehicle sales require an EUR account.");
            var purchase = await transactions.GetQueryByProperties(t => t.VehicleId == vehicleId
                && t.Type == TransactionType.VehiclePurchase && t.Direction == TransactionDirection.Out)
                .SumAsync(t => (decimal?)t.Amount, token) ?? 0m;
            var spent = await expenses.GetQueryByProperties(e => e.VehicleId == vehicleId).SumAsync(e => (decimal?)e.Amount, token) ?? 0m;
            decimal invested;
            try { invested = purchase + spent; }
            catch (OverflowException) { throw new ValidationException("Financial amounts exceed the supported decimal range."); }
            var realized = CalculateRealized(invested, request.ActualSalePrice);
            var now = clock.GetUtcNow();
            var soldAt = request.SoldAt?.ToUniversalTime() ?? now;
            var movement = new FinancialTransaction
            {
                CapitalAccountId = account.Id, VehicleId = vehicleId, Type = TransactionType.VehicleSale,
                Direction = TransactionDirection.In, Amount = request.ActualSalePrice,
                Description = $"Sale of {vehicle.Make} {vehicle.Model}", OccurredAt = soldAt, CreatedAt = now
            };
            await transactions.Create(movement);
            // Obtain the FK; both saves remain inside the same serializable transaction.
            await unitOfWork.SaveChanges(token);
            var sale = new VehicleSale
            {
                VehicleId = vehicleId, SalePrice = request.ActualSalePrice, SoldAt = soldAt,
                FinancialTransactionId = movement.Id, CustomerId = null, Notes = null
            };
            await sales.Create(sale);
            listing.IsActive = false;
            vehicle.Status = VehicleStatus.Sold;
            await statusHistory.Create(new VehicleStatusHistory
            {
                VehicleId = vehicleId, FromStatus = VehicleStatus.Listed, ToStatus = VehicleStatus.Sold,
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
