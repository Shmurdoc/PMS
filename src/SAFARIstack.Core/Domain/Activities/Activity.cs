using System;
using System.Collections.Generic;

namespace SAFARIstack.Core.Domain.Activities;

/// <summary>
/// Activity difficulty level
/// </summary>
public enum ActivityDifficultyLevel
{
    Easy,
    Moderate,
    Challenging
}

/// <summary>
/// Activity availability season
/// </summary>
public enum ActivitySeason
{
    YearRound,
    Summer,
    Winter,
    PeakSeason,
    OffSeason
}

/// <summary>
/// Base domain entity representing an activity/experience
/// </summary>
public class Activity
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // safari_game_drive, guided_tour, etc.
    public int DurationMinutes { get; set; }
    public int MaxCapacity { get; set; }
    public decimal BasePrice { get; set; }
    public string CurrencyCode { get; set; } = "ZAR";
    public string? MeetingLocation { get; set; }
    public ActivityDifficultyLevel DifficultyLevel { get; set; }
    public int? MinAgeYears { get; set; }
    public int? MaxAgeYears { get; set; }
    public bool RequiresFitnessLevel { get; set; }
    public ActivitySeason AvailableSeason { get; set; }
    public string? VehicleRequired { get; set; }
    public bool GuideRequired { get; set; }
    public bool IsActive { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Itinerary { get; set; }
    public List<string> Amenities { get; set; } = new();
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public List<ActivitySchedule> Schedules { get; set; } = new();
}

/// <summary>
/// Scheduled instance of an activity
/// </summary>
public class ActivitySchedule
{
    public Guid Id { get; set; }
    public Guid ActivityId { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public TimeOnly ScheduledStartTime { get; set; }
    public TimeOnly ScheduledEndTime { get; set; }
    public int AvailableCapacity { get; set; }
    public int TotalCapacity { get; set; }
    public string Status { get; set; } = "scheduled"; // scheduled, in_progress, completed, cancelled
    public Guid? GuideId { get; set; }
    public Guid? VehicleId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Activity? Activity { get; set; }
    public List<ActivityBooking> Bookings { get; set; } = new();
}

/// <summary>
/// Guest booking for an activity
/// </summary>
public class ActivityBooking
{
    public Guid Id { get; set; }
    public Guid ActivityScheduleId { get; set; }
    public Guid BookingId { get; set; }
    public Guid GuestId { get; set; }
    public int NumberOfGuests { get; set; }
    public List<string> GuestNames { get; set; } = new();
    public string? SpecialRequests { get; set; }
    public string? DietaryRequirements { get; set; }
    public string? FitnessLevel { get; set; }
    public decimal? PaidPrice { get; set; }
    public string PaymentStatus { get; set; } = "unpaid"; // unpaid, partial, paid, refunded
    public List<string> AddOns { get; set; } = new();
    public DateTime? ConfirmationSentAt { get; set; }
    public string Status { get; set; } = "confirmed"; // confirmed, checked_in, no_show, completed, cancelled
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? FeedbackRating { get; set; }
    public string? FeedbackComment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ActivitySchedule? Schedule { get; set; }
}

/// <summary>
/// Activity guide/staff member
/// </summary>
public class ActivityGuide
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public Guid PropertyId { get; set; }
    public string GuideType { get; set; } = string.Empty; // safari_guide, tour_guide, etc.
    public List<string> Specializations { get; set; } = new();
    public List<string> LanguagesSpoken { get; set; } = new();
    public bool IsCertified { get; set; }
    public bool HasVehicle { get; set; }
    public int MaxGuestsPerActivity { get; set; }
    public bool IsAvailable { get; set; }
    public string? AvailabilitySchedule { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
