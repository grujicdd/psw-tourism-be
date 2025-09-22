using Explorer.BuildingBlocks.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer.Tours.Core.Domain
{
    public class Tour : Entity
    {
        public string Name {  get; set; }
        public string Description { get; set; }
        public int Difficulty {  get; set; }
        public int Category {  get; set; }
        public int Price {  get; set; }
        public DateTime Date { get; set; }
        public TourState State { get; set; }

        public Tour(string name, string description, int difficulty, int category, int price, DateTime date, TourState state)
        {
            Name = name;
            Description = description;
            Difficulty = difficulty;
            Category = category;
            Price = price;
            Date = date;
            State = state;
        }
    }
}

public enum TourState
{
    DRAFT,
    COMPLETE
}
