namespace Explorer.Tours.API.Dtos;

public class TourProblemDto
{
    public long Id { get; set; }
    public long TourId { get; set; }
    public long TouristId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ReviewRequestedAt { get; set; }
    public DateTime? RejectedAt { get; set; }

    // Additional info for display
    public string TourName { get; set; }
    public string TouristName { get; set; }
}

public class CreateTourProblemDto
{
    public long TourId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
}
