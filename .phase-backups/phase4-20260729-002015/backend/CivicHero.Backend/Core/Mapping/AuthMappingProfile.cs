using AutoMapper;
using CivicHero.Backend.Core.DTOs.Users;
using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core.Mapping;

public sealed class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(destination => destination.Role,
                options => options.MapFrom(source => source.Role.ToString()));
    }
}
