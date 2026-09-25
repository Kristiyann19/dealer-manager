using DealerManager.Application.Dtos.Candidate;
using DealerManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;

namespace DealerManager.Tests.Candidate;

public class CandidateApiTests
{
    [Fact]
    public async Task ClientCannotSetSystemOwnedFieldsAndDtoDoesNotExposeEntities()
    {
        using var factory = new CandidateApiFactory();
        using var client = factory.CreateInitializedClient();
        var response = await client.PostAsJsonAsync("/api/candidates", new
        {
            id = 900, status = CandidateStatus.Purchased,
            createdAt = "1999-01-01T00:00:00Z", rejectedAt = "1999-01-01T00:00:00Z",
            purchasedAt = "1999-01-01T00:00:00Z", make = "BMW", model = "X1", askingPrice = 3000, expectedSellingPrice = 7000
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<CandidateDetailsDto>())!;
        Assert.NotEqual(900, created.Id);
        Assert.Equal(CandidateStatus.UnderReview, created.Status);
        Assert.Equal(new FixedTimeProvider().Now, created.CreatedAt);
        Assert.Null(created.RejectedAt);
        Assert.Null(created.PurchasedAt);
        Assert.Equal(3000m, created.AskingPrice);
        Assert.Null(created.ExpectedSellingPrice);
        Assert.NotNull(response.Headers.Location);
        var details = await client.GetFromJsonAsync<CandidateDetailsDto>(response.Headers.Location);
        Assert.Equal(created.Id, details!.Id);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("candidatePhotos", json);
        Assert.DoesNotContain("financialTransactions", json);
    }

    [Fact]
    public async Task CompleteReviewFlowWorksThroughApi()
    {
        using var factory = new CandidateApiFactory();
        using var client = factory.CreateInitializedClient();
        var create = await client.PostAsJsonAsync("/api/candidates", new { make = "BMW", model = "X1", askingPrice = 5000 });
        var candidate = (await create.Content.ReadFromJsonAsync<CandidateDetailsDto>())!;
        Assert.Null(candidate.ExpectedSellingPrice);
        Assert.Null(candidate.ExpectedProfit);

        var prematureApproval = await client.PostAsync($"/api/candidates/{candidate.Id}/approve", null);
        Assert.Equal(HttpStatusCode.Conflict, prematureApproval.StatusCode);

        var estimateResponse = await client.PostAsJsonAsync("/api/candidates/estimates", new
        {
            candidateId = candidate.Id, expectedSellingPrice = 7000,
            version = 999, isDecisionSnapshot = true, createdAt = "1999-01-01T00:00:00Z",
            items = new[] { new { category = CostCategory.Purchase, description = "Purchase", estimatedAmount = 5000m } }
        });
        Assert.Equal(HttpStatusCode.Created, estimateResponse.StatusCode);
        var estimate = (await estimateResponse.Content.ReadFromJsonAsync<CandidateEstimateDto>())!;
        Assert.Equal(1, estimate.Version);
        Assert.False(estimate.IsDecisionSnapshot);
        Assert.Equal(new FixedTimeProvider().Now, estimate.CreatedAt);
        Assert.Equal(2000m, estimate.FinancialAnalysis.ExpectedProfit);
        Assert.Equal(40m, estimate.FinancialAnalysis.ExpectedRoi);

        var approval = await client.PostAsync($"/api/candidates/{candidate.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approval.StatusCode);
        Assert.Equal(CandidateStatus.Approved, (await approval.Content.ReadFromJsonAsync<CandidateDetailsDto>())!.Status);

        var history = await client.GetFromJsonAsync<List<CandidateEstimateDto>>($"/api/candidates/{candidate.Id}/estimates");
        Assert.Equal(estimate.Id, Assert.Single(history!).Id);
        var list = await client.GetFromJsonAsync<CandidateListResultDto>("/api/candidates?Limit=1");
        Assert.Equal(5000m, Assert.Single(list!.Items).EstimatedTotalCost);
        Assert.Equal(5000m, Assert.Single(list.Items).AskingPrice);
        Assert.Equal(7000m, Assert.Single(list.Items).ExpectedSellingPrice);

        var rejection = await client.PostAsJsonAsync($"/api/candidates/{candidate.Id}/reject", new { reason = "Review failed" });
        Assert.Equal(HttpStatusCode.OK, rejection.StatusCode);
        Assert.Equal(CandidateStatus.Rejected, (await rejection.Content.ReadFromJsonAsync<CandidateDetailsDto>())!.Status);
    }

    [Theory]
    [InlineData("/api/candidates/999")]
    [InlineData("/api/candidates/999/estimates")]
    public async Task MissingCandidateReturnsProblemDetails(string url)
    {
        using var factory = new CandidateApiFactory();
        using var client = factory.CreateInitializedClient();
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(404, (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Status);
    }

    [Fact]
    public async Task NegotiatedPurchasePriceUpdatesDetailsAndListAndRejectsMissingOrDuplicatePurchase()
    {
        using var factory = new CandidateApiFactory();
        using var client = factory.CreateInitializedClient();
        var create = await client.PostAsJsonAsync("/api/candidates", new { make = "BMW", model = "X1", askingPrice = 6000 });
        var candidate = (await create.Content.ReadFromJsonAsync<CandidateDetailsDto>())!;
        foreach (var price in new[] { 5700m, 5500m })
        {
            var response = await client.PostAsJsonAsync("/api/candidates/estimates", new
            {
                candidateId = candidate.Id, expectedSellingPrice = 10000,
                items = new[] { new { category = CostCategory.Purchase, description = "Purchase", estimatedAmount = price } }
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(price, (await client.GetFromJsonAsync<CandidateDetailsDto>($"/api/candidates/{candidate.Id}"))!.AskingPrice);
            Assert.Equal(price, Assert.Single((await client.GetFromJsonAsync<CandidateListResultDto>("/api/candidates"))!.Items).AskingPrice);
        }

        foreach (var categories in new[] { new[] { CostCategory.Transport }, new[] { CostCategory.Purchase, CostCategory.Purchase } })
        {
            var invalid = await client.PostAsJsonAsync("/api/candidates/estimates", new
            {
                candidateId = candidate.Id, expectedSellingPrice = 10000,
                items = categories.Select(category => new { category, description = "Cost", estimatedAmount = 4000m }).ToArray()
            });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        var details = (await client.GetFromJsonAsync<CandidateDetailsDto>($"/api/candidates/{candidate.Id}"))!;
        Assert.Equal(5500m, details.AskingPrice);
        Assert.Equal(2, details.EstimateHistory.Count);
        Assert.Equal(5700m, Assert.Single(details.EstimateHistory.Single(item => item.Version == 1).Items).EstimatedAmount);
    }

    [Fact]
    public async Task ValidationReturns400WithoutWriting()
    {
        using var factory = new CandidateApiFactory();
        using var client = factory.CreateInitializedClient();
        var invalidName = await client.PostAsJsonAsync("/api/candidates", new { make = " ", model = "X1", askingPrice = 3000 });
        Assert.Equal(HttpStatusCode.BadRequest, invalidName.StatusCode);
        var invalidYear = await client.PostAsJsonAsync("/api/candidates", new { make = "BMW", model = "X1", year = 9999, askingPrice = 3000 });
        Assert.Equal(HttpStatusCode.BadRequest, invalidYear.StatusCode);
        var missingPrice = await client.PostAsJsonAsync("/api/candidates", new { make = "BMW", model = "X1", expectedSellingPrice = 7000 });
        Assert.Equal(HttpStatusCode.BadRequest, missingPrice.StatusCode);
        var negativePrice = await client.PostAsJsonAsync("/api/candidates", new { make = "BMW", model = "X1", askingPrice = -1 });
        Assert.Equal(HttpStatusCode.BadRequest, negativePrice.StatusCode);
        var list = await client.GetFromJsonAsync<CandidateListResultDto>("/api/candidates");
        Assert.Equal(0, list!.TotalCount);
    }
}
