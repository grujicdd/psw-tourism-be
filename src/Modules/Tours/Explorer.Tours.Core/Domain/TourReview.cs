using Explorer.BuildingBlocks.Core.Domain;

namespace Explorer.Tours.Core.Domain
{
    public class TourReview : Entity
    {
        public long TourPurchaseId { get; private set; }
        public long TourId { get; private set; }
        public long TouristId { get; private set; }
        public int Rating { get; private set; }
        public string? Comment { get; private set; }
        public DateTime ReviewDate { get; private set; }

        // Constructor for EF
        private TourReview() { }

        public TourReview(
            long tourPurchaseId,
            long tourId,
            long touristId,
            int rating,
            string? comment,
            DateTime tourDate)
        {
            TourPurchaseId = tourPurchaseId;
            TourId = tourId;
            TouristId = touristId;
            Rating = rating;
            Comment = comment;
            ReviewDate = DateTime.UtcNow;

            Validate(tourDate);
        }

        private void Validate(DateTime tourDate)
        {
            // Rating must be between 1 and 5
            if (Rating < 1 || Rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5");

            // Comment is required for ratings 1 and 2
            if ((Rating == 1 || Rating == 2) && string.IsNullOrWhiteSpace(Comment))
                throw new ArgumentException("Comment is required for ratings 1 and 2");

            // Normalize dates to UTC for comparison (in case tourDate has timezone info)
            var tourDateUtc = tourDate.Kind == DateTimeKind.Utc ? tourDate : tourDate.ToUniversalTime();
            var nowUtc = DateTime.UtcNow;

            // Debug logging
            Console.WriteLine($"DEBUG TourReview Validation:");
            Console.WriteLine($"  Tour Date: {tourDate} (Kind: {tourDate.Kind})");
            Console.WriteLine($"  Tour Date UTC: {tourDateUtc}");
            Console.WriteLine($"  Now UTC: {nowUtc}");
            Console.WriteLine($"  Days since tour: {(nowUtc - tourDateUtc).TotalDays}");

            // Tour must have already happened
            if (tourDateUtc >= nowUtc)
            {
                throw new ArgumentException($"Cannot review a tour that hasn't happened yet. Tour date: {tourDateUtc}, Now: {nowUtc}");
            }

            // Review must be within 7 days after the tour
            var daysSinceTour = (nowUtc - tourDateUtc).TotalDays;
            if (daysSinceTour > 7)
            {
                throw new ArgumentException($"Reviews can only be submitted within 7 days after the tour. Days since tour: {daysSinceTour:F2}");
            }

            if (daysSinceTour < 0)
            {
                throw new ArgumentException($"Cannot review a tour that hasn't happened yet. Days since tour: {daysSinceTour:F2}");
            }

            // Validate IDs
            if (TourPurchaseId <= 0)
                throw new ArgumentException("Tour Purchase ID must be positive");

            if (TourId <= 0)
                throw new ArgumentException("Tour ID must be positive");

            if (TouristId <= 0)
                throw new ArgumentException("Tourist ID must be positive");
        }

        // Method to update review (in case we want to allow editing later)
        public void Update(int rating, string? comment, DateTime tourDate)
        {
            Rating = rating;
            Comment = comment;
            ReviewDate = DateTime.UtcNow;
            Validate(tourDate);
        }
    }
}
