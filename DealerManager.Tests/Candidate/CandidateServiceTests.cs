using DealerManager.Application.Dtos.Candidate;
using DealerManager.Application.FilterDtos.Candidate;
using DealerManager.Application.IService.Candidate;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace DealerManager.Tests.Candidate;

public class CandidateServiceTests
{
    private static readonly CancellationToken Token = CancellationToken.None;

    private static CreateCandidateRequest NewCandidate() => new()
    {
        Make = " BMW ", Model = " X1 ", Year = 2013, Mileage = 142000,
        ExpectedSellingPrice = 7000, Notes = "Original evaluation"
    };

    private static CreateCandidateEstimateRequest Estimate(int candidateId) => new()
    {
        CandidateId = candidateId, ExpectedSellingPrice = 7000, Notes = "First estimate",
        Items =
        [
            new() { Category = CostCategory.Purchase, Description = " Purchase ", EstimatedAmount = 3000 },
            new() { Category = CostCategory.Transport, Description = "Transport", EstimatedAmount = 660 },
            new() { Category = CostCategory.Repair, Description = "Repair", EstimatedAmount = 1200 },
            new() { Category = CostCategory.Cleaning, Description = "Cleaning", EstimatedAmount = 120 },
            new() { Category = CostCategory.Documents, Description = "Documents", EstimatedAmount = 200 }
        ]
    };

    [Fact]
    public async Task CreateSetsBackendFieldsAndAllowsNoEstimate()
    {
        using var store = new CandidateTestStore();
        var candidate = await store.Service.CreateCandidate(NewCandidate(), Token);

        Assert.True(candidate.Id > 0);
        Assert.Equal("BMW", candidate.Make);
        Assert.Equal("X1", candidate.Model);
        Assert.Equal(CandidateStatus.UnderReview, candidate.Status);
        Assert.Equal(store.Clock.Now, candidate.CreatedAt);
        Assert.Null(candidate.RejectedAt);
        Assert.Null(candidate.PurchasedAt);
        Assert.Null(candidate.LatestEstimate);
        Assert.Null(candidate.EstimatedTotalCost);
        Assert.Empty(candidate.EstimateHistory);
        Assert.Equal(1, await store.Context.Candidates.CountAsync());
    }

    [Theory]
    [InlineData("make")]
    [InlineData("model")]
    [InlineData("oldYear")]
    [InlineData("futureYear")]
    [InlineData("mileage")]
    [InlineData("price")]
    public async Task CreateRejectsInvalidInputBeforeWriting(string field)
    {
        using var store = new CandidateTestStore();
        var request = NewCandidate();
        switch (field)
        {
            case "make": request.Make = " "; break;
            case "model": request.Model = ""; break;
            case "oldYear": request.Year = 1885; break;
            case "futureYear": request.Year = 2028; break;
            case "mileage": request.Mileage = -1; break;
            case "price": request.ExpectedSellingPrice = -0.01m; break;
        }

        await Assert.ThrowsAsync<ValidationException>(() => store.Service.CreateCandidate(request, Token));
        Assert.Empty(await store.Context.Candidates.ToListAsync());
    }

    [Fact]
    public async Task VersionsPreserveItemsAndUseHighestVersionForLatestAnalysis()
    {
        using var store = new CandidateTestStore();
        var candidate = await store.Service.CreateCandidate(NewCandidate(), Token);
        var first = await store.Service.CreateCandidateEstimate(Estimate(candidate.Id), Token);
        Assert.Equal(1, first.Version);
        Assert.Equal(5180m, first.FinancialAnalysis.EstimatedTotalCost);
        Assert.Equal(1820m, first.FinancialAnalysis.ExpectedProfit);
        Assert.Equal(35.14m, Math.Round(first.FinancialAnalysis.ExpectedRoi, 2));
        Assert.All(first.Items, item => Assert.True(item.Id > 0));
        Assert.Equal("Purchase", first.Items[0].Description);

        var request = Estimate(candidate.Id);
        request.ExpectedSellingPrice = 7500;
        request.Items[2].EstimatedAmount = 1600;
        request.Notes = "Revised repair costs";
        var second = await store.Service.CreateCandidateEstimate(request, Token);
        Assert.Equal(2, second.Version);
        Assert.NotEqual(first.Id, second.Id);

        store.Context.ChangeTracker.Clear();
        var details = await store.Service.GetCandidateDetails(candidate.Id, Token);
        Assert.Equal(second.Id, details.LatestEstimate!.Id);
        Assert.Equal(7500m, details.ExpectedSellingPrice);
        Assert.Equal(5580m, details.EstimatedTotalCost);
        Assert.Equal(1920m, details.ExpectedProfit);
        Assert.Equal(new[] { 2, 1 }, details.EstimateHistory.Select(estimate => estimate.Version));
        var persistedFirst = details.EstimateHistory.Single(estimate => estimate.Version == 1);
        Assert.Equal(7000m, persistedFirst.ExpectedSellingPrice);
        Assert.Equal("First estimate", persistedFirst.Notes);
        Assert.Equal(1200m, persistedFirst.Items.Single(item => item.Category == CostCategory.Repair).EstimatedAmount);
        Assert.All(details.EstimateHistory, estimate => Assert.False(estimate.IsDecisionSnapshot));
        Assert.Equal(10, await store.Context.CandidateEstimateItems.CountAsync());
        Assert.Empty(await store.Context.Vehicles.ToListAsync());
        Assert.Empty(await store.Context.FinancialTransactions.ToListAsync());
        Assert.Empty(await store.Context.CapitalAccounts.ToListAsync());

        var list = await store.Service.GetCandidates(new CandidateFilterDto(), Token);
        Assert.Equal(5580m, Assert.Single(list.Items).EstimatedTotalCost);
        Assert.Equal(2, (await store.Service.GetEstimateHistory(candidate.Id, Token)).Count);
    }

