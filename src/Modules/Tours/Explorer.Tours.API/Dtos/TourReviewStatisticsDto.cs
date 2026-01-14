namespace Explorer.Tours.API.Dtos
{
    public class TourReviewStatisticsDto
    {
        public long TourId { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int Rating5Count { get; set; }
        public int Rating4Count { get; set; }
        public int Rating3Count { get; set; }
        public int Rating2Count { get; set; }
        public int Rating1Count { get; set; }

        public TourReviewStatisticsDto()
        {
            // Initialize with no reviews
            AverageRating = 0;
            TotalReviews = 0;
        }
    }
}
