// src/Modules/Tours/Explorer.Tours.Core/Mappers/ToursProfile.cs
using AutoMapper;
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.Core.Domain;

namespace Explorer.Tours.Core.Mappers;

public class ToursProfile : Profile
{
    public ToursProfile()
    {
        CreateMap<EquipmentDto, Equipment>().ReverseMap();

        CreateMap<TourDto, Tour>()
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => (TourState)src.State))
            .ReverseMap()
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => (int)src.State));

        // Add mapping for PagedResult
        CreateMap<PagedResult<Tour>, PagedResult<TourDto>>();
    }
}