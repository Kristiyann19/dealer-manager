using AutoMapper;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Domain.Entities;

namespace DealerManager.Infrastructure.AutoMapperProfiles;

public sealed class VehicleProfile : Profile
{
    public VehicleProfile()
    {
        // Only dossier fields are writable: identity, tenant, lifecycle and navigations are protected.
        CreateMap<UpdateVehicleDossierRequest, Vehicle>()
            .ForMember(d => d.DealershipId, o => o.Ignore())
            .ForMember(d => d.SourceCandidateId, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.Make, o => o.Ignore())
            .ForMember(d => d.Model, o => o.Ignore())
            .ForMember(d => d.Year, o => o.Ignore())
            .ForMember(d => d.PurchaseDate, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.SourceCandidate, o => o.Ignore())
            .ForMember(d => d.VehicleStatusHistory, o => o.Ignore())
            .ForMember(d => d.VehicleCostPlanItems, o => o.Ignore())
            .ForMember(d => d.VehicleExpenses, o => o.Ignore())
            .ForMember(d => d.VehiclePhotos, o => o.Ignore())
            .ForMember(d => d.VehicleDocuments, o => o.Ignore())
            .ForMember(d => d.VehicleSale, o => o.Ignore())
            .ForMember(d => d.VehicleListings, o => o.Ignore())
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Vin, o => o.MapFrom(s => string.IsNullOrWhiteSpace(s.Vin) ? null : s.Vin.Trim().ToUpperInvariant()))
            .ForMember(d => d.RegistrationNumber, o => o.MapFrom(s => string.IsNullOrWhiteSpace(s.RegistrationNumber) ? null : s.RegistrationNumber.Trim().ToUpperInvariant()))
            .ForMember(d => d.Color, o => o.MapFrom(s => string.IsNullOrWhiteSpace(s.Color) ? null : s.Color.Trim()))
            .ForMember(d => d.ImportedFrom, o => o.MapFrom(s => string.IsNullOrWhiteSpace(s.ImportedFrom) ? null : s.ImportedFrom.Trim()))
            .ForMember(d => d.Notes, o => o.MapFrom(s => string.IsNullOrWhiteSpace(s.Notes) ? null : s.Notes.Trim()))
            .MaxDepth(4);
        CreateMap<VehicleReservation, VehicleReservationDto>()
            .ForMember(d => d.ReservationId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.VehicleStatus, o => o.MapFrom(s => s.Vehicle.Status))
            .ForMember(d => d.CapitalAccountId, o => o.MapFrom(s => s.DepositTransaction.CapitalAccountId))
            .MaxDepth(4);
        CreateMap<VehicleStatusHistory, VehicleStatusHistoryDto>().MaxDepth(4);
        CreateMap<VehicleListing, VehicleListingDto>()
            .ForMember(d => d.ListingId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Vehicle.Status)).MaxDepth(4);
        CreateMap<VehicleCostPlanItem, VehicleCostPlanItemDto>()
            .ForMember(d => d.ActualPaid, o => o.MapFrom(s => s.VehicleExpenses.Where(e => !e.IsReversed).Sum(e => (decimal?)e.Amount) ?? 0m))
            .MaxDepth(4);
        CreateMap<VehicleExpense, VehicleExpenseDto>()
            .ForMember(d => d.CostPlanDescription, o => o.MapFrom(s => s.CostPlanItem == null ? null : s.CostPlanItem.Description))
            .ForMember(d => d.CapitalAccountId, o => o.MapFrom(s => s.FinancialTransaction.CapitalAccountId))
            .ForMember(d => d.CapitalAccountName, o => o.MapFrom(s => s.FinancialTransaction.CapitalAccount.Name))
            .ForMember(d => d.Currency, o => o.MapFrom(s => s.FinancialTransaction.CapitalAccount.Currency))
            .MaxDepth(4);
        CreateMap<Vehicle, VehicleDetailsDto>()
            .ForMember(d => d.ActualPurchasePrice, o => o.Ignore())
            .ForMember(d => d.ActualExpenses, o => o.Ignore())
            .ForMember(d => d.TotalInvested, o => o.Ignore())
            .ForMember(d => d.RemainingProjectedCosts, o => o.Ignore())
            .ForMember(d => d.ProjectedFinalCost, o => o.Ignore())
            .ForMember(d => d.ExpectedSellingPrice, o => o.Ignore())
            .ForMember(d => d.ProjectedProfit, o => o.Ignore())
            .ForMember(d => d.ProjectedROI, o => o.Ignore())
            .ForMember(d => d.ActualSalePrice, o => o.Ignore())
            .ForMember(d => d.SoldAt, o => o.Ignore())
            .ForMember(d => d.RealizedProfit, o => o.Ignore())
            .ForMember(d => d.RealizedROI, o => o.Ignore())
            .ForMember(d => d.OriginalForecast, o => o.Ignore())
            .ForMember(d => d.CurrentListing, o => o.Ignore())
            .ForMember(d => d.ListingPrice, o => o.Ignore())
            .ForMember(d => d.ListedAt, o => o.Ignore())
            .ForMember(d => d.ActiveReservation, o => o.Ignore())
            .ForMember(d => d.SaleAccount, o => o.Ignore())
            .MaxDepth(4);
        CreateMap<Vehicle, VehicleListItemDto>()
            .ForMember(d => d.ActualPurchasePrice, o => o.Ignore())
            .ForMember(d => d.ActualExpenses, o => o.Ignore())
            .ForMember(d => d.TotalInvested, o => o.Ignore())
            .ForMember(d => d.RemainingProjectedCosts, o => o.Ignore())
            .ForMember(d => d.ProjectedFinalCost, o => o.Ignore())
            .ForMember(d => d.ExpectedSellingPrice, o => o.Ignore())
            .ForMember(d => d.ProjectedProfit, o => o.Ignore())
            .ForMember(d => d.ProjectedROI, o => o.Ignore())
            .ForMember(d => d.ActualSalePrice, o => o.Ignore())
            .ForMember(d => d.SoldAt, o => o.Ignore())
            .ForMember(d => d.RealizedProfit, o => o.Ignore())
            .ForMember(d => d.RealizedROI, o => o.Ignore())
            .ForMember(d => d.ListingPrice, o => o.Ignore())
            .MaxDepth(4);
    }
}
