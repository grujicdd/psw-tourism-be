using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Explorer.Stakeholders.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explorer.API.Controllers.Tourist
{
    [Authorize(Policy = "touristPolicy")]
    [Route("api/tourist/tour-reviews")]
    public class TourReviewController : BaseApiController
    {
        private readonly ITourReviewService _reviewService;

        public TourReviewController(ITourReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        public ActionResult<TourReviewDto> CreateReview([FromBody] TourReviewCreateDto reviewDto)
        {
            var touristId = User.PersonId();
            var result = _reviewService.CreateReview(touristId, reviewDto);
            return CreateResponse(result);
        }

        [HttpGet("{reviewId:long}")]
        public ActionResult<TourReviewDto> GetReview(long reviewId)
        {
            var result = _reviewService.GetReview(reviewId);
            return CreateResponse(result);
        }

        [HttpGet("purchase/{purchaseId:long}")]
        public ActionResult<List<TourReviewDto>> GetReviewsForPurchase(long purchaseId)
        {
            var touristId = User.PersonId();
            var result = _reviewService.GetReviewsForPurchase(purchaseId, touristId);
            return CreateResponse(result);
        }

        [HttpGet("tour/{tourId:long}")]
        public ActionResult<List<TourReviewDto>> GetReviewsForTour(long tourId)
        {
            var result = _reviewService.GetReviewsForTour(tourId);
            return CreateResponse(result);
        }

        [HttpGet("tour/{tourId:long}/statistics")]
        public ActionResult<TourReviewStatisticsDto> GetTourStatistics(long tourId)
        {
            var result = _reviewService.GetTourStatistics(tourId);
            return CreateResponse(result);
        }

        [HttpGet("can-review")]
        public ActionResult<bool> CanReviewTour([FromQuery] long purchaseId, [FromQuery] long tourId)
        {
            var touristId = User.PersonId();
            var result = _reviewService.CanReviewTour(touristId, purchaseId, tourId);
            return CreateResponse(result);
        }
    }
}
