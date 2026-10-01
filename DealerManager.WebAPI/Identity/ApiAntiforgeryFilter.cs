using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace DealerManager.WebAPI.Identity;
public sealed class ApiAntiforgeryFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method)) return;
        try { await antiforgery.ValidateRequestAsync(context.HttpContext); }
        catch (AntiforgeryValidationException) {
            context.Result = new BadRequestObjectResult(new ProblemDetails { Status = 400, Title = "Invalid antiforgery token" });
        }
    }
}
