using System;
using System.Linq;
using AutoMapper;
using Marten;
using Minio.DataModel;
using Pika.Domain.Storage.Entity;
using Pika.Domain.Storage.Entity.View;
using PikaCore.Areas.Admin.Models.CategoryViewModels;
using PikaCore.Areas.Core.Commands;
using PikaCore.Areas.Core.Models.DTO;
using PikaCore.Areas.Core.Models.File;
using Bucket = Pika.Domain.Storage.Entity.Bucket;

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
        CreateMap<CategoriesView, CategoryDTO>()
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
        CreateMap<Category, CategoriesView>()
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
        CreateMap<BucketsView, BucketDTO>()
            .ForMember(d => d.Name,
                opt =>
                    opt.MapFrom(src => src.Name))
            .ForMember(d => d.Roles,
                opt => 
                    opt.MapFrom(src => src.RoleClaims));
        CreateMap<Bucket, BucketsView>()
            .ForMember(b => b.Id,
                opt =>
                    opt.MapFrom(src => src.Id))
            .ForMember(b => b.Name,
                opt =>
                    opt.MapFrom(src => src.Name))
            .ForMember(b => b.RoleClaims,
                opt =>
                    opt.MapFrom(src => src.RoleClaims));
        CreateMap<ObjectStat, ObjectInfo>()
            .ForMember(o => o.Name,
                opt =>
                    opt.MapFrom(src => src.ObjectName))
            .ForMember(o => o.Size,
                opt =>
                    opt.MapFrom(src => src.Size))
            .ForMember(o => o.ETag,
                opt =>
                    opt.MapFrom(src => src.ETag))
            .ForMember(o => o.LastModified,
                opt =>
                    opt.MapFrom(src => src.LastModified))
            .ForMember(o => o.MimeType,
                opt =>
                    opt.MapFrom(src => src.ContentType));
        CreateMap<ObjectInfo, ResourceInformationViewModel>()
            .ForMember(o => o.HumanName, 
                opt => 
                    opt.MapFrom(src => src.Name.Split("/", StringSplitOptions.None).Last()))
            .ForMember(o => o.FullName,
                opt =>
                    opt.MapFrom(src => src.Name))
            .ForMember(o => o.Size,
                opt =>
                    opt.MapFrom(src => src.Size))
            .ForMember(o => o.ETag,
                opt =>
                    opt.MapFrom(src => src.ETag))
            .ForMember(o => o.LastModified,
                opt =>
                    opt.MapFrom(src => src.LastModified))
            .ForMember(o => o.MimeType,
                opt =>
                    opt.MapFrom(src => src.MimeType));

        CreateMap<ObjectInfo, ObjectInfoDTO>()
            .ForMember(o => o.FullName,
                opt =>
                    opt.MapFrom(src => src.Name))

            .ForMember(o => o.FormattedSize,
                opt =>
                    opt.MapFrom(src => src.SizeWithUnit()))
            .ForMember(o => o.HumanName,
                opt =>
                    opt.MapFrom(src => src.Name.Split("/", StringSplitOptions.None).Last()))
            .ForMember(o => o.FormattedDateTime,
                opt =>
                    opt.MapFrom(src => src.LastModified.ToLocalTime().ToLongDateString()));
    }
}