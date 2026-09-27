namespace DealerManager.Application.Dtos.Finance
{
    public class CapitalAccountDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Currency { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public decimal CurrentBalance { get; init; }
    }
}
