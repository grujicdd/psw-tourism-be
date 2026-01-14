namespace Explorer.Tours.API.Dtos
{
    public class TourReviewDto
    {
        public long Id { get; set; }
        public long TourPurchaseId { get; set; }
        public long TourId { get; set; }
        public long TouristId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime ReviewDate { get; set; }
    }
}
