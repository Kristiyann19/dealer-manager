using System.Net;
using System.Net.Http.Json;
using DealerManager.Application.Dtos.Candidate;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using DealerManager.Tests.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DealerManager.Tests.Candidate;

public class CandidateAuctionTests
{
    private static CandidateAuctionDetailsDto Auction() => new()
    {
        Platform = " BCA ", LotNumber = " LOT-123 ", ListingUrl = "https://example.test/lot/123",
        Country = "DE", EndsAt = new DateTimeOffset(2026, 11, 1, 18, 30, 0, TimeSpan.FromHours(2)),
        ObservedBid = 7000, MaxBid = 7500
    };
    private static CreateCandidateRequest Request() => new()
    {
        Make = "BMW", Model = "320d", Year = 2020,
        AcquisitionType = CandidateAcquisitionType.Auction, AuctionDetails = Auction()
    };
    private static async Task<CandidateDetailsDto> Create(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/candidates", Request());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CandidateDetailsDto>())!;
    }

    [Fact]
    public async Task CreateEditFilterAndEstimatesKeepPricesSeparateWithoutMovingMoney()
    {
        using var factory = new CandidateApiFactory();
        using var client = factory.CreateInitializedClient();
        var candidate = await Create(client);
        Assert.Null(candidate.AskingPrice);
        Assert.Equal(CandidateAcquisitionType.Auction, candidate.AcquisitionType);
        Assert.Equal("BCA", candidate.AuctionDetails!.Platform);
        Assert.Equal("LOT-123", candidate.AuctionDetails.LotNumber);
        Assert.Equal(TimeSpan.Zero, candidate.AuctionDetails.EndsAt!.Value.Offset);
        var direct = await client.PostAsJsonAsync("/api/candidates", new { make = "Ford", model = "Focus", askingPrice = 2000 });
        direct.EnsureSuccessStatusCode();
        Assert.Equal(CandidateAcquisitionType.DirectPurchase, (await direct.Content.ReadFromJsonAsync<CandidateDetailsDto>())!.AcquisitionType);
        var list = await client.GetFromJsonAsync<CandidateListResultDto>("/api/candidates?AcquisitionType=1&Limit=15");
        Assert.Equal(candidate.Id, Assert.Single(list!.Items).Id);
        Assert.Equal(1, list.TotalCount);
        var auction = Auction(); auction.ObservedBid = 7800; // Exceeding one's limit is information, not invalid data.
        var update = await client.PutAsJsonAsync($"/api/candidates/{candidate.Id}/auction-details", auction);
        update.EnsureSuccessStatusCode();
        foreach (var bid in new[] { 7100, 7200 })
        {
            var estimate = await client.PostAsJsonAsync("/api/candidates/estimates", new {
                candidateId = candidate.Id, expectedSellingPrice = 10000,
                items = new[] { new { category = CostCategory.Purchase, estimatedAmount = bid },
                    new { category = CostCategory.AuctionFee, estimatedAmount = 500 } }
            });
            estimate.EnsureSuccessStatusCode();
        }
        var details = (await client.GetFromJsonAsync<CandidateDetailsDto>($"/api/candidates/{candidate.Id}"))!;
        Assert.Null(details.AskingPrice);
        Assert.Equal(7800, details.AuctionDetails!.ObservedBid);
        Assert.Equal(7500, details.AuctionDetails.MaxBid);
        Assert.Equal(2, details.EstimateHistory.Count);
        Assert.Equal(7700, details.EstimatedTotalCost);
        Assert.Equal(7100, details.EstimateHistory.Single(e => e.Version == 1).Items.Single(i => i.Category == CostCategory.Purchase).EstimatedAmount);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        Assert.Empty(await db.FinancialTransactions.ToListAsync());
        Assert.Empty(await db.Vehicles.ToListAsync());
    }

