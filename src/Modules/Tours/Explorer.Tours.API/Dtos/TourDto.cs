using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer.Tours.API.Dtos
{
    public class TourDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Difficulty { get; set; }
        public int Category { get; set; }
        public int Price { get; set; }
        public DateTime Date { get; set; }
        public int State { get; set; }
        public long AuthorId { get; set; }
    }
}
