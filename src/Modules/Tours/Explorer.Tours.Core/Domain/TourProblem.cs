using Explorer.BuildingBlocks.Core.Domain;

namespace Explorer.Tours.Core.Domain;

public class TourProblem : Entity
{
    public long TourId { get; private set; }
    public long TouristId { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public TourProblemStatus Status { get; private set; }
    public DateTime ReportedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ReviewRequestedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }

    public TourProblem(long tourId, long touristId, string title, string description)
    {
        TourId = tourId;
        TouristId = touristId;
        Title = title;
        Description = description;
        Status = TourProblemStatus.Pending;
        ReportedAt = DateTime.UtcNow;
        Validate();
    }

    private void Validate()
    {
        if (TourId <= 0)
            throw new ArgumentException("Tour ID must be positive");

        if (TouristId <= 0)
            throw new ArgumentException("Tourist ID must be positive");

        if (string.IsNullOrWhiteSpace(Title))
            throw new ArgumentException("Title cannot be empty");

        if (string.IsNullOrWhiteSpace(Description))
            throw new ArgumentException("Description cannot be empty");
    }

    // Guide marks problem as resolved
    public void MarkAsResolved()
    {
        if (Status != TourProblemStatus.Pending)
            throw new InvalidOperationException($"Cannot resolve problem with status {Status}");

        Status = TourProblemStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
    }

    // Guide sends problem to administrator for review
    public void SendToAdministrator()
    {
        if (Status != TourProblemStatus.Pending)
            throw new InvalidOperationException($"Cannot send to administrator problem with status {Status}");

        Status = TourProblemStatus.UnderReview;
        ReviewRequestedAt = DateTime.UtcNow;
    }

    // Administrator returns problem to guide
    public void ReturnToGuide()
    {
        if (Status != TourProblemStatus.UnderReview)
            throw new InvalidOperationException($"Cannot return to guide problem with status {Status}");

        Status = TourProblemStatus.Pending;
    }

    // Administrator rejects problem
    public void Reject()
    {
        if (Status != TourProblemStatus.UnderReview)
            throw new InvalidOperationException($"Cannot reject problem with status {Status}");

        Status = TourProblemStatus.Rejected;
        RejectedAt = DateTime.UtcNow;
    }

    public bool IsPending()
    {
        return Status == TourProblemStatus.Pending;
    }

    public bool IsResolved()
    {
        return Status == TourProblemStatus.Resolved;
    }

    public bool IsUnderReview()
    {
        return Status == TourProblemStatus.UnderReview;
    }

    public bool IsRejected()
    {
        return Status == TourProblemStatus.Rejected;
    }
}

public enum TourProblemStatus
{
    Pending = 0,        // Na čekanju
    Resolved = 1,       // Rešen
    UnderReview = 2,    // Na reviziji
    Rejected = 3        // Odbačen
}