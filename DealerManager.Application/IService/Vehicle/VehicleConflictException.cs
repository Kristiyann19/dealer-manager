namespace DealerManager.Application.IService.Vehicle;
public class VehicleConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
