using Explorer.BuildingBlocks.Core.Domain;

namespace Explorer.Tours.Core.Domain
{
    public class TourReplacement : Entity
    {
        public long TourId { get; private set; }
        public long OriginalGuideId { get; private set; }
        public long? ReplacementGuideId { get; private set; }
        public TourReplacementStatus Status { get; private set; }
        public DateTime RequestedAt { get; private set; }
        public DateTime? AcceptedAt { get; private set; }
        public DateTime? CancelledAt { get; private set; }

        // EF Core constructor
        private TourReplacement() { }

        public TourReplacement(long tourId, long originalGuideId)
        {
            TourId = tourId;
            OriginalGuideId = originalGuideId;
            Status = TourReplacementStatus.PENDING;
            RequestedAt = DateTime.UtcNow;
            ReplacementGuideId = null;
            AcceptedAt = null;
            CancelledAt = null;
            Validate();
        }

        private void Validate()
        {
            if (TourId <= 0)
                throw new ArgumentException("Tour ID must be positive");

            if (OriginalGuideId <= 0)
                throw new ArgumentException("Original Guide ID must be positive");
        }

        public void AcceptReplacement(long replacementGuideId)
        {
            if (Status != TourReplacementStatus.PENDING)
                throw new InvalidOperationException($"Cannot accept replacement when status is {Status}");

            if (replacementGuideId <= 0)
                throw new ArgumentException("Replacement Guide ID must be positive");

            if (replacementGuideId == OriginalGuideId)
                throw new ArgumentException("Replacement guide cannot be the same as original guide");

            ReplacementGuideId = replacementGuideId;
            Status = TourReplacementStatus.ACCEPTED;
            AcceptedAt = DateTime.UtcNow;
        }

        public void CancelReplacement()
        {
            if (Status != TourReplacementStatus.PENDING)
                throw new InvalidOperationException($"Cannot cancel replacement when status is {Status}");

            Status = TourReplacementStatus.CANCELLED;
            CancelledAt = DateTime.UtcNow;
        }

        public void MarkAsExpired()
        {
            if (Status != TourReplacementStatus.PENDING)
                throw new InvalidOperationException($"Cannot mark as expired when status is {Status}");

            Status = TourReplacementStatus.EXPIRED;
        }

        public bool IsPending()
        {
            return Status == TourReplacementStatus.PENDING;
        }

        public bool IsAccepted()
        {
            return Status == TourReplacementStatus.ACCEPTED;
        }

        public bool HasReplacement()
        {
            return ReplacementGuideId.HasValue && Status == TourReplacementStatus.ACCEPTED;
        }
    }

    public enum TourReplacementStatus
    {
        PENDING,    // Waiting for replacement guide
        ACCEPTED,   // Replacement guide found and accepted
        CANCELLED,  // Original guide cancelled the replacement request
        EXPIRED     // No replacement found 24h before tour date
    }
}