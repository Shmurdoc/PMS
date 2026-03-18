using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════
// EXPERIENCE & ACTIVITY BOOKING ENGINE — Safari/Activity Marketplace
// ═══════════════════════════════════════════════════════════════════════

public class Experience : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ExperienceCategory Category { get; private set; }
    public int DurationMinutes { get; private set; }
    public int MinGuests { get; private set; } = 1;
    public int MaxGuests { get; private set; }
    public int? MinAge { get; private set; }
    public decimal BasePrice { get; private set; }
    public bool PricePerPerson { get; private set; } = true;
    public string? Location { get; private set; }
    public DifficultyLevel DifficultyLevel { get; private set; } = DifficultyLevel.Easy;
    public string? IncludedItems { get; private set; } // Comma-separated
    public string? ExcludedItems { get; private set; } // Comma-separated
    public string? WhatToBring { get; private set; } // Comma-separated
    public int CancellationHours { get; private set; } = 24;
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsThirdParty { get; private set; }
    public string? ThirdPartyOperator { get; private set; }
    public decimal? CommissionRate { get; private set; } // e.g. 0.15 for 15%

    // Navigation
    private readonly List<ExperienceSchedule> _schedules = new();
    public IReadOnlyCollection<ExperienceSchedule> Schedules => _schedules.AsReadOnly();

    private Experience() { }

    public static Experience Create(
        Guid propertyId, string name, ExperienceCategory category,
        int durationMinutes, int maxGuests, decimal basePrice,
        string? description = null, bool pricePerPerson = true,
        string? location = null, DifficultyLevel difficultyLevel = DifficultyLevel.Easy,
        string? imageUrl = null)
    {
        if (durationMinutes <= 0) throw new ArgumentException("Duration must be positive.");
        if (maxGuests <= 0) throw new ArgumentException("Max guests must be positive.");
        if (basePrice < 0) throw new ArgumentException("Base price cannot be negative.");

        return new Experience
        {
            PropertyId = propertyId,
            Name = name.Trim(),
            Category = category,
            DurationMinutes = durationMinutes,
            MaxGuests = maxGuests,
            BasePrice = basePrice,
            Description = description?.Trim(),
            PricePerPerson = pricePerPerson,
            Location = location?.Trim(),
            DifficultyLevel = difficultyLevel,
            ImageUrl = imageUrl
        };
    }

    public ExperienceSchedule AddSchedule(TimeOnly startTime, TimeOnly endTime, int[] daysOfWeek,
        int maxCapacity, Guid? guideStaffId = null, string? vehicleId = null)
    {
        var schedule = ExperienceSchedule.Create(Id, startTime, endTime, daysOfWeek, maxCapacity, guideStaffId, vehicleId);
        _schedules.Add(schedule);
        return schedule;
    }

    public void SetThirdParty(string operatorName, decimal commissionRate)
    {
        IsThirdParty = true;
        ThirdPartyOperator = operatorName;
        CommissionRate = commissionRate;
    }

    public void SetIncludedExcluded(string? included, string? excluded, string? whatToBring)
    {
        IncludedItems = included;
        ExcludedItems = excluded;
        WhatToBring = whatToBring;
    }

    public void UpdatePricing(decimal basePrice, bool pricePerPerson)
    {
        BasePrice = basePrice;
        PricePerPerson = pricePerPerson;
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}

public enum ExperienceCategory
{
    Safari,      // Game drives, bush walks
    Water,       // River rafting, kayaking
    Cultural,    // Village visits, traditional cooking
    Adventure,   // Quad biking, zip lining, helicopter
    Wellness,    // Spa, yoga, meditation
    Dining,      // Wine tasting, bush dinner, braai
    Nature,      // Bird watching, stargazing, botanical
    Historical,  // Museum tours, heritage sites
    Kids,        // Children's programs
    Other
}

public enum DifficultyLevel { Easy, Moderate, Strenuous }

