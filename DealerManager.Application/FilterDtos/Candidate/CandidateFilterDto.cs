using CandidateEntity = DealerManager.Domain.Entities.Candidate;

namespace DealerManager.Application.FilterDtos.Candidate
{
    // Reuses the existing Limit / Offset / GetAllData mechanism.
    public class CandidateFilterDto : FilterDto<CandidateEntity>
    {
        public override IQueryable<CandidateEntity> WhereBuilder(IQueryable<CandidateEntity> query)
        {
            query = base.WhereBuilder(query);
            if (!string.IsNullOrWhiteSpace(TextFilter))
            {
                var text = TextFilter.Trim().ToLowerInvariant();
                query = query.Where(candidate => candidate.Make.ToLower().Contains(text)
                    || candidate.Model.ToLower().Contains(text)
                    || (candidate.Vin != null && candidate.Vin.ToLower().Contains(text)));
            }

            return query;
        }
    }
}
