using Explorer.BuildingBlocks.Core.Domain;

namespace Explorer.Tours.Core.Domain
{
    public class BonusPoints : Entity
    {
        public long TouristId { get; private set; }
        public decimal AvailablePoints { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public BonusPoints(long touristId, decimal availablePoints = 0)
        {
            TouristId = touristId;
            AvailablePoints = availablePoints;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Validate();
        }

        private void Validate()
        {
            if (TouristId <= 0)
                throw new ArgumentException("Tourist ID must be positive");

            if (AvailablePoints < 0)
                throw new ArgumentException("Available points cannot be negative");
        }

        public void AddPoints(decimal amount, string reason)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount to add must be positive");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason cannot be empty");

            AvailablePoints += amount;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UsePoints(decimal amount, string reason)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount to use must be positive");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason cannot be empty");

            if (amount > AvailablePoints)
                throw new ArgumentException("Cannot use more points than available");

            AvailablePoints -= amount;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool HasSufficientPoints(decimal amount)
        {
            return AvailablePoints >= amount;
        }
    }
}