public class ExperienceSchedule : Entity
{
    public Guid ExperienceId { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int[] DaysOfWeek { get; private set; } = Array.Empty<int>(); // 0=Sun, 1=Mon, etc.
    public int MaxCapacity { get; private set; }
    public Guid? GuideStaffId { get; private set; }
    public string? VehicleId { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation
    public Experience Experience { get; private set; } = null!;

    private ExperienceSchedule() { }

    public static ExperienceSchedule Create(Guid experienceId, TimeOnly startTime, TimeOnly endTime,
        int[] daysOfWeek, int maxCapacity, Guid? guideStaffId = null, string? vehicleId = null)
    {
        return new ExperienceSchedule
        {
            ExperienceId = experienceId,
            StartTime = startTime,
            EndTime = endTime,
            DaysOfWeek = daysOfWeek,
            MaxCapacity = maxCapacity,
            GuideStaffId = guideStaffId,
            VehicleId = vehicleId
        };
    }

    public void UpdateCapacity(int maxCapacity) => MaxCapacity = maxCapacity;
    public void AssignGuide(Guid staffId) => GuideStaffId = staffId;
    public void Deactivate() => IsActive = false;
}

public class ExperienceBooking : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid? BookingId { get; private set; } // Hotel booking reference
    public Guid GuestId { get; private set; }
    public Guid ExperienceId { get; private set; }
    public Guid? ScheduleId { get; private set; }
    public DateTime ScheduledDate { get; private set; }
    public TimeOnly ScheduledTime { get; private set; }
    public int ParticipantCount { get; private set; }
    public decimal TotalPrice { get; private set; }
    public decimal? CommissionAmount { get; private set; }
    public decimal? CommissionRate { get; private set; }
    public ExperienceBookingStatus Status { get; private set; } = ExperienceBookingStatus.Confirmed;
    public string? SpecialRequests { get; private set; }
    public Guid? AssignedGuideId { get; private set; }
    public DateTime? CheckInTime { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int? FeedbackScore { get; private set; } // 1-5
    public string? FeedbackNotes { get; private set; }
    public Guid? FolioId { get; private set; }
    public bool AddedToFolio { get; private set; }

    // Navigation
    public Experience Experience { get; private set; } = null!;
    public Guest Guest { get; private set; } = null!;
    public Booking? HotelBooking { get; private set; }

    private ExperienceBooking() { }

    public static ExperienceBooking Create(
        Guid propertyId, Guid guestId, Guid experienceId,
        DateTime scheduledDate, TimeOnly scheduledTime,
        int participantCount, decimal totalPrice,
        Guid? bookingId = null, Guid? scheduleId = null,
        string? specialRequests = null,
        decimal? commissionAmount = null, decimal? commissionRate = null)
    {
        return new ExperienceBooking
        {
            PropertyId = propertyId,
            GuestId = guestId,
            ExperienceId = experienceId,
            ScheduledDate = scheduledDate,
            ScheduledTime = scheduledTime,
            ParticipantCount = participantCount,
            TotalPrice = totalPrice,
            BookingId = bookingId,
            ScheduleId = scheduleId,
            SpecialRequests = specialRequests?.Trim(),
            CommissionAmount = commissionAmount,
            CommissionRate = commissionRate
        };
    }

    public void AssignGuide(Guid guideId) => AssignedGuideId = guideId;
    public void CheckIn() { CheckInTime = DateTime.UtcNow; Status = ExperienceBookingStatus.InProgress; }
    public void Complete()
    {
        CompletedAt = DateTime.UtcNow;
        Status = ExperienceBookingStatus.Completed;
    }

    public void AddFeedback(int score, string? notes)
    {
        if (score < 1 || score > 5) throw new ArgumentException("Score must be 1-5.");
        FeedbackScore = score;
        FeedbackNotes = notes;
    }

    public void Cancel(string reason)
    {
        Status = ExperienceBookingStatus.Cancelled;
        SpecialRequests = reason;
    }

    public void AddToFolio(Guid folioId)
    {
        FolioId = folioId;
        AddedToFolio = true;
    }

    public void MarkNoShow() => Status = ExperienceBookingStatus.NoShow;
}

public enum ExperienceBookingStatus
{
    Confirmed,
    InProgress,
    Completed,
    Cancelled,
    NoShow,
    Rescheduled
}
