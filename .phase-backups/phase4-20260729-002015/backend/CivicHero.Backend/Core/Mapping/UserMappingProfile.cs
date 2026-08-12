using AutoMapper;
using CivicHero.Backend.Core.DTOs.Users;
using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Mapping;

public sealed class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(destination => destination.Role, options => options.MapFrom(source => source.Role.ToString()))
            .ForMember(destination => destination.DepartmentName, options => options.MapFrom(source => source.Department == null ? null : source.Department.Name))
            .ForMember(destination => destination.WardName, options => options.MapFrom(source => source.Ward == null ? null : source.Ward.Name));

        CreateMap<User, UserProfileDto>()
            .ForMember(destination => destination.Role, options => options.MapFrom(source => source.Role.ToString()))
            .ForMember(destination => destination.DepartmentName, options => options.MapFrom(source => source.Department == null ? null : source.Department.Name))
            .ForMember(destination => destination.WardName, options => options.MapFrom(source => source.Ward == null ? null : source.Ward.Name));

        CreateMap<User, UserListDto>()
            .ForMember(destination => destination.Role, options => options.MapFrom(source => source.Role.ToString()))
            .ForMember(destination => destination.DepartmentName, options => options.MapFrom(source => source.Department == null ? null : source.Department.Name))
            .ForMember(destination => destination.WardName, options => options.MapFrom(source => source.Ward == null ? null : source.Ward.Name));
    }
}
