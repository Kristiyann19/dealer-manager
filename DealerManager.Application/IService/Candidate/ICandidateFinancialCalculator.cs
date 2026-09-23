using DealerManager.Application.Dtos.Candidate;

namespace DealerManager.Application.IService.Candidate
{
    public interface ICandidateFinancialCalculator
    {
        CandidateFinancialAnalysisDto Calculate(decimal expectedSellingPrice, IEnumerable<decimal> estimatedAmounts);
    }
}