    [Fact]
    public async Task VersionUsesMaxRatherThanCountAndPreservesExistingSnapshot()
    {
        using var store = new CandidateTestStore();
        var candidate = await store.Service.CreateCandidate(NewCandidate(), Token);
        var old = new CandidateEstimate
        {
            CandidateId = candidate.Id, Version = 7, ExpectedSellingPrice = 6000,
            CreatedAt = store.Clock.Now.AddDays(-1), IsDecisionSnapshot = true,
            CandidateEstimateItems = [new() { Category = CostCategory.Purchase, Description = "Original", EstimatedAmount = 2000 }]
        };
        store.Context.CandidateEstimates.Add(old);
        await store.Context.SaveChangesAsync();
        var next = await store.Service.CreateCandidateEstimate(Estimate(candidate.Id), Token);

        Assert.Equal(8, next.Version);
        await store.Service.ApproveCandidate(candidate.Id, Token);
        store.Context.ChangeTracker.Clear();
        var unchanged = await store.Context.CandidateEstimates.Include(estimate => estimate.CandidateEstimateItems).SingleAsync(estimate => estimate.Id == old.Id);
        Assert.True(unchanged.IsDecisionSnapshot);
        Assert.Equal(2000m, Assert.Single(unchanged.CandidateEstimateItems).EstimatedAmount);
        Assert.False(next.IsDecisionSnapshot);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("null")]
    [InlineData("nullItem")]
    [InlineData("description")]
    [InlineData("category")]
    [InlineData("amount")]
    [InlineData("price")]
    public async Task InvalidEstimatesDoNotWritePartialHistory(string field)
    {
        using var store = new CandidateTestStore();
        var candidate = await store.Service.CreateCandidate(NewCandidate(), Token);
        var request = Estimate(candidate.Id);
        switch (field)
        {
            case "empty": request.Items.Clear(); break;
            case "null": request.Items = null!; break;
            case "nullItem": request.Items.Add(null!); break;
            case "description": request.Items[0].Description = " "; break;
            case "category": request.Items[0].Category = (CostCategory)999; break;
            case "amount": request.Items[0].EstimatedAmount = -1; break;
            case "price": request.ExpectedSellingPrice = -1; break;
        }

        await Assert.ThrowsAsync<ValidationException>(() => store.Service.CreateCandidateEstimate(request, Token));
        Assert.Empty(await store.Context.CandidateEstimates.ToListAsync());
        Assert.Empty(await store.Context.CandidateEstimateItems.ToListAsync());
    }

    [Fact]
    public async Task ApprovalRequiresEstimateAndRejectionPreservesNotesAndTimestamp()
    {
        using var store = new CandidateTestStore();
        var candidate = await store.Service.CreateCandidate(NewCandidate(), Token);
        await Assert.ThrowsAsync<CandidateConflictException>(() => store.Service.ApproveCandidate(candidate.Id, Token));
        await store.Service.CreateCandidateEstimate(Estimate(candidate.Id), Token);
        var approved = await store.Service.ApproveCandidate(candidate.Id, Token);
        Assert.Equal(CandidateStatus.Approved, approved.Status);
        Assert.False(approved.LatestEstimate!.IsDecisionSnapshot);
        Assert.Equal(CandidateStatus.Approved, (await store.Service.ApproveCandidate(candidate.Id, Token)).Status);

        var rejected = await store.Service.RejectCandidate(candidate.Id, new() { Reason = "Too expensive" }, Token);
        Assert.Equal(CandidateStatus.Rejected, rejected.Status);
        Assert.Equal(store.Clock.Now, rejected.RejectedAt);
        Assert.Equal("Original evaluation\nRejection reason: Too expensive", rejected.Notes);
        store.Clock.Now = store.Clock.Now.AddHours(1);
        var repeated = await store.Service.RejectCandidate(candidate.Id, new() { Reason = "Different reason" }, Token);
        Assert.Equal(rejected.RejectedAt, repeated.RejectedAt);
        Assert.Equal(rejected.Notes, repeated.Notes);
        await Assert.ThrowsAsync<CandidateConflictException>(() => store.Service.ApproveCandidate(candidate.Id, Token));
        await Assert.ThrowsAsync<CandidateConflictException>(() => store.Service.CreateCandidateEstimate(Estimate(candidate.Id), Token));
    }

