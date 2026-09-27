using DealerManager.Application.Dtos.Candidate;
using DealerManager.Application.FilterDtos;
using DealerManager.Application.FilterDtos.Candidate;
using DealerManager.Application.IRepository;
using DealerManager.Application.IService.Candidate;
using DealerManager.Application.IService.Finance;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using CandidateEntity = DealerManager.Domain.Entities.Candidate;
using VehicleEntity = DealerManager.Domain.Entities.Vehicle;

namespace DealerManager.Infrastructure.Service.Candidate;

public class PurchaseCandidateService(
    IBaseRepository<CandidateEntity, CandidateFilterDto, DealerManagerDbContext> candidates,
    IBaseRepository<VehicleEntity, FilterDto<VehicleEntity>, DealerManagerDbContext> vehicles,
    IBaseRepository<CapitalAccount, FilterDto<CapitalAccount>, DealerManagerDbContext> accounts,
    IBaseRepository<FinancialTransaction, FilterDto<FinancialTransaction>, DealerManagerDbContext> transactions,
    ICapitalAccountService finance, IUnitOfWork unitOfWork, TimeProvider clock) : IPurchaseCandidateService
{
    public Task<PurchaseCandidateResultDto> PurchaseCandidate(int candidateId, PurchaseCandidateRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validator.ValidateObject(request, new ValidationContext(request), true);
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            var candidate = await candidates.GetById(candidateId, token, query => query.AsTracking()
                .Include(c => c.CandidateEstimates).ThenInclude(e => e.CandidateEstimateItems))
                ?? throw new CandidateNotFoundException(candidateId);
            if (candidate.Status != CandidateStatus.Approved)
                throw Conflict("status", "Only an approved candidate can be purchased.");
            if (await vehicles.AnyEntity(v => v.SourceCandidateId == candidateId, token))
                throw Conflict("alreadyPurchased", "A vehicle already exists for this candidate.");
            var estimate = candidate.CandidateEstimates.OrderByDescending(e => e.Version).ThenByDescending(e => e.Id).FirstOrDefault()
                ?? throw Conflict("estimate", "A financial estimate is required before purchase.");
            if (candidate.CandidateEstimates.Any(e => e.IsDecisionSnapshot))
                throw Conflict("snapshot", "A decision snapshot already exists. Historical estimates cannot be replaced.");
            if (candidate.Year is not { } year || year < 1886 || year > clock.GetUtcNow().Year + 1)
                throw Conflict("year", "A valid candidate year is required before purchase.");
            var account = await accounts.GetById(request.CapitalAccountId, token)
                ?? throw new CapitalAccountNotFoundException(request.CapitalAccountId);
            if (!account.IsActive) throw Conflict("inactive", "The capital account is inactive.");
            // Vehicle estimates currently use EUR throughout the application.
            if (!string.Equals(account.Currency.Trim(), "EUR", StringComparison.OrdinalIgnoreCase))
                throw Conflict("currency", "Vehicle purchases require an EUR capital account.");
            var previousBalance = await finance.GetBalance(account.Id, token);
            if (previousBalance < request.ActualPurchasePrice)
                throw Conflict("insufficientCapital", "Insufficient available capital for this purchase.");

            var now = clock.GetUtcNow();
            var purchaseDate = request.PurchaseDate?.ToUniversalTime() ?? now;
            estimate.IsDecisionSnapshot = true;
            candidate.Status = CandidateStatus.Purchased;
            candidate.PurchasedAt = purchaseDate;
            var vehicle = new VehicleEntity
            {
                SourceCandidateId = candidate.Id, Make = candidate.Make, Model = candidate.Model,
                Year = year, Mileage = candidate.Mileage, Vin = candidate.Vin,
                Status = VehicleStatus.Purchased, PurchaseDate = purchaseDate, CreatedAt = now,
                VehicleCostPlanItems = estimate.CandidateEstimateItems.Where(i => i.Category != CostCategory.Purchase)
                    .Select(i => new VehicleCostPlanItem
                    {
                        Category = i.Category, Description = i.Description, CurrentEstimatedAmount = i.EstimatedAmount,
                        CommittedAmount = null, IsCancelled = false, CreatedAt = now
                    }).ToList()
            };
            // Keep inverse navigations unset when passing a new graph to the existing repository.
            await vehicles.Create(vehicle);
            await unitOfWork.SaveChanges(token);
            await transactions.Create(new FinancialTransaction
            {
                CapitalAccountId = account.Id, VehicleId = vehicle.Id,
                Type = TransactionType.VehiclePurchase, Direction = TransactionDirection.Out,
                Amount = request.ActualPurchasePrice, Description = $"Purchase of {vehicle.Make} {vehicle.Model}",
                OccurredAt = purchaseDate, CreatedAt = now
            });
            await unitOfWork.SaveChanges(token);
            return new PurchaseCandidateResultDto
            {
                CandidateId = candidate.Id, VehicleId = vehicle.Id, CapitalAccountId = account.Id,
                ActualPurchasePrice = request.ActualPurchasePrice, PurchaseDate = purchaseDate,
                PreviousBalance = previousBalance, RemainingBalance = await finance.GetBalance(account.Id, token)
            };
        }, cancellationToken);
    }
    private static PurchaseCandidateException Conflict(string code, string message) => new(code, message);
}
