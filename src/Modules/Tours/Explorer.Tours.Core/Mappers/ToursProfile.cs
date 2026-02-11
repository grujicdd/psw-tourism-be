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

        CreateMap<KeyPointDto, KeyPoint>().ReverseMap();

        // Purchase-related mappings
        CreateMap<ShoppingCartDto, ShoppingCart>().ReverseMap();

        CreateMap<TourPurchaseDto, TourPurchase>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (Domain.PurchaseStatus)src.Status))
            .ReverseMap()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (API.Dtos.PurchaseStatus)src.Status));

        CreateMap<BonusPointsDto, BonusPoints>().ReverseMap();

        CreateMap<BonusTransactionDto, BonusTransaction>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (Domain.BonusTransactionType)src.Type))
            .ReverseMap()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (API.Dtos.BonusTransactionType)src.Type));

        // Add mapping for PagedResult
        CreateMap<PagedResult<Tour>, PagedResult<TourDto>>();
        CreateMap<PagedResult<TourPurchase>, PagedResult<TourPurchaseDto>>();
        CreateMap<PagedResult<BonusTransaction>, PagedResult<BonusTransactionDto>>();

        CreateMap<TourReviewDto, TourReview>().ReverseMap();
        CreateMap<TourReviewCreateDto, TourReview>()
            .ForMember(dest => dest.TouristId, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewDate, opt => opt.Ignore());

        CreateMap<TourReplacementDto, TourReplacement>()
        .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (Domain.TourReplacementStatus)src.Status))
        .ReverseMap()
        .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (API.Dtos.TourReplacementStatus)src.Status));

            CreateMap<TourReplacementCreateDto, TourReplacement>()
                .ForMember(dest => dest.OriginalGuideId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.RequestedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ReplacementGuideId, opt => opt.Ignore())
                .ForMember(dest => dest.AcceptedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CancelledAt, opt => opt.Ignore());
    }
}