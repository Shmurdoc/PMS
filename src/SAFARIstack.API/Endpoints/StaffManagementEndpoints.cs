using Microsoft.EntityFrameworkCore;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Modules.Staff.Domain.Entities;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// Extended Staff endpoints — full CRUD, scheduling, leave management
/// </summary>
public static class StaffManagementEndpoints
{
    public static void MapStaffManagementEndpoints(this WebApplication app)
    {
        // ═══════════════════════════════════════════════════════════════
        //  STAFF CRUD
        // ═══════════════════════════════════════════════════════════════
        var staff = app.MapGroup("/api/staff")
            .WithTags("Staff Management")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        // Get single staff member by ID
        staff.MapGet("/detail/{staffId:guid}", async (Guid staffId, ApplicationDbContext db) =>
        {
            var member = await db.StaffMembers
                .Where(s => s.Id == staffId)
                .Select(s => new
                {
                    s.Id,
                    s.FirstName,
                    s.LastName,
                    Name = s.FirstName + " " + s.LastName,
                    s.Email,
                    s.Phone,
                    s.IdNumber,
                    Role = s.Role.ToString(),
                    EmploymentType = s.EmploymentType.ToString(),
                    s.HourlyRate,
                    s.IsActive,
                    s.EmploymentStartDate,
                    s.LastLoginAt,
                    RfidCardCount = db.RfidCards.Count(c => c.StaffId == s.Id && c.Status == RfidCardStatus.Active)
                })
                .FirstOrDefaultAsync();

            return member is null ? Results.NotFound() : Results.Ok(member);
        })
        .WithName("GetStaffMemberById")
        .WithOpenApi();

        // Create staff member
        staff.MapPost("/", async (CreateStaffMemberRequest request, ApplicationDbContext db) =>
        {
            var existing = await db.StaffMembers
                .AnyAsync(s => s.PropertyId == request.PropertyId && s.Email == request.Email);
            if (existing) return Results.Conflict("Staff member with this email already exists.");

            var role = Enum.Parse<StaffRole>(request.Role, true);
            var member = StaffMember.Create(request.PropertyId, request.Email, request.FirstName, request.LastName, role);

            db.StaffMembers.Add(member);
            await db.SaveChangesAsync();

            return Results.Created($"/api/staff/detail/{member.Id}", new
            {
                member.Id,
                Name = member.FullName,
                member.Email,
                Role = member.Role.ToString(),
                member.IsActive
            });
        })
        .WithName("CreateStaffMember")
        .WithOpenApi();

        // Update staff member
        staff.MapPut("/detail/{staffId:guid}", async (
            Guid staffId, UpdateStaffMemberRequest request, ApplicationDbContext db) =>
        {
            var member = await db.StaffMembers.FindAsync(staffId);
            if (member is null) return Results.NotFound();

            // Use EF entry to update allowed fields
            var entry = db.Entry(member);
            if (request.Phone is not null) entry.Property("Phone").CurrentValue = request.Phone;
            if (request.IsActive.HasValue) entry.Property("IsActive").CurrentValue = request.IsActive.Value;

            await db.SaveChangesAsync();
            return Results.Ok(new { member.Id, Name = member.FullName, Message = "Staff member updated." });
        })
        .WithName("UpdateStaffMember")
        .WithOpenApi();

        // Deactivate (soft delete) staff member
        staff.MapPost("/detail/{staffId:guid}/deactivate", async (Guid staffId, ApplicationDbContext db) =>
        {
            var member = await db.StaffMembers.FindAsync(staffId);
            if (member is null) return Results.NotFound();

            db.Entry(member).Property("IsActive").CurrentValue = false;
            await db.SaveChangesAsync();

            return Results.Ok(new { member.Id, IsActive = false, Message = "Staff member deactivated." });
        })
        .WithName("DeactivateStaffMember")
        .WithOpenApi();

        // Reactivate staff member
        staff.MapPost("/detail/{staffId:guid}/reactivate", async (Guid staffId, ApplicationDbContext db) =>
        {
            var member = await db.StaffMembers.FindAsync(staffId);
            if (member is null) return Results.NotFound();

            db.Entry(member).Property("IsActive").CurrentValue = true;
            await db.SaveChangesAsync();

            return Results.Ok(new { member.Id, IsActive = true, Message = "Staff member reactivated." });
        })
        .WithName("ReactivateStaffMember")
        .WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  STAFF SCHEDULING
        // ═══════════════════════════════════════════════════════════════
        var schedule = app.MapGroup("/api/staff/schedules")
            .WithTags("Staff Scheduling")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        // Get schedules for a property (date range)
        schedule.MapGet("/{propertyId:guid}", async (
            Guid propertyId,
            DateTime startDate,
            DateTime endDate,
            int? page,
            int? pageSize,
            ApplicationDbContext db) =>
        {
            // Build query based on attendance data for scheduled shifts
            // Since StaffSchedule entity doesn't exist yet, return attendance-based schedule view
            var query = db.StaffAttendances
                .Where(a => a.PropertyId == propertyId
                    && a.CheckInTime.Date >= startDate.Date
                    && a.CheckInTime.Date <= endDate.Date)
                .Join(db.StaffMembers, a => a.StaffId, s => s.Id, (a, s) => new
                {
                    a.Id,
                    a.StaffId,
                    StaffName = s.FirstName + " " + s.LastName,
                    Department = s.Role.ToString(),
                    Date = a.CheckInTime.Date,
                    ShiftType = a.ShiftType.ToString(),
                    ScheduledStart = a.CheckInTime,
                    ScheduledEnd = a.CheckOutTime,
                    a.ScheduledHours,
                    a.ActualHours,
                    Status = a.Status.ToString()
                })
                .OrderBy(a => a.Date).ThenBy(a => a.StaffName);

            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 100));
        })
        .WithName("GetStaffSchedules")
        .WithOpenApi();

        // Get attendance report with SA labor compliance summary
        schedule.MapGet("/compliance/{propertyId:guid}", async (
            Guid propertyId,
            DateTime startDate,
            DateTime endDate,
            ApplicationDbContext db) =>
        {
            var records = await db.StaffAttendances
                .Where(a => a.PropertyId == propertyId
                    && a.CheckInTime.Date >= startDate.Date
                    && a.CheckInTime.Date <= endDate.Date
                    && a.CheckOutTime.HasValue)
                .Join(db.StaffMembers, a => a.StaffId, s => s.Id, (a, s) => new
                {
                    a.StaffId,
                    StaffName = s.FirstName + " " + s.LastName,
                    a.ActualHours,
                    a.OvertimeHours,
                    a.ScheduledHours,
                    a.TotalWage,
                    ShiftType = a.ShiftType.ToString(),
                    CheckIn = a.CheckInTime
                })
                .ToListAsync();

            var summary = records
                .GroupBy(r => new { r.StaffId, r.StaffName })
                .Select(g => new
                {
                    g.Key.StaffId,
                    g.Key.StaffName,
                    TotalShifts = g.Count(),
                    TotalHours = g.Sum(x => x.ActualHours ?? 0),
                    TotalScheduledHours = g.Sum(x => x.ScheduledHours),
                    TotalOvertimeHours = g.Sum(x => x.OvertimeHours),
                    TotalWages = g.Sum(x => x.TotalWage),
                    WeeklyHours = g.Sum(x => x.ActualHours ?? 0),
                    ExceedsWeeklyLimit = g.Sum(x => x.ActualHours ?? 0) > 45, // BCEA: 45hr/week max
                    SundayShifts = g.Count(x => x.CheckIn.DayOfWeek == DayOfWeek.Sunday)
                })
                .OrderBy(s => s.StaffName)
                .ToList();

            return Results.Ok(new
            {
                PropertyId = propertyId,
                Period = new { StartDate = startDate, EndDate = endDate },
                TotalStaff = summary.Count,
                TotalOvertimeHours = summary.Sum(s => s.TotalOvertimeHours),
                TotalWageBill = summary.Sum(s => s.TotalWages),
                ComplianceAlerts = summary.Count(s => s.ExceedsWeeklyLimit),
                StaffSummary = summary
            });
        })
        .WithName("GetLaborComplianceSummary")
        .WithOpenApi();
    }
}

// ─── Request DTOs ────────────────────────────────────────────────
public record CreateStaffMemberRequest(
    Guid PropertyId, string Email, string FirstName, string LastName,
    string Role, string? Phone, string? IdNumber, string? EmploymentType);
public record UpdateStaffMemberRequest(string? Phone, bool? IsActive);
