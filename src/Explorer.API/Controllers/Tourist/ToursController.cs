// src/Explorer.API/Controllers/Tourist/ToursController.cs
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explorer.API.Controllers.Tourist
{
    [Authorize(Policy = "touristPolicy")]
    [Route("api/tourist/tours")]
    public class ToursController : BaseApiController
    {
        private readonly ITouristTourService _tourService;

        public ToursController(ITouristTourService tourService)
        {
            _tourService = tourService;
        }

        [HttpGet]
        public ActionResult<PagedResult<TourDto>> GetPublishedTours([FromQuery] int page, [FromQuery] int pageSize)
        {
            var result = _tourService.GetPublishedTours(page, pageSize);
            return CreateResponse(result);
        }

        [HttpGet("{id:long}")]
        public ActionResult<TourDto> GetTour(long id)
        {
            var result = _tourService.GetTour(id);
            return CreateResponse(result);
        }

        [HttpGet("filter")]
        public ActionResult<PagedResult<TourDto>> GetFilteredTours(
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] int? category,
            [FromQuery] int? difficulty,
            [FromQuery] decimal? maxPrice)
        {
            var result = _tourService.GetFilteredTours(page, pageSize, category, difficulty, maxPrice);
            return CreateResponse(result);
        }

        [HttpGet("categories")]
        public ActionResult<IEnumerable<CategoryDto>> GetCategories()
        {
            var result = _tourService.GetCategories();
            return CreateResponse(result);
        }
    }
}