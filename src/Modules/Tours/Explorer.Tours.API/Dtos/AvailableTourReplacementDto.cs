namespace Explorer.Tours.API.Dtos
{
    // DTO that combines TourReplacement with Tour information for display
    public class AvailableTourReplacementDto
    {
        public long ReplacementId { get; set; }
        public long TourId { get; set; }
        public string TourName { get; set; }
        public string TourDescription { get; set; }
        public DateTime TourDate { get; set; }
        public int TourDifficulty { get; set; }
        public int TourCategory { get; set; }
        public int TourPrice { get; set; }
        public long OriginalGuideId { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}