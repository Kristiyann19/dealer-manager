using DealerManager.Application.Dtos.Dashboard;
namespace DealerManager.Application.IService.Dashboard;
public interface IDashboardService
{
    Task<DashboardDto> GetDashboard(CancellationToken cancellationToken);
}
