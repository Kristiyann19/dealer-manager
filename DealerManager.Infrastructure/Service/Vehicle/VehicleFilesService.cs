#nullable enable
using AutoMapper;
using AutoMapper.QueryableExtensions;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.FilterDtos;
using DealerManager.Application.IRepository;
using DealerManager.Application.IService.Identity;
using DealerManager.Application.IService.Storage;
using DealerManager.Application.IService.Vehicle;
using DealerManager.Domain.Entities;
using DealerManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using VehicleEntity = DealerManager.Domain.Entities.Vehicle;
namespace DealerManager.Infrastructure.Service.Vehicle;
public sealed class VehicleFilesService(
    IBaseRepository<VehicleEntity, FilterDto<VehicleEntity>, DealerManagerDbContext> vehicles,
    IBaseRepository<VehiclePhoto, FilterDto<VehiclePhoto>, DealerManagerDbContext> photos,
    IBaseRepository<VehicleDocument, FilterDto<VehicleDocument>, DealerManagerDbContext> documents,
    IUnitOfWork unitOfWork, IFileStorage storage, ICurrentUserContext user, TimeProvider clock,
    IMapper mapping, ILogger<VehicleFilesService> logger) : IVehicleFilesService
{
    private readonly IMapper mapper = mapping;
    private async Task<VehicleEntity> Vehicle(int id, CancellationToken token) => await vehicles.GetById(id, token) ?? throw new VehicleNotFoundException(id);
    private static void Validate(object metadata) => Validator.ValidateObject(metadata, new ValidationContext(metadata), true);
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static VehicleFileException Missing() => new("notFound", "Vehicle file not found.", 404);
    private async Task Cleanup(string key)
    {
        try { await storage.DeleteAsync(key, CancellationToken.None); }
        catch (Exception e) { logger.LogWarning(e, "Could not clean up private vehicle file {StorageKey}", key); }
    }
    public async Task<IReadOnlyList<VehiclePhotoDto>> Photos(int vehicleId, CancellationToken token)
    {
        await Vehicle(vehicleId, token);
        return await photos.GetQueryByProperties(p => p.VehicleId == vehicleId).OrderByDescending(p => p.IsPrimary)
            .ThenBy(p => p.SortOrder).ThenBy(p => p.UploadedAt).ThenBy(p => p.Id)
            .ProjectTo<VehiclePhotoDto>(mapper.ConfigurationProvider).ToListAsync(token);
    }
    public async Task<IReadOnlyList<VehiclePhotoDto>> UploadPhotos(int vehicleId, VehiclePhotoMetadata metadata, IReadOnlyList<VehicleFileUpload> files, CancellationToken token)
    {
        var vehicle = await Vehicle(vehicleId, token); Validate(metadata);
        if (files.Count is < 1 or > 10) throw new VehicleFileException("photoCount", "Select between 1 and 10 photos.");
        var validated = new List<(string Name, string Extension, string Mime)>();
        foreach (var file in files) validated.Add(await VehicleFileValidation.Validate(file, true, token));
        var keys = new List<string>();
        try
        {
            return await unitOfWork.ExecuteInTransaction<IReadOnlyList<VehiclePhotoDto>>(async ct => {
                await Vehicle(vehicleId, ct);
                var hasPrimary = await photos.AnyEntity(p => p.VehicleId == vehicleId && p.IsPrimary, ct);
                var nextOrder = (await photos.GetQueryByProperties(p => p.VehicleId == vehicleId).MaxAsync(p => (int?)p.SortOrder, ct) ?? -1) + 1;
                var created = new List<VehiclePhoto>();
                for (var i = 0; i < files.Count; i++)
                {
                    var info = validated[i];
                    var stored = await storage.SaveAsync(files[i].Stream, $"dealerships/{vehicle.DealershipId}/vehicles/{vehicleId}/photos", info.Extension, VehicleFileValidation.PhotoLimit, ct);
                    keys.Add(stored.StorageKey);
                    var photo = new VehiclePhoto { VehicleId = vehicleId, Type = metadata.Type!.Value,
                        Caption = Trim(metadata.Caption), OriginalFileName = info.Name, ContentType = info.Mime,
                        StorageKey = stored.StorageKey, SizeBytes = stored.SizeBytes, IsPrimary = !hasPrimary && i == 0,
                        SortOrder = checked(nextOrder + i), UploadedAt = clock.GetUtcNow(), UploadedByUserId = user.UserId };
                    await photos.Create(photo); created.Add(photo);
                }
                await unitOfWork.SaveChanges(ct);
                return mapper.Map<List<VehiclePhotoDto>>(created);
            }, token);
        }
        catch (Exception exception)
        {
            foreach (var key in keys) await Cleanup(key);
            if (exception is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_VehiclePhotos_VehicleId" } })
                throw new VehicleFileException("conflict", "The primary photo changed concurrently. Reload before retrying.", 409);
            throw;
        }
    }
    public async Task SetPrimary(int vehicleId, int photoId, CancellationToken token)
    {
        await unitOfWork.ExecuteInTransaction(async ct => {
            await Vehicle(vehicleId, ct);
            var photo = await photos.GetByProperties(p => p.Id == photoId && p.VehicleId == vehicleId, ct, q => q.AsTracking()) ?? throw Missing();
            if (photo.IsPrimary) return true;
            var old = await photos.GetByProperties(p => p.VehicleId == vehicleId && p.IsPrimary, ct, q => q.AsTracking());
            // Flush the old flag before setting the new one: partial unique indexes are immediate.
            if (old != null) { old.IsPrimary = false; await unitOfWork.SaveChanges(ct); }
            photo.IsPrimary = true; await unitOfWork.SaveChanges(ct); return true;
        }, token);
    }
    public async Task DeletePhoto(int vehicleId, int photoId, CancellationToken token)
    {
        var key = await unitOfWork.ExecuteInTransaction(async ct => {
            await Vehicle(vehicleId, ct);
            var photo = await photos.GetByProperties(p => p.Id == photoId && p.VehicleId == vehicleId, ct, q => q.AsTracking()) ?? throw Missing();
            photo.Vehicle = null!;
            photos.Delete(photo); await unitOfWork.SaveChanges(ct);
            if (photo.IsPrimary)
            {
                var next = await photos.GetQueryByProperties(p => p.VehicleId == vehicleId, q => q.AsTracking())
                    .OrderBy(p => p.SortOrder).ThenBy(p => p.UploadedAt).ThenBy(p => p.Id).FirstOrDefaultAsync(ct);
                if (next != null) { next.IsPrimary = true; await unitOfWork.SaveChanges(ct); }
            }
            return photo.StorageKey;
        }, token);
        await Cleanup(key);
    }
    private async Task<VehicleFileContent> Content(string key, string name, string mime, CancellationToken token)
    {
        try { return new(await storage.OpenReadAsync(key, token), name, mime); }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            logger.LogWarning(e, "Private vehicle file is missing: {StorageKey}", key);
            throw new VehicleFileException("missingContent", "The stored file is unavailable.", 404);
        }
    }
    public async Task<VehicleFileContent> PhotoContent(int vehicleId, int photoId, CancellationToken token)
    {
        await Vehicle(vehicleId, token);
        var photo = await photos.GetByProperties(p => p.Id == photoId && p.VehicleId == vehicleId, token) ?? throw Missing();
        return await Content(photo.StorageKey, photo.OriginalFileName, photo.ContentType, token);
    }
    public async Task<IReadOnlyList<VehicleDocumentDto>> Documents(int vehicleId, CancellationToken token)
    {
        await Vehicle(vehicleId, token);
        return await documents.GetQueryByProperties(d => d.VehicleId == vehicleId).OrderByDescending(d => d.UploadedAt).ThenByDescending(d => d.Id)
            .ProjectTo<VehicleDocumentDto>(mapper.ConfigurationProvider).ToListAsync(token);
    }
    public async Task<VehicleDocumentDto> UploadDocument(int vehicleId, VehicleDocumentMetadata metadata, VehicleFileUpload file, CancellationToken token)
    {
        var vehicle = await Vehicle(vehicleId, token); Validate(metadata);
        var info = await VehicleFileValidation.Validate(file, false, token);
        string? key = null;
        try
        {
            return await unitOfWork.ExecuteInTransaction(async ct => {
                await Vehicle(vehicleId, ct);
                var stored = await storage.SaveAsync(file.Stream, $"dealerships/{vehicle.DealershipId}/vehicles/{vehicleId}/documents", info.Extension, VehicleFileValidation.DocumentLimit, ct);
                key = stored.StorageKey;
                var document = new VehicleDocument { VehicleId = vehicleId, Type = metadata.Type!.Value, Title = metadata.Title.Trim(),
                    DocumentDate = metadata.DocumentDate?.ToUniversalTime(), Notes = Trim(metadata.Notes), OriginalFileName = info.Name,
                    ContentType = info.Mime, StorageKey = key, SizeBytes = stored.SizeBytes, UploadedAt = clock.GetUtcNow(), UploadedByUserId = user.UserId };
                await documents.Create(document); await unitOfWork.SaveChanges(ct); return mapper.Map<VehicleDocumentDto>(document);
            }, token);
        }
        catch { if (key != null) await Cleanup(key); throw; }
    }
    public async Task<VehicleDocumentDto> EditDocument(int vehicleId, int documentId, VehicleDocumentMetadata metadata, CancellationToken token)
    {
        return await unitOfWork.ExecuteInTransaction(async ct => {
            await Vehicle(vehicleId, ct);
            var document = await documents.GetByProperties(d => d.Id == documentId && d.VehicleId == vehicleId, ct, q => q.AsTracking()) ?? throw Missing();
            Validate(metadata);
            document.Type = metadata.Type!.Value; document.Title = metadata.Title.Trim();
            document.DocumentDate = metadata.DocumentDate?.ToUniversalTime(); document.Notes = Trim(metadata.Notes);
            await unitOfWork.SaveChanges(ct); return mapper.Map<VehicleDocumentDto>(document);
        }, token);
    }
    public async Task DeleteDocument(int vehicleId, int documentId, CancellationToken token)
    {
        var key = await unitOfWork.ExecuteInTransaction(async ct => {
            await Vehicle(vehicleId, ct);
            var document = await documents.GetByProperties(d => d.Id == documentId && d.VehicleId == vehicleId, ct, q => q.AsTracking()) ?? throw Missing();
            document.Vehicle = null!;
            documents.Delete(document); await unitOfWork.SaveChanges(ct); return document.StorageKey;
        }, token);
        await Cleanup(key);
    }
    public async Task<VehicleFileContent> DocumentContent(int vehicleId, int documentId, CancellationToken token)
    {
        await Vehicle(vehicleId, token);
        var document = await documents.GetByProperties(d => d.Id == documentId && d.VehicleId == vehicleId, token) ?? throw Missing();
        return await Content(document.StorageKey, document.OriginalFileName, document.ContentType, token);
    }
}
