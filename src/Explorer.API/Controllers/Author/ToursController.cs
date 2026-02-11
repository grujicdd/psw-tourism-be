// src/Explorer.API/Controllers/Author/ToursController.cs
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Administration;
using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explorer.API.Controllers.Author
{
    [Authorize(Policy = "authorPolicy")]
    [Route("api/author/tours")]
    public class ToursController : BaseApiController
    {
        private readonly ITourService _tourService;

        public ToursController(ITourService tourService)
        {
            _tourService = tourService;
        }

        [HttpGet]
        public ActionResult<PagedResult<TourDto>> GetAll([FromQuery] int page, [FromQuery] int pageSize)
        {
            // FIXED: Get logged-in guide's ID from JWT and filter tours
            var authorId = long.Parse(User.Claims.First(c => c.Type == "id").Value);

            var result = _tourService.GetPaged(page, pageSize);

            // Filter tours by author
            if (result.IsSuccess)
            {
                var filteredTours = result.Value.Results.Where(t => t.AuthorId == authorId).ToList();
                var filteredResult = new PagedResult<TourDto>(filteredTours, filteredTours.Count);
                return CreateResponse(Result.Ok(filteredResult));
            }

            return CreateResponse(result);
        }

        // NEW: Add GET by ID endpoint for fetching tour details
        [HttpGet("{id:long}")]
        public ActionResult<TourDto> Get(long id)
        {
            var result = _tourService.Get(id);
            return CreateResponse(result);
        }

        [HttpPost]
        public ActionResult<TourDto> Create([FromBody] TourDto tour)
        {
            // Extract the guide/author ID from JWT claims
            var authorId = long.Parse(User.Claims.First(c => c.Type == "id").Value);

            // Set the AuthorId in the DTO
            tour.AuthorId = authorId;

            var result = _tourService.Create(tour);
            return CreateResponse(result);
        }

        [HttpPut("{id:long}")]
        public ActionResult<TourDto> Update([FromBody] TourDto tour)
        {
            // Extract the guide/author ID from JWT claims
            var authorId = long.Parse(User.Claims.First(c => c.Type == "id").Value);

            // Set the AuthorId to ensure it doesn't get changed
            tour.AuthorId = authorId;

            var result = _tourService.Update(tour);
            return CreateResponse(result);
        }

        [HttpDelete("{id:long}")]
        public ActionResult Delete(long id)
        {
            // Optional: Add authorization check to ensure guide can only delete own tours
            var result = _tourService.Delete(id);
            return CreateResponse(result);
        }
    }
}