using AutoMapper;
using CivicHero.Backend.Core.DTOs.Complaints;
using CivicHero.Backend.Core.Entities;

namespace CivicHero.Backend.Core
{
    public class ComplaintMappingProfile : Profile
    {
        public ComplaintMappingProfile()
        {
            CreateMap<CreateComplaintDto, Complaint>();
            CreateMap<UpdateComplaintDto, Complaint>();
            CreateMap<Complaint, ComplaintDto>();
            CreateMap<ComplaintUpdate, ComplaintUpdateDto>();
            CreateMap<CreateComplaintUpdateDto, ComplaintUpdate>();
        }
    }
}