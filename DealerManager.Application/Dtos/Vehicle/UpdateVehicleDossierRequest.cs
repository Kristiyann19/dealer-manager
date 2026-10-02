using System.Text.Json.Serialization;
using DriveType = DealerManager.Domain.Enums.DriveType;
using DealerManager.Domain.Enums;
#nullable enable
using System.ComponentModel.DataAnnotations;
namespace DealerManager.Application.Dtos.Vehicle;
public class UpdateVehicleDossierRequest
{
    [MaxLength(32)]
    public string? Vin { get; set; }
    [Range(0, 2147483647)]
    public int? Mileage { get; set; }
    [MaxLength(32)]
    public string? RegistrationNumber { get; set; }
    public DateOnly? FirstRegistration { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [EnumDataType(typeof(FuelType))]
    public FuelType? FuelType { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [EnumDataType(typeof(Transmission))]
    public Transmission? Transmission { get; set; }
    [Range(1, 100000)]
    public int? EngineDisplacementCc { get; set; }
    [Range(1, 10000)]
    public int? PowerKw { get; set; }
    [Range(1, 10000)]
    public int? PowerHp { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [EnumDataType(typeof(DriveType))]
    public DriveType? DriveType { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [EnumDataType(typeof(EuroStandard))]
    public EuroStandard? EuroStandard { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [EnumDataType(typeof(BodyType))]
    public BodyType? BodyType { get; set; }
    [MaxLength(100)]
    public string? Color { get; set; }
    [Range(0, 10)]
    public int? NumberOfDoors { get; set; }
    [Range(1, 100)]
    public int? NumberOfSeats { get; set; }
    [Range(0, 20)]
    public int? NumberOfKeys { get; set; }
    [MaxLength(100)]
    public string? ImportedFrom { get; set; }
    [MaxLength(4000)]
    public string? Notes { get; set; }
}
