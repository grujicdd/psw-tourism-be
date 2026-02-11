namespace Explorer.Tours.API.Dtos
{
    public class TourReplacementDto
    {
        public long Id { get; set; }
        public long TourId { get; set; }
        public long OriginalGuideId { get; set; }
        public long? ReplacementGuideId { get; set; }
        public TourReplacementStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
    }

    public class TourReplacementCreateDto
    {
        public long TourId { get; set; }
    }

    public class TourReplacementAcceptDto
    {
        public long ReplacementId { get; set; }
    }

    public enum TourReplacementStatus
    {
        PENDING = 0,
        ACCEPTED = 1,
        CANCELLED = 2,
        EXPIRED = 3
    }
}