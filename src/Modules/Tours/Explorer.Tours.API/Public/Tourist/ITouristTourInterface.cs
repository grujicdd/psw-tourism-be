// src/Modules/Tours/Explorer.Tours.API/Public/Tourist/ITouristTourService.cs
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using FluentResults;

namespace Explorer.Tours.API.Public.Tourist
{
    public interface ITouristTourService
    {
        Result<PagedResult<TourDto>> GetPublishedTours(int page, int pageSize, string? sortByDate = null);
        Result<TourDto> GetTour(long id);
        Result<PagedResult<TourDto>> GetFilteredTours(int page, int pageSize, int? category, int? difficulty, decimal? maxPrice, string? sortByDate = null);
        Result<IEnumerable<CategoryDto>> GetCategories();
    }
}
