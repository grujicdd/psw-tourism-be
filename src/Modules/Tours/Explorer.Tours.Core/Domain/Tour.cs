using Explorer.BuildingBlocks.Core.Domain;

namespace Explorer.Tours.Core.Domain;

public class Tour : Entity
{
    public long AuthorId { get; set; }  // Guide/Author who created the tour
    public string Name { get; set; }
    public string Description { get; set; }
    public int Difficulty { get; set; }
    public int Category { get; set; }
    public int Price { get; set; }
    public DateTime Date { get; set; }
    public TourState State { get; set; }

    // Constructor with AuthorId
    public Tour(long authorId, string name, string description, int difficulty, int category, int price, DateTime date, TourState state)
    {
        AuthorId = authorId;
        Name = name;
        Description = description;
        Difficulty = difficulty;
        Category = category;
        Price = price;
        Date = date;
        State = state;
    }

    // Keep old constructor for backward compatibility (can be removed later)
    public Tour(string name, string description, int difficulty, int category, int price, DateTime date, TourState state)
    {
        AuthorId = 0; // Default value for old data
        Name = name;
        Description = description;
        Difficulty = difficulty;
        Category = category;
        Price = price;
        Date = date;
        State = state;
    }
}

public enum TourState
{
    DRAFT,
    COMPLETE
}