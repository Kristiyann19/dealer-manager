namespace DealerManager.Application.IService.Candidate;

public class PurchaseCandidateException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
