using DealerManager.Application.Dtos.Candidate;
using DealerManager.Application.IService.Candidate;
using System.ComponentModel.DataAnnotations;

namespace DealerManager.Infrastructure.Service.Candidate
{
    public class CandidateFinancialCalculator : ICandidateFinancialCalculator
    {
        public CandidateFinancialAnalysisDto Calculate(decimal expectedSellingPrice, IEnumerable<decimal> estimatedAmounts)
        {
            ArgumentNullException.ThrowIfNull(estimatedAmounts);
            try
            {
                var total = estimatedAmounts.Sum();
                var profit = expectedSellingPrice - total;
                return new CandidateFinancialAnalysisDto
                {
                    EstimatedTotalCost = total,
                    ExpectedProfit = profit,
                    ExpectedRoi = total == 0 ? 0 : profit / total * 100m
                };
            }
            catch (OverflowException)
            {
                throw new ValidationException("The financial amounts exceed the supported decimal range.");
            }
        }
    }
}
