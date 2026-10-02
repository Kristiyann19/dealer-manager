using DriveType = DealerManager.Domain.Enums.DriveType;
using System.Net;
using System.Net.Http.Json;
using DealerManager.Application.Dtos.Candidate;
using DealerManager.Application.Dtos.Finance;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace DealerManager.Tests.Identity;
public class VehicleDossierTests
{
    private static async Task<T> Post<T>(HttpClient client, string path, object body)
    {
        var response = await client.PostAsJsonAsync(path, body); response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
    [Fact]
    public async Task DossierSurvivesPurchaseEditSaleAndSearchWithoutChangingCandidateOrFinancials()
    {
        using var factory = new AuthApiFactory();
        var (a, owner) = await factory.Register("dossier@example.test", "Dossier"); using var client = a;
        var candidate = await Post<CandidateDetailsDto>(client, "/api/candidates", new { make = "BMW", model = "320d", year = 2018, mileage = 140000, askingPrice = 3000, vin = "  original-vin  " });
        Assert.Equal("ORIGINAL-VIN", candidate.Vin);
        await Post<CandidateEstimateDto>(client, "/api/candidates/estimates", new { candidateId = candidate.Id, expectedSellingPrice = 8000, items = new[] { new { category = 0, estimatedAmount = 3000 } } });
        await Post<CandidateDetailsDto>(client, $"/api/candidates/{candidate.Id}/approve", new { });
        var account = await Post<CapitalAccountDto>(client, "/api/capital-accounts", new { name = "Main", currency = "EUR" });
        await Post<FinancialTransactionDto>(client, $"/api/capital-accounts/{account.Id}/contributions", new { amount = 10000 });
        var purchase = await Post<PurchaseCandidateResultDto>(client, $"/api/candidates/{candidate.Id}/purchase", new { capitalAccountId = account.Id, actualPurchasePrice = 3000 });
        var url = $"/api/vehicles/{purchase.VehicleId}";
        var before = (await client.GetFromJsonAsync<VehicleDetailsDto>(url))!;
        Assert.Equal("ORIGINAL-VIN", before.Vin); Assert.Equal(140000, before.Mileage); Assert.Equal("BMW", before.Make); Assert.Equal("320d", before.Model); Assert.Equal(2018, before.Year);
        Assert.Null(before.RegistrationNumber); Assert.Null(before.PowerHp);
        var response = await client.PutAsJsonAsync(url + "/details", new {
            vin = "  updated-vin  ", registrationNumber = " pb1234ab ", firstRegistration = "2018-06-01", mileage = 141000,
            fuelType = "Diesel", transmission = "Automatic", engineDisplacementCc = 1995, powerKw = 100, powerHp = 136,
            driveType = "RearWheelDrive", euroStandard = "Euro6", bodyType = "Sedan", color = "Black", numberOfDoors = 4,
            numberOfSeats = 5, numberOfKeys = 2, importedFrom = "Germany", notes = " Updated ",
            dealershipId = 999, status = 9, sourceCandidateId = 999, purchaseDate = "2000-01-01", actualSalePrice = 1, totalInvested = 1
        }); response.EnsureSuccessStatusCode();
        var updated = (await client.GetFromJsonAsync<VehicleDetailsDto>(url))!;
        Assert.Equal("UPDATED-VIN", updated.Vin); Assert.Equal("PB1234AB", updated.RegistrationNumber);
        Assert.Equal(new DateOnly(2018, 6, 1), updated.FirstRegistration); Assert.Equal(141000, updated.Mileage);
        Assert.Equal(FuelType.Diesel, updated.FuelType); Assert.Equal(Transmission.Automatic, updated.Transmission); Assert.Equal(1995, updated.EngineDisplacementCc);
        Assert.Equal(100, updated.PowerKw); Assert.Equal(136, updated.PowerHp); Assert.Equal(DriveType.RearWheelDrive, updated.DriveType);
        Assert.Equal(EuroStandard.Euro6, updated.EuroStandard); Assert.Equal(BodyType.Sedan, updated.BodyType); Assert.Equal("Black", updated.Color);
        Assert.Equal(4, updated.NumberOfDoors); Assert.Equal(5, updated.NumberOfSeats); Assert.Equal(2, updated.NumberOfKeys);
        Assert.Equal("Germany", updated.ImportedFrom); Assert.Equal("Updated", updated.Notes);
        Assert.Equal(before.Status, updated.Status); Assert.Equal(before.PurchaseDate, updated.PurchaseDate); Assert.Equal(before.SourceCandidateId, updated.SourceCandidateId);
        Assert.Equal(before.TotalInvested, updated.TotalInvested); Assert.Null(updated.ActualSalePrice);
        using var db = factory.TenantDb(owner.Dealership.Id); Assert.Equal(owner.Dealership.Id, (await db.Vehicles.SingleAsync()).DealershipId);
        var historical = (await client.GetFromJsonAsync<CandidateDetailsDto>($"/api/candidates/{candidate.Id}"))!;
        Assert.Equal("ORIGINAL-VIN", historical.Vin); Assert.Equal(140000, historical.Mileage);
        await Post<VehicleStatusHistoryDto>(client, url + "/status", new { status = "ReadyForSale" });
        await Post<VehicleListingDto>(client, url + "/listing", new { listingPrice = 8000 });
        await Post<VehicleSaleResultDto>(client, url + "/sale", new { actualSalePrice = 8000 });
        var sold = (await client.GetFromJsonAsync<VehicleDetailsDto>(url))!;
        Assert.Equal(VehicleStatus.Sold, sold.Status); Assert.Equal(updated.Vin, sold.Vin); Assert.Equal(updated.PowerHp, sold.PowerHp); Assert.Equal(updated.Notes, sold.Notes);
        foreach (var search in new[] { "updated-vin", "pb1234ab", "BMW", "320d" }) {
            var list = (await client.GetFromJsonAsync<VehicleListResultDto>("/api/vehicles?TextFilter=" + search))!;
            Assert.Equal(purchase.VehicleId, Assert.Single(list.Items).Id);
        }
    }
    [Fact]
    public async Task ForeignTenantCannotReadUpdateOrSearchDossier()
    {
        using var factory = new AuthApiFactory(); var (a, ownerA) = await factory.Register("a@example.test", "A"); using var clientA = a;
        var (b, ownerB) = await factory.Register("b@example.test", "B"); using var clientB = b;
        using var db = factory.TenantDb(ownerB.Dealership.Id);
        var vehicle = new DealerManager.Domain.Entities.Vehicle { Make = "BMW", Model = "X", Vin = "FOREIGN", RegistrationNumber = "FOREIGNREG" };
        db.Vehicles.Add(vehicle); await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await a.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}/details", new { vin = "ATTACK" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync($"/api/vehicles/{vehicle.Id}")).StatusCode);
        Assert.Empty((await a.GetFromJsonAsync<VehicleListResultDto>("/api/vehicles?TextFilter=FOREIGN"))!.Items);
        await db.Entry(vehicle).ReloadAsync(); Assert.Equal("FOREIGN", vehicle.Vin);
    }
    [Theory]
    [InlineData("fuelType", 999)] [InlineData("transmission", -1)] [InlineData("driveType", 999)] [InlineData("bodyType", 999)] [InlineData("euroStandard", 999)]
    [InlineData("mileage", -1)] [InlineData("powerKw", -1)] [InlineData("powerHp", 0)]
    [InlineData("engineDisplacementCc", 0)] [InlineData("numberOfDoors", -1)] [InlineData("numberOfSeats", 0)]
    [InlineData("numberOfKeys", -1)] [InlineData("numberOfDoors", 11)] [InlineData("numberOfKeys", 21)]
    public async Task InvalidNumbersDoNotUpdateDossier(string field, int value)
    {
        using var factory = new AuthApiFactory(); var (client, owner) = await factory.Register("a@example.test", "A"); using var owned = client;
        using var db = factory.TenantDb(owner.Dealership.Id); var vehicle = new DealerManager.Domain.Entities.Vehicle { Vin = "UNCHANGED" }; db.Vehicles.Add(vehicle); await db.SaveChangesAsync();
        var response = await client.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}/details", new Dictionary<string, object> { [field] = value, ["vin"] = "CHANGED" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); await db.Entry(vehicle).ReloadAsync(); Assert.Equal("UNCHANGED", vehicle.Vin);
    }
    [Fact]
    public async Task OptionalValuesCanBeClearedAndLongVinIsRejected()
    {
        using var factory = new AuthApiFactory(); var (client, owner) = await factory.Register("a@example.test", "A"); using var owned = client;
        using var db = factory.TenantDb(owner.Dealership.Id); var vehicle = new DealerManager.Domain.Entities.Vehicle { Vin = "OLD", RegistrationNumber = "OLD" }; db.Vehicles.Add(vehicle); await db.SaveChangesAsync();
        var url = $"/api/vehicles/{vehicle.Id}/details";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(url, new { vin = new string('X', 33) })).StatusCode);
        (await client.PutAsJsonAsync(url, new { vin = "  ", registrationNumber = "  " })).EnsureSuccessStatusCode();
        await db.Entry(vehicle).ReloadAsync(); Assert.Null(vehicle.Vin); Assert.Null(vehicle.RegistrationNumber);
    }
}
