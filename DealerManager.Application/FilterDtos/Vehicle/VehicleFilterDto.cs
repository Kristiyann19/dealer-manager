using DealerManager.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using VehicleEntity = DealerManager.Domain.Entities.Vehicle;

namespace DealerManager.Application.FilterDtos.Vehicle;
public class VehicleFilterDto : FilterDto<VehicleEntity>
{
    [EnumDataType(typeof(VehicleStatus))]
    public VehicleStatus? Status { get; set; }
    public override IQueryable<VehicleEntity> WhereBuilder(IQueryable<VehicleEntity> query)
    {
        query = base.WhereBuilder(query);
        if (Status is { } status) query = query.Where(v => v.Status == status);
        if (!string.IsNullOrWhiteSpace(TextFilter))
        {
            var text = TextFilter.Trim().ToLowerInvariant();
            query = query.Where(v => v.Make.ToLower().Contains(text) || v.Model.ToLower().Contains(text)
                || (v.Vin != null && v.Vin.ToLower().Contains(text)));
        }
        return query;
    }
}
