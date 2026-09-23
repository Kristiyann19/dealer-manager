using DealerManager.Application.Dtos.Candidate;
using DealerManager.Application.FilterDtos.Candidate;
using DealerManager.Application.IService.Candidate;
using Microsoft.AspNetCore.Mvc;

namespace DealerManager.Controllers
{
    [ApiController]
    [Route("api/candidates")]
    public class CandidatesController : ControllerBase
    {
        private readonly ICandidateService candidateService;

        public CandidatesController(ICandidateService candidateService)
        {
            this.candidateService = candidateService;
        }

        [HttpPost]
        public async Task<ActionResult<CandidateDetailsDto>> CreateCandidate(CreateCandidateRequest request, CancellationToken cancellationToken)
        {
            var result = await candidateService.CreateCandidate(request, cancellationToken);
            return CreatedAtAction(nameof(GetCandidateDetails), new { id = result.Id }, result);
        }

        [HttpGet]
        public Task<CandidateListResultDto> GetCandidates([FromQuery] CandidateFilterDto filter, CancellationToken cancellationToken)
            => candidateService.GetCandidates(filter, cancellationToken);

        [HttpGet("{id:int}")]
        public Task<CandidateDetailsDto> GetCandidateDetails(int id, CancellationToken cancellationToken)
            => candidateService.GetCandidateDetails(id, cancellationToken);

        [HttpPost("estimates")]
        public async Task<ActionResult<CandidateEstimateDto>> CreateCandidateEstimate(CreateCandidateEstimateRequest request, CancellationToken cancellationToken)
        {
            var result = await candidateService.CreateCandidateEstimate(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpGet("{id:int}/estimates")]
        public Task<IReadOnlyList<CandidateEstimateDto>> GetEstimateHistory(int id, CancellationToken cancellationToken)
            => candidateService.GetEstimateHistory(id, cancellationToken);

        [HttpPost("{id:int}/approve")]
        public Task<CandidateDetailsDto> ApproveCandidate(int id, CancellationToken cancellationToken)
            => candidateService.ApproveCandidate(id, cancellationToken);

        [HttpPost("{id:int}/reject")]
        public Task<CandidateDetailsDto> RejectCandidate(int id, RejectCandidateRequest request, CancellationToken cancellationToken)
            => candidateService.RejectCandidate(id, request, cancellationToken);
    }
}
