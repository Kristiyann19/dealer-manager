using DealerManager.Application.Dtos.Finance;
using DealerManager.Application.IService.Finance;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;

namespace DealerManager.Tests.Finance;

public class CapitalAccountTests
{
    [Theory]
    [InlineData("{\"amount\":25}")]
    [InlineData("{\"amount\":25,\"description\":null}")]
    [InlineData("{\"amount\":25,\"description\":\"\"}")]
    [InlineData("{\"amount\":25,\"description\":\"   \"}")]
    public async Task ContributionDescriptionIsOptional(string body)
    {
        using var factory = new FinanceApiFactory();
        using var client = factory.CreateInitializedClient();
        var account = await Create(client);
        var response = await client.PostAsync($"/api/capital-accounts/{account.Id}/contributions",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(string.Empty, (await response.Content.ReadFromJsonAsync<FinancialTransactionDto>())!.Description);
        var details = (await client.GetFromJsonAsync<CapitalAccountDetailsDto>($"/api/capital-accounts/{account.Id}"))!;
        Assert.Equal(25m, details.CurrentBalance);
        Assert.Single(details.LatestTransactions);
    }
    private static async Task<CapitalAccountDto> Create(HttpClient client, string name = "Main", string currency = "EUR")
    {
        var response = await client.PostAsJsonAsync("/api/capital-accounts", new { name, currency, isActive = false, currentBalance = 999 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        return (await response.Content.ReadFromJsonAsync<CapitalAccountDto>())!;
    }

    [Fact]
    public async Task CreatesTrimmedActiveAccountWithZeroDerivedBalance()
    {
        using var factory = new FinanceApiFactory();
        using var client = factory.CreateInitializedClient();
        var account = await Create(client, " Main capital ", " EUR ");
        Assert.True(account.Id > 0);
        Assert.Equal("Main capital", account.Name);
        Assert.Equal("EUR", account.Currency);
        Assert.True(account.IsActive);
        Assert.Equal(0m, account.CurrentBalance);
        var details = (await client.GetFromJsonAsync<CapitalAccountDetailsDto>($"/api/capital-accounts/{account.Id}"))!;
        Assert.Empty(details.LatestTransactions);
        Assert.Equal(0m, details.CurrentBalance);
        Assert.Empty((await client.GetFromJsonAsync<List<FinancialTransactionDto>>($"/api/capital-accounts/{account.Id}/transactions"))!);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        Assert.Null(db.Model.FindEntityType(typeof(CapitalAccount))!.FindProperty("CurrentBalance"));
        Assert.Empty(await db.FinancialTransactions.ToListAsync());
    }

    [Fact]
    public async Task ContributionsSumPreciselyAndOutflowsReduceOnlyTheirOwnAccountBalance()
    {
        using var factory = new FinanceApiFactory();
        using var client = factory.CreateInitializedClient();
        var account = await Create(client);
        var other = await Create(client, "Cash", "BGN");
        foreach (var amount in new[] { 20000.10m, 500.20m })
        {
            var response = await client.PostAsJsonAsync($"/api/capital-accounts/{account.Id}/contributions", new
            {
                amount, description = " Contribution ", type = TransactionType.OwnerWithdrawal,
                direction = TransactionDirection.Out, vehicleId = 123, createdAt = "1999-01-01T00:00:00Z"
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var transaction = (await response.Content.ReadFromJsonAsync<FinancialTransactionDto>())!;
            Assert.Equal(amount, transaction.Amount);
            Assert.Equal(account.Id, transaction.CapitalAccountId);
            Assert.Equal(TransactionDirection.In, transaction.Direction);
            Assert.Equal(TransactionType.CapitalContribution, transaction.Type);
            Assert.Equal("Contribution", transaction.Description);
            Assert.Equal(factory.Clock.Now, transaction.OccurredAt);
            Assert.Equal(factory.Clock.Now, transaction.CreatedAt);
            Assert.Null(transaction.VehicleId);
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
            db.FinancialTransactions.Add(new FinancialTransaction
            {
                CapitalAccountId = account.Id, Amount = 3000.05m, Direction = TransactionDirection.Out,
                Type = TransactionType.Other, Description = "Direct test outflow", OccurredAt = factory.Clock.Now, CreatedAt = factory.Clock.Now
            });
            await db.SaveChangesAsync();
        }
        factory.Queries.Commands.Clear();
        var accounts = (await client.GetFromJsonAsync<List<CapitalAccountDto>>("/api/capital-accounts"))!;
        Assert.Equal(17500.25m, accounts.Single(a => a.Id == account.Id).CurrentBalance);
        Assert.Equal(0m, accounts.Single(a => a.Id == other.Id).CurrentBalance);
        var sql = Assert.Single(factory.Queries.Commands);
        Assert.Contains("sum", sql, StringComparison.OrdinalIgnoreCase);
        var details = (await client.GetFromJsonAsync<CapitalAccountDetailsDto>($"/api/capital-accounts/{account.Id}"))!;
        Assert.Equal(17500.25m, details.CurrentBalance);
        Assert.Equal(3, details.LatestTransactions.Count);
    }

    [Fact]
    public async Task HistoryUsesOccurredAtThenIdAndDetailsLimitDoesNotLimitBalance()
    {
        using var factory = new FinanceApiFactory();
        using var client = factory.CreateInitializedClient();
        var account = await Create(client);
        var other = await Create(client, "Other");
        List<int> orderedIds;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
            var transactions = Enumerable.Range(0, 25).Select(i => new FinancialTransaction
            {
                CapitalAccountId = account.Id, Amount = 1.01m, Direction = TransactionDirection.In,
                Type = TransactionType.CapitalContribution, Description = "Test", OccurredAt = factory.Clock.Now.AddDays(i % 5), CreatedAt = factory.Clock.Now
            }).ToList();
            db.FinancialTransactions.AddRange(transactions);
            db.FinancialTransactions.Add(new FinancialTransaction
            {
                CapitalAccountId = other.Id, Amount = 999, Direction = TransactionDirection.In,
                Type = TransactionType.CapitalContribution, Description = "Other", OccurredAt = factory.Clock.Now.AddYears(1), CreatedAt = factory.Clock.Now
            });
            await db.SaveChangesAsync();
            orderedIds = transactions.OrderByDescending(t => t.OccurredAt).ThenByDescending(t => t.Id).Select(t => t.Id).ToList();
        }
        var details = (await client.GetFromJsonAsync<CapitalAccountDetailsDto>($"/api/capital-accounts/{account.Id}"))!;
        Assert.Equal(25.25m, details.CurrentBalance);
        Assert.Equal(orderedIds.Take(20), details.LatestTransactions.Select(t => t.Id));
        var history = (await client.GetFromJsonAsync<List<FinancialTransactionDto>>($"/api/capital-accounts/{account.Id}/transactions"))!;
        Assert.Equal(orderedIds, history.Select(t => t.Id));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task InvalidAmountsAreRejectedAtApiAndServiceWithoutWriting(string amountText)
    {
        using var factory = new FinanceApiFactory();
        using var client = factory.CreateInitializedClient();
        var account = await Create(client);
        var request = new AddCapitalRequest { CapitalAccountId = account.Id, Amount = decimal.Parse(amountText), Description = "Test" };
        var response = await client.PostAsJsonAsync($"/api/capital-accounts/{account.Id}/contributions", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICapitalAccountService>();
        await Assert.ThrowsAsync<ValidationException>(() => service.AddCapital(request, CancellationToken.None));
        Assert.Empty(await service.GetTransactions(account.Id, CancellationToken.None));
    }

    [Theory]
    [InlineData("", "EUR")]
    [InlineData("  ", "EUR")]
    [InlineData("Main", " ")]
    public async Task InvalidAccountStringsAreRejected(string name, string currency)
    {
        using var factory = new FinanceApiFactory();
        using var client = factory.CreateInitializedClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/capital-accounts", new { name, currency })).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<CapitalAccountDto>>("/api/capital-accounts"))!);
    }

    [Fact]
    public async Task UnknownAccountsReturnFinanceProblemDetails()
    {
        using var factory = new FinanceApiFactory();
        using var client = factory.CreateInitializedClient();
        var responses = new[] {
            await client.GetAsync("/api/capital-accounts/999"),
            await client.GetAsync("/api/capital-accounts/999/transactions"),
            await client.PostAsJsonAsync("/api/capital-accounts/999/contributions", new { amount = 10, description = "Test" })
        };
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            Assert.Contains("Capital account 999", (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail);
        }
        using var scope = factory.Services.CreateScope();
        await Assert.ThrowsAsync<CapitalAccountNotFoundException>(() => scope.ServiceProvider.GetRequiredService<ICapitalAccountService>()
            .GetCapitalAccountDetails(999, CancellationToken.None));
    }

    [Fact]
    public async Task InactiveAccountsAreReadableButExcludedFromListAndCannotReceiveContributions()
    {
        using var factory = new FinanceApiFactory();
        using var client = factory.CreateInitializedClient();
        var account = await Create(client);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
            (await db.CapitalAccounts.FindAsync(account.Id))!.IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Empty((await client.GetFromJsonAsync<List<CapitalAccountDto>>("/api/capital-accounts"))!);
        Assert.False((await client.GetFromJsonAsync<CapitalAccountDetailsDto>($"/api/capital-accounts/{account.Id}"))!.IsActive);
        var response = await client.PostAsJsonAsync($"/api/capital-accounts/{account.Id}/contributions", new { amount = 10, description = "Test" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<FinancialTransactionDto>>($"/api/capital-accounts/{account.Id}/transactions"))!);
    }

    [Fact]
    public async Task ContributionUsesSuppliedDateAndRejectsRouteMismatch()
    {
        using var factory = new FinanceApiFactory();
        using var client = factory.CreateInitializedClient();
        var account = await Create(client);
        var occurredAt = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.FromHours(2));
        var response = await client.PostAsJsonAsync($"/api/capital-accounts/{account.Id}/contributions", new { amount = 0.001m, description = "Test", occurredAt });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var transaction = (await response.Content.ReadFromJsonAsync<FinancialTransactionDto>())!;
        Assert.Equal(occurredAt.ToUniversalTime(), transaction.OccurredAt);
        Assert.Equal(factory.Clock.Now, transaction.CreatedAt);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/capital-accounts/{account.Id}/contributions", new { capitalAccountId = account.Id + 1, amount = 5, description = "Test" })).StatusCode);
        Assert.Single((await client.GetFromJsonAsync<List<FinancialTransactionDto>>($"/api/capital-accounts/{account.Id}/transactions"))!);
    }
}
