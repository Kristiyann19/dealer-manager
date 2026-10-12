using AutoMapper;
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Domain.Entities;
namespace DealerManager.Infrastructure.AutoMapperProfiles;
public sealed class VehicleFilesProfile : Profile
{
    public VehicleFilesProfile()
    {
        CreateMap<VehiclePhoto, VehiclePhotoDto>().MaxDepth(4);
        CreateMap<VehicleDocument, VehicleDocumentDto>().MaxDepth(4);
    }
}
