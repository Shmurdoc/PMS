using System;
using System.Collections.Generic;

namespace SAFARIstack.Core.Domain.Housekeeping;

/// <summary>
/// Task priority level
/// </summary>
public enum HousekeepingTaskPriority
{
    Low,
    Normal,
    High
}

/// <summary>
/// Housekeeping area entity
/// </summary>
public class HousekeepingArea
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AreaType { get; set; } = string.Empty; // room, common_area, kitchen, laundry, etc.
    public int OrderPriority { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public List<HousekeepingTask> Tasks { get; set; } = new();
}

/// <summary>
/// Housekeeping task type template
/// </summary>
public class HousekeepingTaskType
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
    public string? RequiredSkillLevel { get; set; } // entry, intermediate, expert
    public List<string> ChecklistItems { get; set; } = new();
    public List<string> SuppliesNeeded { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public List<HousekeepingTask> Tasks { get; set; } = new();
}

/// <summary>
/// Housekeeping task assignment
/// </summary>
public class HousekeepingTask
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid AreaId { get; set; }
    public Guid? TaskTypeId { get; set; }
    public Guid? AssignedToStaffId { get; set; }
    public DateOnly TaskDate { get; set; }
    public TimeOnly? ScheduledStartTime { get; set; }
    public TimeOnly? ScheduledEndTime { get; set; }
    public HousekeepingTaskPriority Priority { get; set; }
    public string Status { get; set; } = "pending"; // pending, in_progress, completed, verified, cancelled
    public string Description { get; set; } = string.Empty;
    public string? SpecialInstructions { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool QCRequired { get; set; }
    public Guid? QCCheckedByStaffId { get; set; }
    public string? QCStatus { get; set; } // pending, pass, fail, needs_rework
    public string? QCFeedback { get; set; }
    public DateTime? QCCheckedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public HousekeepingArea? Area { get; set; }
    public HousekeepingTaskType? TaskType { get; set; }
    public HousekeepingStaff? AssignedToStaff { get; set; }
    public HousekeepingStaff? QCCheckedByStaff { get; set; }
    public List<HousekeepingTaskEvidence> Evidence { get; set; } = new();
}

/// <summary>
/// Evidence for task completion (photos, signatures, notes)
/// </summary>
public class HousekeepingTaskEvidence
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string EvidenceType { get; set; } = string.Empty; // before_photo, after_photo, signature, note
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public int? FileSizeBytes { get; set; }
    public string? Notes { get; set; }
    public Guid UploadedByStaffId { get; set; }
    public DateTime UploadedAt { get; set; }

    // Navigation properties
    public HousekeepingTask? Task { get; set; }
}

/// <summary>
/// Housekeeping staff member profile
/// </summary>
public class HousekeepingStaff
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid StaffId { get; set; }
    public string SkillLevel { get; set; } = "entry"; // entry, intermediate, expert
    public bool IsQCInspector { get; set; }
    public int MaxConcurrentTasks { get; set; }
    public bool IsAvailable { get; set; }
    public string? AvailabilitySchedule { get; set; }
    public List<string> LanguagesSpoken { get; set; } = new();
    public List<string> Certifications { get; set; } = new();
    public int TasksCompleted { get; set; }
    public decimal? QCPassRate { get; set; }
    public decimal? AverageRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public List<HousekeepingTask> AssignedTasks { get; set; } = new();
    public List<HousekeepingTask> QCTasks { get; set; } = new();
}

/// <summary>
/// Housekeeping schedule template
/// </summary>
public class HousekeepingSchedule
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = "daily"; // daily, weekly, custom
    public bool IsActive { get; set; }
    public string ScheduleJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Housekeeping incident/issue report
/// </summary>
public class HousekeepingIncident
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? AreaId { get; set; }
    public Guid? TaskId { get; set; }
    public string IncidentType { get; set; } = string.Empty; // damage, theft, safety_hazard, quality_issue, etc.
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "normal"; // critical, high, normal, low
    public Guid? ReportedByStaffId { get; set; }
    public DateTime ReportedAt { get; set; }
    public Guid? ResolvedByStaffId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public string Status { get; set; } = "open"; // open, investigating, resolved, closed
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public HousekeepingArea? Area { get; set; }
    public HousekeepingTask? Task { get; set; }
}
