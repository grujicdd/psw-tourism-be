// src/Modules/Tours/Explorer.Tours.Core/UseCases/Tourist/TouristTourService.cs
using AutoMapper;
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Explorer.Tours.Core.Domain;
using FluentResults;

namespace Explorer.Tours.Core.UseCases.Tourist
{
    public class TouristTourService : ITouristTourService
    {
        private readonly ICrudRepository<Tour> _tourRepository;
        private readonly IMapper _mapper;

        public TouristTourService(ICrudRepository<Tour> tourRepository, IMapper mapper)
        {
            _tourRepository = tourRepository;
            _mapper = mapper;
        }

        public Result<PagedResult<TourDto>> GetPublishedTours(int page, int pageSize)
        {
            try
            {
                // Get all published tours
                var allPublishedTours = _tourRepository.GetAll().Where(t => t.State == TourState.COMPLETE).ToList();
                var totalCount = allPublishedTours.Count;

                // Apply pagination
                var pagedTours = allPublishedTours.Skip(page * pageSize).Take(pageSize).ToList();

                // Map to DTOs
                var tourDtos = _mapper.Map<List<TourDto>>(pagedTours);

                // Create PagedResult manually
                var result = new PagedResult<TourDto>(tourDtos, totalCount);
                return Result.Ok(result);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error retrieving published tours: {ex.Message}");
            }
        }

        public Result<TourDto> GetTour(long id)
        {
            try
            {
                var tour = _tourRepository.Get(id);

                // Only allow access to published tours for tourists
                if (tour.State != TourState.COMPLETE)
                {
                    return Result.Fail(FailureCode.NotFound);
                }

                var tourDto = _mapper.Map<TourDto>(tour);
                return Result.Ok(tourDto);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error retrieving tour: {ex.Message}");
            }
        }

        public Result<PagedResult<TourDto>> GetFilteredTours(int page, int pageSize, int? category, int? difficulty, decimal? maxPrice)
        {
            try
            {
                var allTours = _tourRepository.GetAll().Where(t => t.State == TourState.COMPLETE);

                // Apply filters
                if (category.HasValue)
                {
                    allTours = allTours.Where(t => t.Category == category.Value);
                }

                if (difficulty.HasValue)
                {
                    allTours = allTours.Where(t => t.Difficulty == difficulty.Value);
                }

                if (maxPrice.HasValue && maxPrice.Value > 0)
                {
                    allTours = allTours.Where(t => t.Price <= (int)maxPrice.Value);
                }

                var filteredTours = allTours.ToList();
                var totalCount = filteredTours.Count;

                // Apply pagination
                var pagedTours = filteredTours.Skip(page * pageSize).Take(pageSize).ToList();

                // Map to DTOs
                var tourDtos = _mapper.Map<List<TourDto>>(pagedTours);

                // Create PagedResult manually
                var result = new PagedResult<TourDto>(tourDtos, totalCount);
                return Result.Ok(result);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error retrieving filtered tours: {ex.Message}");
            }
        }

        public Result<IEnumerable<CategoryDto>> GetCategories()
        {
            try
            {
                var categories = new List<CategoryDto>
                {
                    new CategoryDto { Id = 1, Name = "Nature" },
                    new CategoryDto { Id = 2, Name = "Art" },
                    new CategoryDto { Id = 3, Name = "Sport" },
                    new CategoryDto { Id = 4, Name = "Shopping" },
                    new CategoryDto { Id = 5, Name = "Food" }
                };

                return Result.Ok(categories.AsEnumerable());
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error retrieving categories: {ex.Message}");
            }
        }
    }
}