#nullable enable
using DealerManager.Application.Dtos.Candidate;
using DealerManager.Application.FilterDtos;
using DealerManager.Application.FilterDtos.Candidate;
using DealerManager.Application.IRepository;
using DealerManager.Application.IService.Candidate;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using CandidateEntity = DealerManager.Domain.Entities.Candidate;

namespace DealerManager.Infrastructure.Service.Candidate
{
    public class CandidateService : ICandidateService
    {
        private readonly IBaseRepository<CandidateEntity, CandidateFilterDto, DealerManagerDbContext> candidates;
        private readonly IBaseRepository<CandidateEstimate, FilterDto<CandidateEstimate>, DealerManagerDbContext> estimates;
        private readonly IUnitOfWork unitOfWork;
        private readonly ICandidateFinancialCalculator calculator;
        private readonly TimeProvider timeProvider;

        public CandidateService(
            IBaseRepository<CandidateEntity, CandidateFilterDto, DealerManagerDbContext> candidates,
            IBaseRepository<CandidateEstimate, FilterDto<CandidateEstimate>, DealerManagerDbContext> estimates,
            IUnitOfWork unitOfWork,
            ICandidateFinancialCalculator calculator,
            TimeProvider timeProvider)
        {
            this.candidates = candidates;
            this.estimates = estimates;
            this.unitOfWork = unitOfWork;
            this.calculator = calculator;
            this.timeProvider = timeProvider;
        }

        public async Task<CandidateDetailsDto> CreateCandidate(CreateCandidateRequest request, CancellationToken cancellationToken)
        {
            Validate(request);
            var now = timeProvider.GetUtcNow();
            if (request.Year is { } year && (year < 1886 || year > now.Year + 1))
                throw new ValidationException($"Year must be between 1886 and {now.Year + 1}.");

            cancellationToken.ThrowIfCancellationRequested();
            var candidate = new CandidateEntity
            {
                Make = request.Make.Trim(),
                Model = request.Model.Trim(),
                Year = request.Year,
                Mileage = request.Mileage,
                Vin = Normalize(request.Vin),
                Source = Normalize(request.Source),
                Location = Normalize(request.Location),
                ExpectedSellingPrice = request.ExpectedSellingPrice,
                Notes = Normalize(request.Notes),
                Status = CandidateStatus.UnderReview,
                CreatedAt = now,
                RejectedAt = null,
                PurchasedAt = null
            };

            await candidates.Create(candidate);
            await unitOfWork.SaveChanges(cancellationToken);
            return ToDetails(candidate);
        }

        public async Task<CandidateListResultDto> GetCandidates(CandidateFilterDto filter, CancellationToken cancellationToken)
        {
            filter ??= new CandidateFilterDto();
            if (filter.Offset < 0 || (!filter.GetAllData && (filter.Limit < 1 || filter.Limit > 500)))
                throw new ValidationException("Offset must be non-negative and Limit must be between 1 and 500.");

            var (entities, totalCount) = await candidates.GetAll(
                filter, cancellationToken,
                orderBy: query => query.OrderByDescending(candidate => candidate.Id));

            // Load only the latest estimates for the page in one batch (no N+1 or full histories).
            var candidateIds = entities.Select(candidate => candidate.Id).ToList();
            var estimateQuery = estimates.GetQueryByProperties(_ => true);
            var latestEstimates = candidateIds.Count == 0 ? [] : await estimates.GetListByProperties(
                estimate => candidateIds.Contains(estimate.CandidateId)
                    && !estimateQuery.Any(newer => newer.CandidateId == estimate.CandidateId
                        && (newer.Version > estimate.Version || (newer.Version == estimate.Version && newer.Id > estimate.Id))),
                cancellationToken, query => query.Include(estimate => estimate.CandidateEstimateItems));
            var latestByCandidate = latestEstimates.ToDictionary(estimate => estimate.CandidateId);

            return new CandidateListResultDto
            {
                Items = entities.Select(candidate => ToListDto(candidate, latestByCandidate.GetValueOrDefault(candidate.Id))).ToList(),
                TotalCount = totalCount
            };
        }

