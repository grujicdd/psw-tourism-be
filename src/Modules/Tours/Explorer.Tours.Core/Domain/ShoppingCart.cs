using Explorer.BuildingBlocks.Core.Domain;

namespace Explorer.Tours.Core.Domain
{
    public class ShoppingCart : Entity
    {
        public long TouristId { get; private set; }
        public List<long> TourIds { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public ShoppingCart(long touristId)
        {
            TouristId = touristId;
            TourIds = new List<long>();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Validate();
        }

        private void Validate()
        {

        }

        public void AddTour(long tourId)
        {
            //if (tourId <= 0)
            //    throw new ArgumentException("Tour ID must be positive");

            if (!TourIds.Contains(tourId))
            {
                TourIds.Add(tourId);
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void RemoveTour(long tourId)
        {
            if (TourIds.Remove(tourId))
            {
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void ClearCart()
        {
            if (TourIds.Count > 0)
            {
                TourIds.Clear();
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public bool IsEmpty()
        {
            return TourIds.Count == 0;
        }

        public bool ContainsTour(long tourId)
        {
            return TourIds.Contains(tourId);
        }
    }
}