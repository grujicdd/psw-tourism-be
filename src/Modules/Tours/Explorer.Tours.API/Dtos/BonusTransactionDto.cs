using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer.Tours.API.Dtos
{
    public class BonusTransactionDto
    {
        public long Id { get; set; }
        public long TouristId { get; set; }
        public decimal Amount { get; set; }
        public BonusTransactionType Type { get; set; }
        public string Description { get; set; }
        public long? RelatedTourId { get; set; }
        public long? RelatedPurchaseId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum BonusTransactionType
    {
        EARNED_FROM_CANCELLATION,
        SPENT_ON_PURCHASE,
        EXPIRED
    }
}
