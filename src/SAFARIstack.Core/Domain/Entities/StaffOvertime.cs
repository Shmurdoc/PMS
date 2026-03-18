using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

/// <summary>
/// Overtime request and tracking for staff members.
/// BCEA compliance: tracks overtime beyond normal 45-hour work week.
/// </summary>
public class OvertimeRequest : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid StaffMemberId { get; private set; }
    public DateTime RequestedDate { get; private set; }
    public decimal RequestedHours { get; private set; } // Overtime hours requested
    public string Reason { get; private set; } = string.Empty;
    public OvertimeRequestStatus Status { get; private set; } // PendingApproval, Approved, Rejected
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public string? ReviewNotes { get; private set; }
    public decimal? ActualHoursWorked { get; private set; } // May be reduced by approver
    public Guid? ModifiedByUserId { get; private set; }

    // Protected constructor for EF Core
    protected OvertimeRequest() { }

    /// <summary>
    /// Create new overtime request.
    /// </summary>
    public static OvertimeRequest Create(
        Guid propertyId,
        Guid staffMemberId,
        DateTime requestedDate,
        decimal requestedHours,
        string reason = "")
    {
        return new OvertimeRequest
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            StaffMemberId = staffMemberId,
            RequestedDate = requestedDate,
            RequestedHours = requestedHours,
            Reason = reason,
            Status = OvertimeRequestStatus.PendingApproval,
            CreatedAt = DateTime.UtcNow,
            RowVersion = 0
        };
    }

    /// <summary>
    /// Approve overtime request.
    /// </summary>
    public void Approve(Guid approvedByUserId, decimal? actualHours = null, string? notes = null)
    {
        if (Status != OvertimeRequestStatus.PendingApproval)
            throw new InvalidOperationException("Only pending requests can be approved.");

        Status = OvertimeRequestStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        ApprovedByUserId = approvedByUserId;
        ActualHoursWorked = actualHours ?? RequestedHours;
        ReviewNotes = notes;
        UpdatedAt = DateTime.UtcNow;
        ModifiedByUserId = approvedByUserId;
    }

    /// <summary>
    /// Reject overtime request.
    /// </summary>
    public void Reject(Guid rejectedByUserId, string? notes = null)
    {
        if (Status != OvertimeRequestStatus.PendingApproval)
            throw new InvalidOperationException("Only pending requests can be rejected.");

        Status = OvertimeRequestStatus.Rejected;
        ReviewedAt = DateTime.UtcNow;
        ApprovedByUserId = rejectedByUserId;
        ReviewNotes = notes;
        ActualHoursWorked = 0;
        UpdatedAt = DateTime.UtcNow;
        ModifiedByUserId = rejectedByUserId;
    }
}

/// <summary>
/// Status of overtime request.
/// </summary>
public enum OvertimeRequestStatus
{
    PendingApproval = 0,
    Approved = 1,
    Rejected = 2,
    Completed = 3,
    Cancelled = 4
}
