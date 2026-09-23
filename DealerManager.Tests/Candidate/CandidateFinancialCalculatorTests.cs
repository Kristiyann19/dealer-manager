using DealerManager.Infrastructure.Service.Candidate;
using System.ComponentModel.DataAnnotations;

namespace DealerManager.Tests.Candidate;

public class CandidateFinancialCalculatorTests
{
    [Fact]
    public void CalculatesTotalsAndLoss()
    {
        var result = new CandidateFinancialCalculator().Calculate(4000m, [3000m, 600m, 1400m]);
        Assert.Equal(5000m, result.EstimatedTotalCost);
        Assert.Equal(-1000m, result.ExpectedProfit);
        Assert.Equal(-20m, result.ExpectedRoi);
    }

    [Fact]
    public void ZeroCostHasSafeRoi()
    {
        var result = new CandidateFinancialCalculator().Calculate(7000m, [0m]);
        Assert.Equal(0m, result.EstimatedTotalCost);
        Assert.Equal(7000m, result.ExpectedProfit);
        Assert.Equal(0m, result.ExpectedRoi);
    }

    [Fact]
    public void PreservesDecimalCents()
    {
        var result = new CandidateFinancialCalculator().Calculate(1m, [0.1m, 0.2m]);
        Assert.Equal(0.3m, result.EstimatedTotalCost);
        Assert.Equal(0.7m, result.ExpectedProfit);
    }

    [Fact]
    public void ReportsOverflowAsValidationFailure()
    {
        Assert.Throws<ValidationException>(() => new CandidateFinancialCalculator().Calculate(0, [decimal.MaxValue, 1m]));
    }
}
