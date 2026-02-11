using Explorer.API.Controllers;
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Stakeholders.Core.Domain;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explorer.Tours.API.Controllers.Guide
{
    [Authorize(Policy = "authorPolicy")]
    [Route("api/guide/tour-replacement")]
    public class TourReplacementController : BaseApiController
    {
        private readonly ITourReplacementService _tourReplacementService;

        public TourReplacementController(ITourReplacementService tourReplacementService)
        {
            _tourReplacementService = tourReplacementService;
        }

        [HttpPost("request")]
        public ActionResult<TourReplacementDto> RequestReplacement([FromBody] TourReplacementCreateDto request)
        {
            var guideId = long.Parse(User.Claims.First(c => c.Type == "id").Value);
            var result = _tourReplacementService.RequestReplacement(guideId, request.TourId);
            return CreateResponse(result);
        }

        [HttpDelete("{replacementId}/cancel")]
        public ActionResult CancelReplacementRequest(long replacementId)
        {
            var guideId = long.Parse(User.Claims.First(c => c.Type == "id").Value);
            var result = _tourReplacementService.CancelReplacementRequest(guideId, replacementId);
            return CreateResponse(result);
        }

        [HttpGet("available")]
        public ActionResult<PagedResult<AvailableTourReplacementDto>> GetAvailableReplacements(
            [FromQuery] int page = 0,
            [FromQuery] int pageSize = 10)
        {
            var guideId = long.Parse(User.Claims.First(c => c.Type == "id").Value);
            var result = _tourReplacementService.GetAvailableReplacements(guideId, page, pageSize);
            return CreateResponse(result);
        }

        [HttpPost("{replacementId}/accept")]
        public ActionResult<TourReplacementDto> AcceptReplacement(long replacementId)
        {
            var guideId = long.Parse(User.Claims.First(c => c.Type == "id").Value);
            var result = _tourReplacementService.AcceptReplacement(guideId, replacementId);
            return CreateResponse(result);
        }

        [HttpGet("my-requests")]
        public ActionResult<PagedResult<TourReplacementDto>> GetMyReplacementRequests(
            [FromQuery] int page = 0,
            [FromQuery] int pageSize = 10)
        {
            var guideId = long.Parse(User.Claims.First(c => c.Type == "id").Value);
            var result = _tourReplacementService.GetMyReplacementRequests(guideId, page, pageSize);
            return CreateResponse(result);
        }

        [HttpGet("{replacementId}")]
        public ActionResult<AvailableTourReplacementDto> GetReplacementDetails(long replacementId)
        {
            var result = _tourReplacementService.GetReplacementWithTourDetails(replacementId);
            return CreateResponse(result);
        }
    }
}