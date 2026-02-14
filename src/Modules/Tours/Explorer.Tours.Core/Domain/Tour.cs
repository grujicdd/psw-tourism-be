using Explorer.BuildingBlocks.Core.Domain;

namespace Explorer.Tours.Core.Domain;

public class Tour : Entity
{
    public long AuthorId { get; set; }
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
        Validate();
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
        Validate();
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Tour name is required");

        if (string.IsNullOrWhiteSpace(Description))
            throw new ArgumentException("Tour description is required");

        if (Difficulty < 1 || Difficulty > 5)
            throw new ArgumentException("Difficulty must be between 1 and 5");

        if (Category < 1 || Category > 5)
            throw new ArgumentException("Category must be between 1 and 5");

        if (Price < 0)
            throw new ArgumentException("Price cannot be negative");

        //if (Date < DateTime.UtcNow)
        //    throw new ArgumentException("Tour date cannot be in the past");
    }
}

public enum TourState
{
    DRAFT,
    COMPLETE
}