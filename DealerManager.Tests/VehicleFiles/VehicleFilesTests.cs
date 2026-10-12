using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.IRepository;
using DealerManager.Application.IService.Storage;
using DealerManager.Application.IService.Vehicle;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using DealerManager.Infrastructure.Service.Vehicle;
using DealerManager.Infrastructure.Storage;
using DealerManager.Tests.Finance;
using DealerManager.Tests.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace DealerManager.Tests.VehicleFiles;
public class VehicleFilesTests
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aOXsAAAAASUVORK5CYII=");
    private static readonly byte[] Pdf = "%PDF-1.4\nexample document\n%%EOF"u8.ToArray();
    private static async Task<int> Seed(DealerManagerDbContext db, VehicleStatus status = VehicleStatus.Purchased)
    {
        var vehicle = new DealerManager.Domain.Entities.Vehicle { Make = "BMW", Model = "X1", Year = 2020, Status = status, PurchaseDate = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow };
        db.Add(vehicle); await db.SaveChangesAsync(); return vehicle.Id;
    }
    private static MultipartFormDataContent Form(bool photo, byte[]? bytes = null, string? name = null, string? mime = null, int count = 1)
    {
        var form = new MultipartFormDataContent(); form.Add(new StringContent("1"), "type");
        if (!photo) { form.Add(new StringContent("  Италиански талон  "), "title"); form.Add(new StringContent("2026-10-12T00:00:00Z"), "documentDate"); }
        for (var i = 0; i < count; i++) {
            var content = new ByteArrayContent(bytes ?? (photo ? Png : Pdf)); content.Headers.ContentType = new MediaTypeHeaderValue(mime ?? (photo ? "image/png" : "application/pdf"));
            form.Add(content, photo ? "files" : "file", name ?? (photo ? "IMG_1234.png" : "registration.pdf"));
        }
        return form;
    }
    private static async Task<List<VehiclePhotoDto>> Upload(HttpClient client, int id, int count = 1)
    {
        using var form = Form(true, count: count); var response = await client.PostAsync($"/api/vehicles/{id}/photos", form);
        response.EnsureSuccessStatusCode(); var json = await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("storageKey", json);
        return (await response.Content.ReadFromJsonAsync<List<VehiclePhotoDto>>())!;
    }
    [Fact]
    public async Task MultiplePhotosPrimaryContentAndDeletionPreserveVehicle()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var id = await Seed(db); var other = await Seed(db); var uploaded = await Upload(client, id, 3);
        Assert.Equal(3, uploaded.Count); Assert.True(uploaded[0].IsPrimary); Assert.Equal(new[] {0,1,2}, uploaded.Select(p => p.SortOrder));
        Assert.All(uploaded, p => { Assert.Equal(1, p.UploadedByUserId); Assert.Equal(factory.Clock.Now, p.UploadedAt); });
        Assert.Equal(3, factory.Storage.Files.Length);
        var metadata = await db.VehiclePhotos.AsNoTracking().ToListAsync(); Assert.All(metadata, p => { Assert.NotEqual(p.OriginalFileName, Path.GetFileName(p.StorageKey)); Assert.StartsWith($"dealerships/1/vehicles/{id}/photos/", p.StorageKey); Assert.Equal(Png.Length, p.SizeBytes); });
        Assert.Equal(Png, await client.GetByteArrayAsync($"/api/vehicles/{id}/photos/{uploaded[0].Id}/content"));
        var content = await client.GetAsync($"/api/vehicles/{id}/photos/{uploaded[0].Id}/content"); Assert.Equal("image/png", content.Content.Headers.ContentType!.MediaType); Assert.True(content.Headers.CacheControl!.NoStore);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/vehicles/{other}/photos/{uploaded[0].Id}/content")).StatusCode);
        var more = await Upload(client, id); Assert.False(more[0].IsPrimary); Assert.Equal(3, more[0].SortOrder);
        (await client.PutAsJsonAsync($"/api/vehicles/{id}/photos/{uploaded[1].Id}/primary", new { })).EnsureSuccessStatusCode();
        var list = (await client.GetFromJsonAsync<List<VehiclePhotoDto>>($"/api/vehicles/{id}/photos"))!; Assert.Equal(uploaded[1].Id, list[0].Id); Assert.Single(list, p => p.IsPrimary);
        (await client.DeleteAsync($"/api/vehicles/{id}/photos/{more[0].Id}")).EnsureSuccessStatusCode();
        (await client.DeleteAsync($"/api/vehicles/{id}/photos/{uploaded[1].Id}")).EnsureSuccessStatusCode();
        list = (await client.GetFromJsonAsync<List<VehiclePhotoDto>>($"/api/vehicles/{id}/photos"))!; Assert.Equal(uploaded[0].Id, list[0].Id); Assert.True(list[0].IsPrimary);
        foreach (var photo in list) (await client.DeleteAsync($"/api/vehicles/{id}/photos/{photo.Id}")).EnsureSuccessStatusCode();
        Assert.Empty((await client.GetFromJsonAsync<List<VehiclePhotoDto>>($"/api/vehicles/{id}/photos"))!); Assert.Empty(factory.Storage.Files); Assert.Equal(2, await db.Vehicles.CountAsync());
    }
    [Fact]
    public async Task DocumentsUploadEditDownloadDeleteAndMissingContentAreHandled()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var id = await Seed(db, VehicleStatus.Sold); var other = await Seed(db);
        using var form = Form(false); var response = await client.PostAsync($"/api/vehicles/{id}/documents", form); response.EnsureSuccessStatusCode(); Assert.DoesNotContain("storageKey", await response.Content.ReadAsStringAsync());
        var doc = (await response.Content.ReadFromJsonAsync<VehicleDocumentDto>())!; Assert.Equal("Италиански талон", doc.Title);
        Assert.Single((await client.GetFromJsonAsync<List<VehicleDocumentDto>>($"/api/vehicles/{id}/documents"))!);
        var physical = Assert.Single(factory.Storage.Files); Assert.Equal(Pdf, await File.ReadAllBytesAsync(physical));
        (await client.PutAsJsonAsync($"/api/vehicles/{id}/documents/{doc.Id}", new { type = 2, title = "  Updated title  ", notes = "Notes" })).EnsureSuccessStatusCode();
        Assert.Equal(Pdf, await File.ReadAllBytesAsync(physical)); Assert.Equal("Updated title", (await client.GetFromJsonAsync<List<VehicleDocumentDto>>($"/api/vehicles/{id}/documents"))![0].Title);
        var download = await client.GetAsync($"/api/vehicles/{id}/documents/{doc.Id}/content?download=true"); download.EnsureSuccessStatusCode(); Assert.Equal(Pdf, await download.Content.ReadAsByteArrayAsync()); Assert.Contains("registration.pdf", download.Content.Headers.ContentDisposition!.ToString());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/vehicles/{other}/documents/{doc.Id}/content")).StatusCode);
        File.Delete(physical); var missing = await client.GetAsync($"/api/vehicles/{id}/documents/{doc.Id}/content"); Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode); Assert.Contains("missingContent", await missing.Content.ReadAsStringAsync());
        (await client.DeleteAsync($"/api/vehicles/{id}/documents/{doc.Id}")).EnsureSuccessStatusCode(); Assert.Empty(await db.VehicleDocuments.ToListAsync()); Assert.Equal(2, await db.Vehicles.CountAsync());
        // A real existing file is deleted too, and sold vehicles allow historical uploads.
        using var nextForm = Form(false); var nextResponse = await client.PostAsync($"/api/vehicles/{id}/documents", nextForm); nextResponse.EnsureSuccessStatusCode(); var next = (await nextResponse.Content.ReadFromJsonAsync<VehicleDocumentDto>())!;
        (await client.DeleteAsync($"/api/vehicles/{id}/documents/{next.Id}")).EnsureSuccessStatusCode(); Assert.Empty(factory.Storage.Files);
    }
    [Theory]
    [InlineData(true, "image.svg", "image/svg+xml", false)] [InlineData(false, "file.exe", "application/octet-stream", false)]
    [InlineData(true, "fake.png", "image/png", true)] [InlineData(false, "fake.pdf", "application/pdf", true)]
    [InlineData(true, "image.png", "image/jpeg", false)] [InlineData(false, "image.webp", "image/webp", false)]
    public async Task InvalidTypesMimeAndSignaturesWriteNothing(bool photo, string name, string mime, bool fake)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>(); var id = await Seed(db);
        using var form = Form(photo, fake ? "not a real file"u8.ToArray() : null, name, mime);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"/api/vehicles/{id}/{(photo ? "photos" : "documents")}", form)).StatusCode);
        Assert.Empty(factory.Storage.Files); Assert.Empty(await db.VehiclePhotos.ToListAsync()); Assert.Empty(await db.VehicleDocuments.ToListAsync());
    }
    [Theory] [InlineData(true)] [InlineData(false)]
    public async Task OversizeFilesAreRejectedBeforeStorage(bool photo)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>(); var id = await Seed(db);
        var service = scope.ServiceProvider.GetRequiredService<IVehicleFilesService>(); using var stream = new MemoryStream(photo ? Png : Pdf);
        var file = new VehicleFileUpload(stream, photo ? "test.png" : "test.pdf", photo ? "image/png" : "application/pdf", (photo ? 10 : 20) * 1024 * 1024 + 1);
        await Assert.ThrowsAsync<VehicleFileException>(async () => { if (photo) await service.UploadPhotos(id, new() { Type = VehiclePhotoType.Other }, [file], default); else await service.UploadDocument(id, new() { Type = VehicleDocumentType.Other, Title = "Document" }, file, default); });
        Assert.Empty(factory.Storage.Files);
    }
    private class FailingUnitOfWork(IUnitOfWork inner, int failAt = 1) : IUnitOfWork {
        private int calls;
        public Task<int> SaveChanges(CancellationToken token) => ++calls == failAt ? throw new InvalidOperationException("Injected DB failure") : inner.SaveChanges(token);
        public Task<T> ExecuteInTransaction<T>(Func<CancellationToken, Task<T>> operation, CancellationToken token) => inner.ExecuteInTransaction(operation, token);
    }
    [Theory] [InlineData(true)] [InlineData(false)]
    public async Task DbFailureCleansUpAndDeleteFailureDoesNotRestoreMetadata(bool photo)
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>(); var id = await Seed(db);
        var service = ActivatorUtilities.CreateInstance<VehicleFilesService>(scope.ServiceProvider, new FailingUnitOfWork(scope.ServiceProvider.GetRequiredService<IUnitOfWork>()));
        using var stream = new MemoryStream(photo ? Png : Pdf); var file = new VehicleFileUpload(stream, photo ? "x.png" : "x.pdf", photo ? "image/png" : "application/pdf", stream.Length);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => { if (photo) await service.UploadPhotos(id, new() { Type = VehiclePhotoType.Other }, [file], default); else await service.UploadDocument(id, new() { Type = VehicleDocumentType.Other, Title = "Doc" }, file, default); });
        Assert.Empty(factory.Storage.Files); Assert.True(factory.Storage.DeleteAttempts > 0); Assert.Empty(await db.VehiclePhotos.ToListAsync()); Assert.Empty(await db.VehicleDocuments.ToListAsync());
        using var form = Form(photo); var result = await client.PostAsync($"/api/vehicles/{id}/{(photo ? "photos" : "documents")}", form); result.EnsureSuccessStatusCode();
        var fileId = photo ? (await result.Content.ReadFromJsonAsync<List<VehiclePhotoDto>>())![0].Id : (await result.Content.ReadFromJsonAsync<VehicleDocumentDto>())!.Id;
        factory.Storage.FailDelete = true;
        (await client.DeleteAsync($"/api/vehicles/{id}/{(photo ? "photos" : "documents")}/{fileId}")).EnsureSuccessStatusCode();
        Assert.Empty(await db.VehiclePhotos.ToListAsync()); Assert.Empty(await db.VehicleDocuments.ToListAsync()); Assert.Single(factory.Storage.Files);
    }
    [Fact]
    public async Task AllForeignFileEndpointsAndWrongVehiclePairsReturn404()
    {
        using var factory = new AuthApiFactory(); var (a, userA) = await factory.Register("files-a@example.test", "A"); using var ca = a;
        var (b, userB) = await factory.Register("files-b@example.test", "B"); using var cb = b;
        using var dbA = factory.TenantDb(userA.Dealership.Id); using var dbB = factory.TenantDb(userB.Dealership.Id);
        var idA = await Seed(dbA); var idB = await Seed(dbB); var photo = (await Upload(b, idB))[0];
        using var form = Form(false); var upload = await b.PostAsync($"/api/vehicles/{idB}/documents", form); upload.EnsureSuccessStatusCode(); var doc = (await upload.Content.ReadFromJsonAsync<VehicleDocumentDto>())!;
        foreach (var id in new[] {idB, idA}) {
            foreach (var suffix in new[] { $"photos/{photo.Id}/content", $"documents/{doc.Id}/content" }) Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync($"/api/vehicles/{id}/{suffix}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await a.DeleteAsync($"/api/vehicles/{id}/photos/{photo.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await a.PutAsJsonAsync($"/api/vehicles/{id}/photos/{photo.Id}/primary", new {})).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await a.DeleteAsync($"/api/vehicles/{id}/documents/{doc.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await a.PutAsJsonAsync($"/api/vehicles/{id}/documents/{doc.Id}", new { type = 0, title = "Attack" })).StatusCode);
        }
        foreach (var kind in new[] {"photos", "documents"}) {
            Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync($"/api/vehicles/{idB}/{kind}")).StatusCode);
            using var attack = Form(kind == "photos"); Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsync($"/api/vehicles/{idB}/{kind}", attack)).StatusCode);
        }
        Assert.Equal(2, factory.Storage.Files.Length);
        Assert.StartsWith($"dealerships/{userB.Dealership.Id}/", (await dbB.VehiclePhotos.SingleAsync()).StorageKey);
        Assert.Equal(userB.Id, photo.UploadedByUserId);
    }
    [Fact]
    public async Task PrimaryChangeFailureRestoresPreviousPrimary()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var id = await Seed(db); var photos = await Upload(client, id, 2); db.ChangeTracker.Clear();
        var service = ActivatorUtilities.CreateInstance<VehicleFilesService>(scope.ServiceProvider, new FailingUnitOfWork(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), 2));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetPrimary(id, photos[1].Id, default));
        Assert.Equal(photos[0].Id, (await db.VehiclePhotos.SingleAsync(p => p.IsPrimary)).Id); Assert.Equal(2, factory.Storage.Files.Length);
    }
    [Fact]
    public async Task SaleKeepsPhotosAndDocumentsAndAllowsHistoricalUploads()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient(); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var id = await Seed(db, VehicleStatus.Listed);
        var account = new CapitalAccount { Name = "Sales", Currency = "EUR", IsActive = true }; db.Add(account);
        db.VehicleListings.Add(new() { VehicleId = id, ListingPrice = 7000, IsActive = true, ListedAt = factory.Clock.Now, CreatedAt = factory.Clock.Now }); await db.SaveChangesAsync();
        var photo = (await Upload(client, id))[0]; using var form = Form(false);
        var uploaded = await client.PostAsync($"/api/vehicles/{id}/documents", form); uploaded.EnsureSuccessStatusCode(); var document = (await uploaded.Content.ReadFromJsonAsync<VehicleDocumentDto>())!;
        (await client.PostAsJsonAsync($"/api/vehicles/{id}/sale", new { actualSalePrice = 7000, capitalAccountId = account.Id })).EnsureSuccessStatusCode();
        Assert.Equal(Png, await client.GetByteArrayAsync($"/api/vehicles/{id}/photos/{photo.Id}/content"));
        Assert.Equal(Pdf, await client.GetByteArrayAsync($"/api/vehicles/{id}/documents/{document.Id}/content"));
        using var historical = Form(false); (await client.PostAsync($"/api/vehicles/{id}/documents", historical)).EnsureSuccessStatusCode();
        Assert.Equal(3, factory.Storage.Files.Length); Assert.Equal(2, (await client.GetFromJsonAsync<List<VehicleDocumentDto>>($"/api/vehicles/{id}/documents"))!.Count);
    }
    [Fact]
    public async Task LocalStorageRejectsTraversalAndEnforcesActualStreamSize()
    {
        using var storage = new TestFileStorage();
        await Assert.ThrowsAsync<IOException>(() => storage.OpenReadAsync("../secret.pdf", default));
        await Assert.ThrowsAsync<IOException>(() => storage.DeleteAsync("C:/secret.pdf", default));
        using var stream = new MemoryStream(new byte[100]);
        await Assert.ThrowsAsync<VehicleFileException>(() => storage.SaveAsync(stream, "dealerships/1/vehicles/1/photos", ".png", 50, default));
        Assert.Empty(storage.Files);
    }
}
