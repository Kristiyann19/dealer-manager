#nullable enable
using DealerManager.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace DealerManager.Application.Dtos.Vehicle;
public class ChangeVehicleStatusRequest
{
    [Required, EnumDataType(typeof(VehicleStatus))]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehicleStatus? Status { get; set; }
    public string? Notes { get; set; }
}