    [Theory]
    [InlineData("missingAuction")]
    [InlineData("askingPriceOnAuction")]
    [InlineData("missingDirectPrice")]
    [InlineData("auctionOnDirect")]
    [InlineData("unknownType")]
    [InlineData("emptyPlatform")]
    [InlineData("emptyLot")]
    [InlineData("unsafeUrl")]
    [InlineData("negativeBid")]
    [InlineData("zeroLimit")]
    [InlineData("fractionalCent")]
    public async Task InvalidRequestsDoNotCreateCandidates(string invalid)
    {
        using var factory = new CandidateApiFactory();
        using var client = factory.CreateInitializedClient();
        var request = Request();
        switch (invalid)
        {
            case "missingAuction": request.AuctionDetails = null; break;
            case "askingPriceOnAuction": request.AskingPrice = 100; break;
            case "missingDirectPrice": request.AcquisitionType = CandidateAcquisitionType.DirectPurchase; request.AuctionDetails = null; break;
            case "auctionOnDirect": request.AcquisitionType = CandidateAcquisitionType.DirectPurchase; request.AskingPrice = 100; break;
            case "unknownType": request.AcquisitionType = (CandidateAcquisitionType)99; break;
            case "emptyPlatform": request.AuctionDetails!.Platform = " "; break;
            case "emptyLot": request.AuctionDetails!.LotNumber = " "; break;
            case "unsafeUrl": request.AuctionDetails!.ListingUrl = "javascript:alert(1)"; break;
            case "negativeBid": request.AuctionDetails!.ObservedBid = -1; break;
            case "zeroLimit": request.AuctionDetails!.MaxBid = 0; break;
            case "fractionalCent": request.AuctionDetails!.MaxBid = 100.123m; break;
        }
        var response = await client.PostAsJsonAsync("/api/candidates", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, (await client.GetFromJsonAsync<CandidateListResultDto>("/api/candidates"))!.TotalCount);
    }

    [Fact]
    public async Task AuctionPurchaseIsBlockedAndClosedCandidateCannotBeEdited()
    {
        using var factory = new CandidateApiFactory();
        using var client = factory.CreateInitializedClient();
        var candidate = await Create(client);
        (await client.PostAsJsonAsync("/api/candidates/estimates", new {
            candidateId = candidate.Id, expectedSellingPrice = 10000,
            items = new[] { new { category = CostCategory.Purchase, estimatedAmount = 7000 } }
        })).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/candidates/{candidate.Id}/approve", null)).EnsureSuccessStatusCode();
        var purchase = await client.PostAsJsonAsync($"/api/candidates/{candidate.Id}/purchase", new {
            capitalAccountId = 1, actualPurchasePrice = 7000
        });
        Assert.Equal(HttpStatusCode.Conflict, purchase.StatusCode);
        Assert.Contains("auctionPurchasePending", await purchase.Content.ReadAsStringAsync());
        (await client.PostAsJsonAsync($"/api/candidates/{candidate.Id}/reject", new { reason = "Over budget" })).EnsureSuccessStatusCode();
        var edit = await client.PutAsJsonAsync($"/api/candidates/{candidate.Id}/auction-details", Auction());
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        Assert.Empty(await db.FinancialTransactions.ToListAsync());
        Assert.Empty(await db.Vehicles.ToListAsync());
        Assert.False(await db.CandidateEstimates.AnyAsync(e => e.IsDecisionSnapshot));
    }

    [Fact]
    public async Task OtherDealershipCannotReadOrModifyAuctionDetails()
    {
        using var factory = new AuthApiFactory();
        var (clientA, _) = await factory.Register("auction-a@example.test", "Auction A");
        var (clientB, userB) = await factory.Register("auction-b@example.test", "Auction B");
        using (clientA) using (clientB)
        {
            var candidate = await Create(clientA);
            Assert.Equal(HttpStatusCode.NotFound, (await clientB.GetAsync($"/api/candidates/{candidate.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await clientB.PutAsJsonAsync($"/api/candidates/{candidate.Id}/auction-details", Auction())).StatusCode);
            using var db = factory.TenantDb(userB.Dealership.Id);
            Assert.Empty(await db.CandidateAuctionDetails.ToListAsync());
        }
    }
}
