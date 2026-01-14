namespace Explorer.Tours.API.Dtos
{
    public class TourReviewCreateDto
    {
        public long TourPurchaseId { get; set; }
        public long TourId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
