using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer.Tours.API.Dtos
{
    public class TourPurchaseDto
    {
        public long Id { get; set; }
        public long TouristId { get; set; }
        public List<long> TourIds { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal BonusPointsUsed { get; set; }
        public decimal FinalAmount { get; set; }
        public DateTime PurchaseDate { get; set; }
        public PurchaseStatus Status { get; set; }

        public TourPurchaseDto()
        {
            TourIds = new List<long>();
        }

        
    }
    public enum PurchaseStatus
    {
        Completed,
        Cancelled,
        Refunded
    }
}
