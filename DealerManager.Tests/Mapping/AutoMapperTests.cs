using AutoMapper;
using DealerManager.Application.Dtos.Candidate;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.AutoMapperProfiles;
using DealerManager.WebAPI.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace DealerManager.Tests.Mapping;

public class AutoMapperTests
{
    private static ServiceProvider Services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.ConfigureAutoMapper();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void All_profiles_are_registered_and_valid()
    {
        using var services = Services();
        services.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Fact]
    public void Dossier_mapping_preserves_tenant_lifecycle_and_relationships()
    {
        using var services = Services();
        var vehicle = new Vehicle
        {
            Id = 7, DealershipId = 42, Status = VehicleStatus.Sold,
            Make = "BMW", Model = "320", SourceCandidateId = 3,
            PurchaseDate = DateTimeOffset.UtcNow,
            VehicleExpenses = [new VehicleExpense { Id = 9, Amount = 100 }]
        };
        var expenses = vehicle.VehicleExpenses;
        var purchased = vehicle.PurchaseDate;
        services.GetRequiredService<IMapper>().Map(new UpdateVehicleDossierRequest
        {
            Vin = " abc123 ", Color = " blue ", Notes = "  ", Mileage = 0
        }, vehicle);
        Assert.Equal(7, vehicle.Id);
        Assert.Equal(42, vehicle.DealershipId);
        Assert.Equal(VehicleStatus.Sold, vehicle.Status);
        Assert.Equal("BMW", vehicle.Make);
        Assert.Equal(3, vehicle.SourceCandidateId);
        Assert.Equal(purchased, vehicle.PurchaseDate);
        Assert.Same(expenses, vehicle.VehicleExpenses);
        Assert.Equal("ABC123", vehicle.Vin);
        Assert.Equal("blue", vehicle.Color);
        Assert.Null(vehicle.Notes);
        Assert.Equal(0, vehicle.Mileage);
    }

    [Fact]
    public void Estimate_mapping_orders_items_and_preserves_calculated_analysis()
    {
        using var services = Services();
        var analysis = new CandidateFinancialAnalysisDto { ExpectedProfit = 700 };
        var source = new CandidateEstimate
        {
            Id = 5, Version = 2, IsDecisionSnapshot = true,
            CandidateEstimateItems = [new() { Id = 2 }, new() { Id = 1 }]
        };
        var dto = services.GetRequiredService<IMapper>().Map(source,
            new CandidateEstimateDto { FinancialAnalysis = analysis });
        Assert.Equal(new[] { 1, 2 }, dto.Items.Select(item => item.Id));
        Assert.Same(analysis, dto.FinancialAnalysis);
        Assert.True(dto.IsDecisionSnapshot);
        Assert.Equal(2, dto.Version);
    }
}
