// src/Modules/Tours/Explorer.Tours.Core/Domain/KeyPoint.cs
using Explorer.BuildingBlocks.Core.Domain;

namespace Explorer.Tours.Core.Domain
{
    public class KeyPoint : Entity
    {
        public long TourId { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public string? ImageUrl { get; private set; }
        public int Order { get; private set; } // Order of key points in the tour

        public KeyPoint(long tourId, string name, string description, double latitude, double longitude, string? imageUrl = null, int order = 0)
        {
            TourId = tourId;
            Name = name;
            Description = description;
            Latitude = latitude;
            Longitude = longitude;
            ImageUrl = imageUrl;
            Order = order;
            Validate();
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new ArgumentException("Key point name cannot be empty");

            if (string.IsNullOrWhiteSpace(Description))
                throw new ArgumentException("Key point description cannot be empty");

            if (Latitude < -90 || Latitude > 90)
                throw new ArgumentException("Latitude must be between -90 and 90 degrees");

            if (Longitude < -180 || Longitude > 180)
                throw new ArgumentException("Longitude must be between -180 and 180 degrees");

            if (TourId <= 0)
                throw new ArgumentException("Tour ID must be positive");
        }

        public void Update(string name, string description, double latitude, double longitude, string? imageUrl, int order)
        {
            Name = name;
            Description = description;
            Latitude = latitude;
            Longitude = longitude;
            ImageUrl = imageUrl;
            Order = order;
            Validate();
        }
    }
}
