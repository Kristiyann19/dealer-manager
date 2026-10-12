using DealerManager.Application.Dtos.Dashboard;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DealerManager.Infrastructure.Service.Dashboard;

internal static class DashboardAttentionQueries
{
    public static async Task<List<DashboardAttentionItemDto>> Read(
        DealerManagerDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var items = new List<DashboardAttentionItemDto>();
        var expiryLimit = now.AddHours(48);
        var reservations = await db.VehicleReservations.AsNoTracking()
            .Where(r => r.Status == VehicleReservationStatus.Active && r.Vehicle.Status != VehicleStatus.Sold
                && r.ExpiresAt != null && r.ExpiresAt <= expiryLimit)
            .Select(r => new { r.VehicleId, r.Vehicle.Make, r.Vehicle.Model, r.ExpiresAt, r.DepositAmount })
            .ToListAsync(cancellationToken);
        foreach (var r in reservations)
        {
            var expired = r.ExpiresAt!.Value < now;
            items.Add(new() {
                Type = expired ? DashboardAttentionType.ReservationExpired : DashboardAttentionType.ReservationExpiringSoon,
                Severity = expired ? DashboardAttentionSeverity.Critical : DashboardAttentionSeverity.Warning,
                VehicleId = r.VehicleId, Make = r.Make, Model = r.Model,
                ReferenceDate = r.ExpiresAt.Value, DepositAmount = r.DepositAmount,
                Days = Math.Max(0, (int)(now - r.ExpiresAt.Value).TotalDays)
            });
        }
        var candidateCutoff = now.AddDays(-5);
        var candidates = await db.Candidates.AsNoTracking()
            .Where(c => c.Status == CandidateStatus.UnderReview && c.CreatedAt <= candidateCutoff)
            .Select(c => new { c.Id, c.Make, c.Model, c.CreatedAt }).ToListAsync(cancellationToken);
        items.AddRange(candidates.Select(c => new DashboardAttentionItemDto {
            Type = DashboardAttentionType.CandidateWaiting, Severity = DashboardAttentionSeverity.Info,
            CandidateId = c.Id, Make = c.Make, Model = c.Model, ReferenceDate = c.CreatedAt,
            Days = (int)(now - c.CreatedAt).TotalDays
        }));

        // One scalar projection, including correlated EXISTS/latest-transition queries.
        // Never load complete histories or use CreatedAt as a status-start fallback.
        var vehicles = await db.Vehicles.AsNoTracking()
            .Where(v => v.Status == VehicleStatus.Repairing || v.Status == VehicleStatus.Transporting
                || v.Status == VehicleStatus.ReadyForSale || v.Status == VehicleStatus.Reserved || v.Status == VehicleStatus.Listed)
            .Select(v => new {
                v.Id, v.Make, v.Model, v.Status,
                HasReservation = db.VehicleReservations.Any(r => r.VehicleId == v.Id && r.Status == VehicleReservationStatus.Active),
                HasListing = db.VehicleListings.Any(l => l.VehicleId == v.Id && l.IsActive),
                Since = db.VehicleStatusHistories.Where(h => h.VehicleId == v.Id && h.ToStatus == v.Status && h.FromStatus != h.ToStatus)
                    .OrderByDescending(h => h.ChangedAt).ThenByDescending(h => h.Id)
                    .Select(h => (DateTimeOffset?)h.ChangedAt).FirstOrDefault()
            }).ToListAsync(cancellationToken);
        foreach (var v in vehicles)
        {
            DashboardAttentionType? type = null;
            var severity = DashboardAttentionSeverity.Warning;
            if (v.Status == VehicleStatus.Reserved && !v.HasReservation) {
                type = DashboardAttentionType.ReservedWithoutReservation; severity = DashboardAttentionSeverity.Critical;
            } else if (v.Status == VehicleStatus.Listed && !v.HasListing) {
                type = DashboardAttentionType.ListedWithoutListing; severity = DashboardAttentionSeverity.Critical;
            } else if (v.Since is { } since) {
                if (v.Status == VehicleStatus.Repairing && since <= now.AddDays(-14))
                    type = DashboardAttentionType.RepairTakingTooLong;
                else if (v.Status == VehicleStatus.Transporting && since <= now.AddDays(-10))
                    type = DashboardAttentionType.TransportTakingTooLong;
                else if (v.Status == VehicleStatus.ReadyForSale && !v.HasListing && since <= now.AddDays(-3)) {
                    type = DashboardAttentionType.ReadyNotListed; severity = DashboardAttentionSeverity.Info;
                }
            }
            if (type is null) continue;
            items.Add(new() {
                Type = type.Value, Severity = severity, VehicleId = v.Id, Make = v.Make, Model = v.Model,
                // Missing history is only allowed for defensive consistency alerts.
                ReferenceDate = v.Since ?? now, Days = v.Since is { } start ? Math.Max(0, (int)(now - start).TotalDays) : 0
            });
        }
        return items.OrderBy(i => i.Severity).ThenBy(i => i.ReferenceDate)
            .ThenBy(i => i.Type).ThenBy(i => i.VehicleId).ThenBy(i => i.CandidateId).ToList();
    }
}
