#nullable enable
using AutoMapper.QueryableExtensions;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DealerManager.Infrastructure.Service.Vehicle;
public partial class VehicleService
{
    public Task<VehicleExpenseDto> ReverseExpense(int vehicleId, int expenseId, ReverseVehicleExpenseRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new ValidationException("A reason is required.");
        return unitOfWork.ExecuteInTransaction(async token => {
            var original = await ExpenseForCorrection(vehicleId, expenseId, token);
            await Reverse(original, request.Reason, token);
            return await ExpenseDto(original.Id, token);
        }, cancellationToken);
    }

    public Task<VehicleExpenseDto> CorrectExpense(int vehicleId, int expenseId, CorrectVehicleExpenseRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        if (string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(request.Description))
            throw new ValidationException("Description and correction reason are required.");
        return unitOfWork.ExecuteInTransaction(async token => {
            var original = await ExpenseForCorrection(vehicleId, expenseId, token);
            var accountId = original.FinancialTransaction.CapitalAccountId;
            if (request.CapitalAccountId is { } requested && requested != accountId)
                throw Conflict("purchaseAccountMismatch", "A correction must use the original account.");
            if (request.CostPlanItemId is { } planId && planId != original.CostPlanItemId)
                throw Conflict("planMismatch", "A correction must retain the original cost plan link.");
            var account = await accounts.GetById(accountId, token);
            if (account == null || !account.IsActive) throw Conflict("inactive", "The original account is inactive.");
            if (original.CostPlanItemId.HasValue && request.Category != original.Category)
                throw Conflict("planCategoryLocked", "A planned expense must retain its category.");
            await Reverse(original, request.Reason, token, original.FinancialTransaction.OccurredAt);
            // Reversal and replacement are atomic. Any failure rolls both back.
            var replacement = await RecordExpense(vehicleId, accountId, original.CostPlanItemId,
                request.Category, request.Description, request.Amount, request.PaidAt ?? original.PaidAt,
                request.Supplier, request.DocumentNumber, token);
            original.ReplacementExpenseId = replacement.Id;
            await unitOfWork.SaveChanges(token);
            return await ExpenseDto(original.Id, token);
        }, cancellationToken);
    }

    private async Task<VehicleExpense> ExpenseForCorrection(int vehicleId, int expenseId, CancellationToken token)
    {
        await LoadVehicle(vehicleId, token); // Corrections are permitted for sold vehicles too.
        var original = await expenses.GetByProperties(e => e.Id == expenseId && e.VehicleId == vehicleId, token,
            q => q.AsTracking().Include(e => e.FinancialTransaction))
            ?? throw Conflict("expenseNotFound", "Expense not found for this vehicle.");
        if (original.IsReversed) throw Conflict("expenseAlreadyReversed", "This expense has already been reversed or corrected.");
        var payment = original.FinancialTransaction;
        if (payment.VehicleId != vehicleId || payment.Type != TransactionType.VehicleExpense
            || payment.Direction != TransactionDirection.Out || payment.Amount != original.Amount)
            throw Conflict("expensePaymentMismatch", "The expense does not match its original payment.");
        return original;
    }

    private async Task Reverse(VehicleExpense original, string reason, CancellationToken token, DateTimeOffset? occurredAt = null)
    {
        var now = clock.GetUtcNow();
        var reversal = new FinancialTransaction {
            VehicleId = original.VehicleId, CapitalAccountId = original.FinancialTransaction.CapitalAccountId,
            Type = TransactionType.VehicleExpense, Direction = TransactionDirection.In, Amount = original.Amount,
            Description = $"Reversal of expense #{original.Id}: {reason.Trim()}", OccurredAt = occurredAt ?? now, CreatedAt = now
        };
        await transactions.Create(reversal);
        await unitOfWork.SaveChanges(token);
        original.IsReversed = true; original.ReversedAt = now; original.ReversalReason = reason.Trim();
        original.ReversedByUserId = currentUser.UserId; original.ReversalTransactionId = reversal.Id;
        await unitOfWork.SaveChanges(token);
    }
    private Task<VehicleExpenseDto> ExpenseDto(int id, CancellationToken token)
        => expenses.GetQueryByProperties(e => e.Id == id).ProjectTo<VehicleExpenseDto>(mapper.ConfigurationProvider).SingleAsync(token);
}