        public async Task<CandidateDetailsDto> GetCandidateDetails(int id, CancellationToken cancellationToken)
        {
            var candidate = await LoadCandidate(id, cancellationToken);
            return ToDetails(candidate);
        }

        public Task<CandidateEstimateDto> CreateCandidateEstimate(CreateCandidateEstimateRequest request, CancellationToken cancellationToken)
        {
            Validate(request);
            foreach (var item in request.Items)
                Validate(item);

            // Validate aggregate arithmetic before anything is tracked or written.
            calculator.Calculate(request.ExpectedSellingPrice, request.Items.Select(item => item.EstimatedAmount));

            return unitOfWork.ExecuteInTransaction(async token =>
            {
                var candidate = await candidates.GetById(request.CandidateId, token, query => query.AsTracking())
                    ?? throw new CandidateNotFoundException(request.CandidateId);
                if (candidate.Status is CandidateStatus.Purchased or CandidateStatus.Rejected)
                    throw new CandidateConflictException("Cannot add an estimate to a purchased or rejected candidate.");

                var highestVersion = await estimates.GetQueryByProperties(estimate => estimate.CandidateId == request.CandidateId)
                    .Select(estimate => (int?)estimate.Version).MaxAsync(token) ?? 0;
                if (highestVersion == int.MaxValue)
                    throw new CandidateConflictException("No further estimate versions can be created.");

                var estimate = new CandidateEstimate
                {
                    CandidateId = request.CandidateId,
                    Version = highestVersion + 1,
                    ExpectedSellingPrice = request.ExpectedSellingPrice,
                    Notes = Normalize(request.Notes),
                    CreatedAt = timeProvider.GetUtcNow(),
                    IsDecisionSnapshot = false,
                    CandidateEstimateItems = request.Items.Select(item => new CandidateEstimateItem
                    {
                        Category = item.Category,
                        Description = item.Description.Trim(),
                        EstimatedAmount = item.EstimatedAmount
                    }).ToList()
                };

                // Leave inverse navigations unset until AddAsync performs EF relationship fixup.
                // BaseRepository.Create traverses the new graph before tracking it.
                await estimates.Create(estimate);
                candidate.ExpectedSellingPrice = request.ExpectedSellingPrice;
                candidates.SetEntryModified(candidate);
                await unitOfWork.SaveChanges(token);
                return ToEstimateDto(estimate);
            }, cancellationToken);
        }

        public async Task<IReadOnlyList<CandidateEstimateDto>> GetEstimateHistory(int candidateId, CancellationToken cancellationToken)
        {
            var candidate = await LoadCandidate(candidateId, cancellationToken);
            return candidate.CandidateEstimates.OrderByDescending(estimate => estimate.Version)
                .ThenByDescending(estimate => estimate.Id).Select(ToEstimateDto).ToList();
        }

        public Task<CandidateDetailsDto> ApproveCandidate(int id, CancellationToken cancellationToken)
        {
            return unitOfWork.ExecuteInTransaction(async token =>
            {
                var candidate = await LoadCandidate(id, token, tracking: true);
                if (candidate.Status is CandidateStatus.Purchased or CandidateStatus.Rejected)
                    throw new CandidateConflictException("A purchased or rejected candidate cannot be approved.");
                if (candidate.CandidateEstimates.Count == 0)
                    throw new CandidateConflictException("At least one estimate is required before approval.");

                if (candidate.Status != CandidateStatus.Approved)
                {
                    candidate.Status = CandidateStatus.Approved;
                    candidates.SetEntryModified(candidate);
                    await unitOfWork.SaveChanges(token);
                }

                // Approval never changes historical estimates or chooses a decision snapshot.
                return ToDetails(candidate);
            }, cancellationToken);
        }

