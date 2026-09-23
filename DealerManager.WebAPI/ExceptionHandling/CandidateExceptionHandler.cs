using DealerManager.Application.IService.Candidate;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace DealerManager.WebAPI.ExceptionHandling
{
    public class CandidateExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var statusCode = exception switch
            {
                CandidateNotFoundException => StatusCodes.Status404NotFound,
                CandidateConflictException or DBConcurrencyException => StatusCodes.Status409Conflict,
                ValidationException => StatusCodes.Status400BadRequest,
                _ => 0
            };
            if (statusCode == 0)
                return false;

            httpContext.Response.StatusCode = statusCode;
            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode switch
                {
                    404 => "Candidate not found",
                    409 => "Candidate operation conflict",
                    _ => "Invalid candidate request"
                },
                Detail = exception.Message,
                Instance = httpContext.Request.Path
            };
            await httpContext.Response.WriteAsJsonAsync(problem, options: null,
                contentType: "application/problem+json", cancellationToken: cancellationToken);
            return true;
        }
    }
}
