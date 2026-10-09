using DealerManager.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using VehicleEntity = DealerManager.Domain.Entities.Vehicle;

namespace DealerManager.Application.FilterDtos.Vehicle;
public class VehicleFilterDto : FilterDto<VehicleEntity>
{
    [EnumDataType(typeof(VehicleStatus))]
    public VehicleStatus? Status { get; set; }
    public bool AgedStock { get; set; }
    private DateTimeOffset? stockCutoff;
    // Calendar days use UTC consistently with the dashboard's TimeProvider clock.
    public void SetStockReferenceDate(DateTimeOffset now) => stockCutoff = new DateTimeOffset(now.UtcDateTime.Date.AddDays(-30), TimeSpan.Zero);
    public override IQueryable<VehicleEntity> WhereBuilder(IQueryable<VehicleEntity> query)
    {
        query = base.WhereBuilder(query);
        if (AgedStock)
        {
            var cutoff = stockCutoff ?? throw new InvalidOperationException("Stock reference date must be supplied by the service.");
            query = query.Where(v => v.Status != VehicleStatus.Sold && v.PurchaseDate < cutoff);
        }
        if (Status is { } status) query = query.Where(v => v.Status == status);
        if (!string.IsNullOrWhiteSpace(TextFilter))
        {
            var text = TextFilter.Trim().ToLowerInvariant();
            query = query.Where(v => v.Make.ToLower().Contains(text) || v.Model.ToLower().Contains(text)
                || (v.Vin != null && v.Vin.ToLower().Contains(text))
                || (v.RegistrationNumber != null && v.RegistrationNumber.ToLower().Contains(text)));
        }
        return query;
    }
}