        public Task<CandidateDetailsDto> RejectCandidate(int id, RejectCandidateRequest request, CancellationToken cancellationToken)
        {
            Validate(request);
            return unitOfWork.ExecuteInTransaction(async token =>
            {
                var candidate = await LoadCandidate(id, token, tracking: true);
                if (candidate.Status == CandidateStatus.Purchased)
                    throw new CandidateConflictException("A purchased candidate cannot be rejected.");

                if (candidate.Status != CandidateStatus.Rejected)
                {
                    candidate.Status = CandidateStatus.Rejected;
                    candidate.RejectedAt = timeProvider.GetUtcNow();
                    var reason = Normalize(request.Reason);
                    if (reason != null)
                    {
                        var rejectionNote = $"Rejection reason: {reason}";
                        candidate.Notes = string.IsNullOrWhiteSpace(candidate.Notes)
                            ? rejectionNote : $"{candidate.Notes}\n{rejectionNote}";
                    }

                    candidates.SetEntryModified(candidate);
                    await unitOfWork.SaveChanges(token);
                }

                return ToDetails(candidate);
            }, cancellationToken);
        }

        private async Task<CandidateEntity> LoadCandidate(int id, CancellationToken cancellationToken, bool tracking = false)
        {
            return await candidates.GetById(id, cancellationToken, query =>
                (tracking ? query.AsTracking() : query)
                    .Include(candidate => candidate.CandidateEstimates)
                    .ThenInclude(estimate => estimate.CandidateEstimateItems))
                ?? throw new CandidateNotFoundException(id);
        }

        private CandidateListDto ToListDto(CandidateEntity candidate, CandidateEstimate? latest)
        {
            var analysis = latest == null ? null : calculator.Calculate(latest.ExpectedSellingPrice,
                latest.CandidateEstimateItems.Select(item => item.EstimatedAmount));
            return new CandidateListDto
            {
                Id = candidate.Id, Make = candidate.Make, Model = candidate.Model,
                Year = candidate.Year, Mileage = candidate.Mileage, Status = candidate.Status,
                ExpectedSellingPrice = candidate.ExpectedSellingPrice, CreatedAt = candidate.CreatedAt,
                EstimatedTotalCost = analysis?.EstimatedTotalCost,
                ExpectedProfit = analysis?.ExpectedProfit, ExpectedRoi = analysis?.ExpectedRoi
            };
        }

        private CandidateDetailsDto ToDetails(CandidateEntity candidate)
        {
            var history = candidate.CandidateEstimates.OrderByDescending(estimate => estimate.Version)
                .ThenByDescending(estimate => estimate.Id).Select(ToEstimateDto).ToList();
            var latest = history.FirstOrDefault();
            return new CandidateDetailsDto
            {
                Id = candidate.Id, Make = candidate.Make, Model = candidate.Model,
                Year = candidate.Year, Mileage = candidate.Mileage, Status = candidate.Status,
                ExpectedSellingPrice = candidate.ExpectedSellingPrice, CreatedAt = candidate.CreatedAt,
                Vin = candidate.Vin, Source = candidate.Source, Location = candidate.Location,
                Notes = candidate.Notes, RejectedAt = candidate.RejectedAt, PurchasedAt = candidate.PurchasedAt,
                EstimateHistory = history, LatestEstimate = latest,
                EstimatedTotalCost = latest?.FinancialAnalysis.EstimatedTotalCost,
                ExpectedProfit = latest?.FinancialAnalysis.ExpectedProfit,
                ExpectedRoi = latest?.FinancialAnalysis.ExpectedRoi
            };
        }

        private CandidateEstimateDto ToEstimateDto(CandidateEstimate estimate)
        {
            return new CandidateEstimateDto
            {
                Id = estimate.Id, CandidateId = estimate.CandidateId, Version = estimate.Version,
                ExpectedSellingPrice = estimate.ExpectedSellingPrice, Notes = estimate.Notes,
                CreatedAt = estimate.CreatedAt, IsDecisionSnapshot = estimate.IsDecisionSnapshot,
                Items = estimate.CandidateEstimateItems.OrderBy(item => item.Id).Select(item => new CandidateEstimateItemDto
                {
                    Id = item.Id, Category = item.Category, Description = item.Description,
                    EstimatedAmount = item.EstimatedAmount
                }).ToList(),
                FinancialAnalysis = calculator.Calculate(estimate.ExpectedSellingPrice,
                    estimate.CandidateEstimateItems.Select(item => item.EstimatedAmount))
            };
        }

        private static void Validate(object? request)
        {
            if (request == null)
                throw new ValidationException("A request and its items must not be null.");
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        }

        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
