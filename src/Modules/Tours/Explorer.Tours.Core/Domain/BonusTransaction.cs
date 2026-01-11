using Explorer.BuildingBlocks.Core.Domain;

namespace Explorer.Tours.Core.Domain
{
    public class BonusTransaction : Entity
    {
        public long TouristId { get; private set; }
        public decimal Amount { get; private set; } // Positive for earned, negative for spent
        public BonusTransactionType Type { get; private set; }
        public string Description { get; private set; }
        public long? RelatedTourId { get; private set; } // If related to a specific tour
        public long? RelatedPurchaseId { get; private set; } // If related to a purchase
        public DateTime CreatedAt { get; private set; }

        public BonusTransaction(long touristId, decimal amount, BonusTransactionType type, string description, long? relatedTourId = null, long? relatedPurchaseId = null)
        {
            TouristId = touristId;
            Amount = amount;
            Type = type;
            Description = description;
            RelatedTourId = relatedTourId;
            RelatedPurchaseId = relatedPurchaseId;
            CreatedAt = DateTime.UtcNow;
            Validate();
        }

        private void Validate()
        {
            if (TouristId <= 0)
                throw new ArgumentException("Tourist ID must be positive");

            if (Amount == 0)
                throw new ArgumentException("Amount cannot be zero");

            if (string.IsNullOrWhiteSpace(Description))
                throw new ArgumentException("Description cannot be empty");

            // Validate amount sign matches transaction type
            if (Type == BonusTransactionType.EARNED_FROM_CANCELLATION && Amount <= 0)
                throw new ArgumentException("Earned transactions must have positive amount");

            if (Type == BonusTransactionType.SPENT_ON_PURCHASE && Amount >= 0)
                throw new ArgumentException("Spent transactions must have negative amount");
        }

        public bool IsEarned()
        {
            return Amount > 0;
        }

        public bool IsSpent()
        {
            return Amount < 0;
        }
    }

    public enum BonusTransactionType
    {
        EARNED_FROM_CANCELLATION,  // When guide cancels tour
        SPENT_ON_PURCHASE,         // When tourist uses points
        EXPIRED                    // If we implement expiration later
    }
}
