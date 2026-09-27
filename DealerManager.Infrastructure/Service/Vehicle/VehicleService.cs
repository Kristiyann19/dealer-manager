using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.FilterDtos;
using DealerManager.Application.IRepository;
using DealerManager.Application.IService.Vehicle;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using VehicleEntity = DealerManager.Domain.Entities.Vehicle;

namespace DealerManager.Infrastructure.Service.Vehicle;
public class VehicleService(
    IBaseRepository<VehicleEntity, FilterDto<VehicleEntity>, DealerManagerDbContext> vehicles,
    IBaseRepository<FinancialTransaction, FilterDto<FinancialTransaction>, DealerManagerDbContext> transactions) : IVehicleService
{
    public async Task<VehicleDetailsDto> GetDetails(int id, CancellationToken cancellationToken)
    {
        var purchases = transactions.GetQueryByProperties(t => t.Type == TransactionType.VehiclePurchase && t.Direction == TransactionDirection.Out);
        return await vehicles.GetQueryByProperties(v => v.Id == id).Select(v => new VehicleDetailsDto
        {
            Id = v.Id, Make = v.Make, Model = v.Model, Year = v.Year, Status = v.Status,
            SourceCandidateId = v.SourceCandidateId, PurchaseDate = v.PurchaseDate,
            ActualPurchasePrice = purchases.Where(t => t.VehicleId == v.Id).Select(t => (decimal?)t.Amount).Sum()
        }).SingleOrDefaultAsync(cancellationToken) ?? throw new VehicleNotFoundException(id);
    }
}
