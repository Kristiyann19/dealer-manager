#nullable enable
namespace DealerManager.Application.Dtos.Dashboard;

// Response-only concepts; never mapped to persistence.
public enum DashboardAttentionSeverity { Critical = 0, Warning = 1, Info = 2 }
public enum DashboardAttentionType
{
    ReservationExpired = 0, ReservationExpiringSoon = 1, CandidateWaiting = 2,
    RepairTakingTooLong = 3, TransportTakingTooLong = 4, ReadyNotListed = 5,
    ReservedWithoutReservation = 6, ListedWithoutListing = 7
}
public sealed class DashboardAttentionItemDto
{
    public DashboardAttentionType Type { get; init; }
    public DashboardAttentionSeverity Severity { get; init; }
    public int? VehicleId { get; init; }
    public int? CandidateId { get; init; }
    public string Make { get; init; } = "";
    public string Model { get; init; } = "";
    public DateTimeOffset ReferenceDate { get; init; }
    public int Days { get; init; }
    public decimal? DepositAmount { get; init; }
}
