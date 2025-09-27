using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer.Tours.API.Dtos
{
    public class ShoppingCartDto
    {
        public long Id { get; set; }
        public long TouristId { get; set; }
        public List<long> TourIds { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ShoppingCartDto()
        {
            TourIds = new List<long>();
        }
    }
}
