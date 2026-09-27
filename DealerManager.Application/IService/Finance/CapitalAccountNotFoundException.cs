namespace DealerManager.Application.IService.Finance
{
    public class CapitalAccountNotFoundException(int id) : Exception($"Capital account {id} was not found.");
}
