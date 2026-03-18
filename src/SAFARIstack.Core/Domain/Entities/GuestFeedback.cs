using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

/// <summary>
/// Guest feedback and ratings for property experience.
/// Links to booking or guest for context.
/// </summary>
public class GuestFeedback : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid? BookingId { get; private set; }
    public Guid GuestId { get; private set; }
    public string? GuestName { get; private set; } // Allow anonymous feedback too
    public string? GuestEmail { get; private set; }
    
    // Ratings (1-5 scale)
    public int OverallRating { get; private set; }
    public int? RoomCleanliness { get; private set; }
    public int? RoomComfort { get; private set; }
    public int? FrontDeskService { get; private set; }
    public int? AmenityQuality { get; private set; }
    public int? ValueForMoney { get; private set; }
    
    // Text feedback
    public string? Comment { get; private set; }
    
    // Tags/Categories
    public FeedbackCategory Category { get; private set; } // General, Complaint, Compliment, Suggestion
    public FeedbackSentiment Sentiment { get; private set; } // Positive, Neutral, Negative
    
    // Status tracking
    public FeedbackStatus Status { get; private set; } // New, Reviewed, Responded, Resolved
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public string? ManagerResponse { get; private set; }
    public DateTime? ResponseDate { get; private set; }
    public Guid? RespondedByUserId { get; private set; }
    
    // Tracking
    public DateTime SubmittedAt { get; private set; }
    public bool IsPublished { get; private set; } // Can be shown on website
    public bool RequiresAction { get; private set; } // Flag for management

    // Navigation
    public virtual Booking? Booking { get; private set; }
    public virtual Guest? Guest { get; private set; }

    // Protected constructor for EF Core
    protected GuestFeedback() { }

    /// <summary>
    /// Create new guest feedback.
    /// </summary>
    public static GuestFeedback Create(
        Guid propertyId,
        Guid guestId,
        int overallRating,
        string? comment = null,
        Guid? bookingId = null,
        string? guestName = null,
        string? guestEmail = null)
    {
        if (overallRating < 1 || overallRating > 5)
            throw new ArgumentException("Overall rating must be between 1 and 5.");

        var sentiment = overallRating >= 4 ? FeedbackSentiment.Positive
                      : overallRating == 3 ? FeedbackSentiment.Neutral
                      : FeedbackSentiment.Negative;

        return new GuestFeedback
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            GuestId = guestId,
            BookingId = bookingId,
            GuestName = guestName,
            GuestEmail = guestEmail,
            OverallRating = overallRating,
            Comment = comment,
            Category = FeedbackCategory.General,
            Sentiment = sentiment,
            Status = FeedbackStatus.New,
            SubmittedAt = DateTime.UtcNow,
            IsPublished = false,
            RequiresAction = sentiment == FeedbackSentiment.Negative || 
                            (overallRating <= 2 && !string.IsNullOrEmpty(comment)),
            RowVersion = 0
        };
    }

    /// <summary>
    /// Set satisfaction ratings for specific aspects.
    /// </summary>
    public void SetRatings(
        int? cleanliness = null,
        int? comfort = null,
        int? frontDesk = null,
        int? amenity = null,
        int? valueForMoney = null)
    {
        if (cleanliness.HasValue && (cleanliness < 1 || cleanliness > 5))
            throw new ArgumentException("Rating must be between 1 and 5.");
        if (comfort.HasValue && (comfort < 1 || comfort > 5))
            throw new ArgumentException("Rating must be between 1 and 5.");
        if (frontDesk.HasValue && (frontDesk < 1 || frontDesk > 5))
            throw new ArgumentException("Rating must be between 1 and 5.");
        if (amenity.HasValue && (amenity < 1 || amenity > 5))
            throw new ArgumentException("Rating must be between 1 and 5.");
        if (valueForMoney.HasValue && (valueForMoney < 1 || valueForMoney > 5))
            throw new ArgumentException("Rating must be between 1 and 5.");

        if (cleanliness.HasValue) RoomCleanliness = cleanliness.Value;
        if (comfort.HasValue) RoomComfort = comfort.Value;
        if (frontDesk.HasValue) FrontDeskService = frontDesk.Value;
        if (amenity.HasValue) AmenityQuality = amenity.Value;
        if (valueForMoney.HasValue) ValueForMoney = valueForMoney.Value;
    }

    /// <summary>
    /// Mark feedback as reviewed.
    /// </summary>
    public void MarkReviewed(Guid reviewedByUserId)
    {
        Status = FeedbackStatus.Reviewed;
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = reviewedByUserId;
    }

    /// <summary>
    /// Add manager response to feedback.
    /// </summary>
    public void AddResponse(string response, Guid respondedByUserId)
    {
        if (string.IsNullOrWhiteSpace(response))
            throw new ArgumentException("Response cannot be empty.");

        ManagerResponse = response;
        ResponseDate = DateTime.UtcNow;
        RespondedByUserId = respondedByUserId;
        Status = FeedbackStatus.Responded;
    }

    /// <summary>
    /// Mark as resolved.
    /// </summary>
    public void MarkResolved()
    {
        Status = FeedbackStatus.Resolved;
        RequiresAction = false;
    }

    /// <summary>
    /// Publish feedback (make visible on reviews).
    /// </summary>
    public void Publish()
    {
        IsPublished = true;
    }

    /// <summary>
    /// Unpublish feedback.
    /// </summary>
    public void Unpublish()
    {
        IsPublished = false;
    }
}

/// <summary>
/// Type of feedback.
/// </summary>
public enum FeedbackCategory
{
    General = 0,
    Complaint = 1,
    Compliment = 2,
    Suggestion = 3
}

/// <summary>
/// Sentiment analysis result.
/// </summary>
public enum FeedbackSentiment
{
    Positive = 0,
    Neutral = 1,
    Negative = 2
}

/// <summary>
/// Feedback processing status.
/// </summary>
public enum FeedbackStatus
{
    New = 0,
    Reviewed = 1,
    Responded = 2,
    Resolved = 3,
    Archived = 4
}
