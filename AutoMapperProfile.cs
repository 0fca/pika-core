using AutoMapper;
using Minio.DataModel;
using Pika.Domain.Storage.Entity;
using PikaCore.Areas.Admin.Models.CategoryViewModels;
using PikaCore.Areas.Core.Commands;
using PikaCore.Areas.Core.Models.DTO;
using PikaCore.Areas.Core.Models.File;

namespace PikaCore;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Item, ObjectInfo>()
            .ForMember(d => d.Name, 
                opt => 
                    opt.MapFrom(src => src.Key))
            .ForMember(d => d.Size, 
                opt => 
                    opt.MapFrom(src => src.Size))
            .ForMember(d => d.ETag, 
                opt => 
                    opt.MapFrom(src => src.ETag))
            .ForMember(d => d.LastModified, 
                opt => 
                    opt.MapFrom(src => src.LastModified));
        CreateMap<Category, CategoryDTO>()
            .ForMember(d => d.Guid,
                opt => 
                    opt.MapFrom(src => src.Id))
            .ForMember(d => d.Name,
                opt => 
                    opt.MapFrom(src => src.Name))
            .ForMember(d => d.Description,
                opt => 
                    opt.MapFrom(src => src.Description))
            .ForMember(d => d.Tags,
                opt => 
                    opt.MapFrom(src => src.Tags));
        CreateMap<EditCategoryViewModel, UpdateCategoryCommand>()
            .ForMember(d => d.Guid,
                opt => 
                    opt.MapFrom(src => src.Id))
            .ForMember(d => d.Name,
                opt => 
                    opt.MapFrom(src => src.Name))
            .ForMember(d => d.Description,
                opt => 
                    opt.MapFrom(src => src.Description))
            .ForMember(d => d.Mimes, opt => 
                opt.MapFrom(src => src.GetMimes()));
        CreateMap<Category, EditCategoryViewModel>()
            .ForMember(d => d.Id,
                opt => 
                    opt.MapFrom(src => src.Id))
            .ForMember(d => d.Name,
                opt => 
                    opt.MapFrom(src => src.Name))
            .ForMember(d => d.Description,
                opt => 
                    opt.MapFrom(src => src.Description))
            .ForMember(d => d.Mimes, opt => 
                opt.MapFrom(src => src.GetMimesAsString()));
    }
}