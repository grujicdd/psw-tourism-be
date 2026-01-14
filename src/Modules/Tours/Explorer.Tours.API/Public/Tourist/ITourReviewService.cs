using Explorer.Tours.API.Dtos;
using FluentResults;

namespace Explorer.Tours.API.Public.Tourist
{
    public interface ITourReviewService
    {
        Result<TourReviewDto> CreateReview(long touristId, TourReviewCreateDto reviewDto);
        Result<TourReviewDto> GetReview(long reviewId);
        Result<List<TourReviewDto>> GetReviewsForPurchase(long purchaseId, long touristId);
        Result<List<TourReviewDto>> GetReviewsForTour(long tourId);
        Result<TourReviewStatisticsDto> GetTourStatistics(long tourId);
        Result<bool> CanReviewTour(long touristId, long purchaseId, long tourId);
    }
}