    [Fact]
    public async Task PurchasedCandidatesCannotBeApprovedRejectedOrGivenNewEstimates()
    {
        using var store = new CandidateTestStore();
        var candidate = await store.Service.CreateCandidate(NewCandidate(), Token);
        var entity = await store.Context.Candidates.FindAsync(candidate.Id);
        entity!.Status = CandidateStatus.Purchased;
        await store.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<CandidateConflictException>(() => store.Service.ApproveCandidate(candidate.Id, Token));
        await Assert.ThrowsAsync<CandidateConflictException>(() => store.Service.RejectCandidate(candidate.Id, new(), Token));
        await Assert.ThrowsAsync<CandidateConflictException>(() => store.Service.CreateCandidateEstimate(Estimate(candidate.Id), Token));
        Assert.Equal(CandidateStatus.Purchased, (await store.Service.GetCandidateDetails(candidate.Id, Token)).Status);
    }

    [Fact]
    public async Task MissingCandidatesUseSameNotFoundPattern()
    {
        using var store = new CandidateTestStore();
        await Assert.ThrowsAsync<CandidateNotFoundException>(() => store.Service.GetCandidateDetails(999, Token));
        await Assert.ThrowsAsync<CandidateNotFoundException>(() => store.Service.GetEstimateHistory(999, Token));
        await Assert.ThrowsAsync<CandidateNotFoundException>(() => store.Service.CreateCandidateEstimate(Estimate(999), Token));
        await Assert.ThrowsAsync<CandidateNotFoundException>(() => store.Service.ApproveCandidate(999, Token));
        await Assert.ThrowsAsync<CandidateNotFoundException>(() => store.Service.RejectCandidate(999, new(), Token));
    }

    [Fact]
    public async Task ListUsesExistingPaginationAndTextFilter()
    {
        using var store = new CandidateTestStore();
        await store.Service.CreateCandidate(NewCandidate(), Token);
        await store.Service.CreateCandidate(new() { Make = "Audi", Model = "Q3" }, Token);
        var page = await store.Service.GetCandidates(new() { Limit = 1, Offset = 1 }, Token);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal("BMW", Assert.Single(page.Items).Make);
        var search = await store.Service.GetCandidates(new() { TextFilter = "aUDi" }, Token);
        Assert.Equal("Audi", Assert.Single(search.Items).Make);
        Assert.Equal(1, search.TotalCount);
        Assert.Equal(2, (await store.Service.GetCandidates(new() { GetAllData = true }, Token)).Items.Count);
        await Assert.ThrowsAsync<ValidationException>(() => store.Service.GetCandidates(new() { Limit = 0 }, Token));
    }

    [Fact]
    public async Task TransactionRollsBackAlreadySavedGraph()
    {
        using var store = new CandidateTestStore();
        var candidate = await store.Service.CreateCandidate(NewCandidate(), Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.UnitOfWork.ExecuteInTransaction<int>(async token =>
        {
            store.Context.CandidateEstimates.Add(new CandidateEstimate
            {
                CandidateId = candidate.Id, Version = 1,
                CandidateEstimateItems = [new() { Category = CostCategory.Repair, Description = "Repair", EstimatedAmount = 25 }]
            });
            await store.UnitOfWork.SaveChanges(token);
            throw new InvalidOperationException("Simulated failure after saving");
        }, Token));

        Assert.Empty(store.Context.ChangeTracker.Entries());
        Assert.Empty(await store.Context.CandidateEstimates.ToListAsync());
        Assert.Empty(await store.Context.CandidateEstimateItems.ToListAsync());
        Assert.Single(await store.Context.Candidates.ToListAsync());
    }

    [Fact]
    public async Task SerializationFailuresBecomeRetryableConflicts()
    {
        using var store = new CandidateTestStore();
        var exception = await Assert.ThrowsAsync<DBConcurrencyException>(() =>
            store.UnitOfWork.ExecuteInTransaction<int>(_ => throw new PostgresException(
                "Serialization failure", "ERROR", "ERROR", PostgresErrorCodes.SerializationFailure), Token));
        Assert.Contains("Retry", exception.Message);
    }
}
