using Explorer.BuildingBlocks.Core.Domain;

namespace Explorer.Tours.Core.Domain
{
    public class TourPurchase : Entity
    {
        public long TouristId { get; private set; }
        public List<long> TourIds { get; private set; }
        public decimal TotalAmount { get; private set; }
        public decimal BonusPointsUsed { get; private set; }
        public decimal FinalAmount { get; private set; }
        public DateTime PurchaseDate { get; private set; }
        public PurchaseStatus Status { get; private set; }

        public TourPurchase(long touristId, List<long> tourIds, decimal totalAmount, decimal bonusPointsUsed)
        {
            TouristId = touristId;
            TourIds = new List<long>(tourIds); // Create a copy to avoid external modifications
            TotalAmount = totalAmount;
            BonusPointsUsed = bonusPointsUsed;
            FinalAmount = totalAmount - bonusPointsUsed;
            PurchaseDate = DateTime.UtcNow;
            Status = PurchaseStatus.Completed;
            Validate();
        }

        private void Validate()
        {
            if (TouristId <= 0)
                throw new ArgumentException("Tourist ID must be positive");

            if (TourIds == null || TourIds.Count == 0)
                throw new ArgumentException("Tour IDs cannot be empty");

            if (TourIds.Any(id => id <= 0))
                throw new ArgumentException("All Tour IDs must be positive");

            if (TotalAmount < 0)
                throw new ArgumentException("Total amount cannot be negative");

            if (BonusPointsUsed < 0)
                throw new ArgumentException("Bonus points used cannot be negative");

            if (BonusPointsUsed > TotalAmount)
                throw new ArgumentException("Bonus points used cannot exceed total amount");

            if (FinalAmount < 0)
                throw new ArgumentException("Final amount cannot be negative");
        }

        public void ChangeStatus(PurchaseStatus newStatus)
        {
            Status = newStatus;
        }

        public bool ContainsTour(long tourId)
        {
            return TourIds.Contains(tourId);
        }
    }

    public enum PurchaseStatus
    {
        Completed,
        Cancelled,
        Refunded
    }
}