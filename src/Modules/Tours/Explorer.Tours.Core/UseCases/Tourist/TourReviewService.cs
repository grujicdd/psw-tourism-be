using AutoMapper;
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Explorer.Tours.Core.Domain;
using FluentResults;

namespace Explorer.Tours.Core.UseCases.Tourist
{
    public class TourReviewService : ITourReviewService
    {
        private readonly ICrudRepository<TourReview> _reviewRepository;
        private readonly ICrudRepository<TourPurchase> _purchaseRepository;
        private readonly ICrudRepository<Tour> _tourRepository;
        private readonly IMapper _mapper;

        public TourReviewService(
            ICrudRepository<TourReview> reviewRepository,
            ICrudRepository<TourPurchase> purchaseRepository,
            ICrudRepository<Tour> tourRepository,
            IMapper mapper)
        {
            _reviewRepository = reviewRepository;
            _purchaseRepository = purchaseRepository;
            _tourRepository = tourRepository;
            _mapper = mapper;
        }

        public Result<TourReviewDto> CreateReview(long touristId, TourReviewCreateDto reviewDto)
        {
            try
            {
                // 1. Verify purchase exists and belongs to tourist
                var purchase = _purchaseRepository.Get(reviewDto.TourPurchaseId);
                if (purchase.TouristId != touristId)
                {
                    return Result.Fail(FailureCode.Forbidden).WithError("This purchase does not belong to you");
                }

                // 2. Verify tour exists in this purchase
                if (!purchase.ContainsTour(reviewDto.TourId))
                {
                    return Result.Fail(FailureCode.InvalidArgument)
                        .WithError("This tour is not part of the specified purchase");
                }

                // 3. Get tour to check date
                var tour = _tourRepository.Get(reviewDto.TourId);

                // 4. Check if already reviewed
                var existingReview = _reviewRepository.GetAll()
                    .FirstOrDefault(r => r.TourPurchaseId == reviewDto.TourPurchaseId
                                      && r.TourId == reviewDto.TourId);

                if (existingReview != null)
                {
                    return Result.Fail(FailureCode.InvalidArgument)
                        .WithError("You have already reviewed this tour");
                }

                // 5. Create review (validation happens in constructor)
                var review = new TourReview(
                    reviewDto.TourPurchaseId,
                    reviewDto.TourId,
                    touristId,
                    reviewDto.Rating,
                    reviewDto.Comment,
                    tour.Date
                );

                var createdReview = _reviewRepository.Create(review);
                return Result.Ok(_mapper.Map<TourReviewDto>(createdReview));
            }
            catch (ArgumentException ex)
            {
                return Result.Fail(FailureCode.InvalidArgument).WithError(ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return Result.Fail(FailureCode.NotFound).WithError("Purchase or tour not found");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error creating review: {ex.Message}");
            }
        }

        public Result<TourReviewDto> GetReview(long reviewId)
        {
            try
            {
                var review = _reviewRepository.Get(reviewId);
                return Result.Ok(_mapper.Map<TourReviewDto>(review));
            }
            catch (KeyNotFoundException)
            {
                return Result.Fail(FailureCode.NotFound).WithError("Review not found");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error retrieving review: {ex.Message}");
            }
        }

        public Result<List<TourReviewDto>> GetReviewsForPurchase(long purchaseId, long touristId)
        {
            try
            {
                // Verify purchase belongs to tourist
                var purchase = _purchaseRepository.Get(purchaseId);
                if (purchase.TouristId != touristId)
                {
                    return Result.Fail(FailureCode.Forbidden)
                        .WithError("This purchase does not belong to you");
                }

                var reviews = _reviewRepository.GetAll()
                    .Where(r => r.TourPurchaseId == purchaseId)
                    .ToList();

                var reviewDtos = reviews.Select(r => _mapper.Map<TourReviewDto>(r)).ToList();
                return Result.Ok(reviewDtos);
            }
            catch (KeyNotFoundException)
            {
                return Result.Fail(FailureCode.NotFound).WithError("Purchase not found");
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error retrieving reviews: {ex.Message}");
            }
        }

        public Result<List<TourReviewDto>> GetReviewsForTour(long tourId)
        {
            try
            {
                var reviews = _reviewRepository.GetAll()
                    .Where(r => r.TourId == tourId)
                    .OrderByDescending(r => r.ReviewDate)
                    .ToList();

                var reviewDtos = reviews.Select(r => _mapper.Map<TourReviewDto>(r)).ToList();
                return Result.Ok(reviewDtos);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error retrieving reviews for tour: {ex.Message}");
            }
        }

        public Result<TourReviewStatisticsDto> GetTourStatistics(long tourId)
        {
            try
            {
                var reviews = _reviewRepository.GetAll()
                    .Where(r => r.TourId == tourId)
                    .ToList();

                if (!reviews.Any())
                {
                    return Result.Ok(new TourReviewStatisticsDto { TourId = tourId });
                }

                var statistics = new TourReviewStatisticsDto
                {
                    TourId = tourId,
                    TotalReviews = reviews.Count,
                    AverageRating = reviews.Average(r => r.Rating),
                    Rating5Count = reviews.Count(r => r.Rating == 5),
                    Rating4Count = reviews.Count(r => r.Rating == 4),
                    Rating3Count = reviews.Count(r => r.Rating == 3),
                    Rating2Count = reviews.Count(r => r.Rating == 2),
                    Rating1Count = reviews.Count(r => r.Rating == 1)
                };

                return Result.Ok(statistics);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error calculating statistics: {ex.Message}");
            }
        }

        public Result<bool> CanReviewTour(long touristId, long purchaseId, long tourId)
        {
            try
            {
                // 1. Verify purchase exists and belongs to tourist
                var purchase = _purchaseRepository.Get(purchaseId);
                if (purchase.TouristId != touristId)
                {
                    return Result.Ok(false);
                }

                // 2. Verify tour is in the purchase
                if (!purchase.ContainsTour(tourId))
                {
                    return Result.Ok(false);
                }

                // 3. Get tour to check date
                var tour = _tourRepository.Get(tourId);

                // 4. Check if tour has happened
                if (tour.Date >= DateTime.UtcNow)
                {
                    return Result.Ok(false);
                }

                // 5. Check if within 7 days
                var daysSinceTour = (DateTime.UtcNow - tour.Date).TotalDays;
                if (daysSinceTour > 7)
                {
                    return Result.Ok(false);
                }

                // 6. Check if already reviewed
                var existingReview = _reviewRepository.GetAll()
                    .FirstOrDefault(r => r.TourPurchaseId == purchaseId && r.TourId == tourId);

                if (existingReview != null)
                {
                    return Result.Ok(false);
                }

                return Result.Ok(true);
            }
            catch (KeyNotFoundException)
            {
                return Result.Ok(false);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error checking review eligibility: {ex.Message}");
            }
        }
    }
}