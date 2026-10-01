using Microsoft.AspNetCore.Authorization;
using DealerManager.Application.Dtos.Dashboard;
using DealerManager.Application.IService.Dashboard;
using Microsoft.AspNetCore.Mvc;
namespace DealerManager.Controllers;

[ApiController, Authorize]
[Route("api/dashboard")]
public class DashboardController(IDashboardService dashboard) : ControllerBase
{
    [HttpGet]
    public Task<DashboardDto> Get(CancellationToken cancellationToken) => dashboard.GetDashboard(cancellationToken);
}
