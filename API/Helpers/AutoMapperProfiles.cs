using System.Linq;
using API.DTOs;
using API.Entities;
using AutoMapper;
using API.Extensions;
using System;

namespace API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<AppUser, MemberDto>()
                .ForMember(
                    dest => dest.PhotoUrl,
                    opt => opt.MapFrom(src => src.Photos.FirstOrDefault(x => (x.IsMain ?? false)).Url))
                .ForMember(dest => dest.Age, memberOptions => memberOptions.MapFrom(src => src.DateOfBirth.CalculateAge()));
            CreateMap<Photo, PhotoDto>();
            CreateMap<MemberUpdateDto, AppUser>();
            CreateMap<RegisterDto, AppUser>();
            CreateMap<Message, MessageDto>()
                .ForMember(dest => dest.SenderPhotoUrl, opt => opt.MapFrom(src => src.Sender.Photos.FirstOrDefault(x => (x.IsMain ?? false)).Url))
                .ForMember(dest => dest.RecepientPhotoUrl, opt => opt.MapFrom(src => src.Recipient.Photos.FirstOrDefault(x => (x.IsMain ?? false)).Url));
            CreateMap<DateTime, DateTime>().ConvertUsing(d => DateTime.SpecifyKind(d, DateTimeKind.Utc));
        }
    }
}