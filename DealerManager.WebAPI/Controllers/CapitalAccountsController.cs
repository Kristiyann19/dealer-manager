using Microsoft.AspNetCore.Authorization;
using DealerManager.Application.Dtos.Finance;
using DealerManager.Application.IService.Finance;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DealerManager.Controllers
{
    [ApiController, Authorize]
    [Route("api/capital-accounts")]
    public class CapitalAccountsController(ICapitalAccountService capitalAccountService) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<CapitalAccountDto>> CreateCapitalAccount(CreateCapitalAccountRequest request, CancellationToken cancellationToken)
        {
            var result = await capitalAccountService.CreateCapitalAccount(request, cancellationToken);
            return CreatedAtAction(nameof(GetCapitalAccountDetails), new { id = result.Id }, result);
        }

        [HttpGet]
        public Task<IReadOnlyList<CapitalAccountDto>> GetCapitalAccounts(CancellationToken cancellationToken)
            => capitalAccountService.GetCapitalAccounts(cancellationToken);

        [HttpGet("{id:int}")]
        public Task<CapitalAccountDetailsDto> GetCapitalAccountDetails(int id, CancellationToken cancellationToken)
            => capitalAccountService.GetCapitalAccountDetails(id, cancellationToken);

        [HttpPost("{id:int}/contributions")]
        public async Task<ActionResult<FinancialTransactionDto>> AddCapital(int id, AddCapitalRequest request, CancellationToken cancellationToken)
        {
            if (request.CapitalAccountId.HasValue && request.CapitalAccountId != id)
                throw new ValidationException("CapitalAccountId must match the route account ID.");
            request.CapitalAccountId = id;
            var result = await capitalAccountService.AddCapital(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpGet("{id:int}/transactions")]
        public Task<IReadOnlyList<FinancialTransactionDto>> GetTransactions(int id, CancellationToken cancellationToken)
            => capitalAccountService.GetTransactions(id, cancellationToken);
    }
}
