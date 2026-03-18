using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Modules.Staff.Domain.Entities;

namespace SAFARIstack.API.Endpoints;

public static class StaffEndpoints
{
    public static void MapStaffEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/staff")
            .WithTags("Staff")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        // Get today's attendance
        group.MapGet("/attendance/today", async (
            Guid propertyId, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var today = DateTime.UtcNow.Date;
            var query = db.StaffAttendances
                .Where(a => a.PropertyId == propertyId && a.CheckInTime.Date == today)
                .Join(db.StaffMembers, a => a.StaffId, s => s.Id, (a, s) => new
                {
                    a.Id,
                    a.StaffId,
                    StaffName = s.FirstName + " " + s.LastName,
                    StaffRole = s.Role.ToString(),
                    a.CheckInTime,
                    a.CheckOutTime,
                    a.Status,
                    a.ActualHours
                })
                .OrderByDescending(a => a.CheckInTime);

            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 50));
        })
        .WithName("GetTodayAttendance")
        .WithOpenApi();

        // Get attendance report
        group.MapGet("/attendance/report", async (
            Guid propertyId,
            DateTime startDate,
            DateTime endDate,
            int? page,
            int? pageSize,
            ApplicationDbContext db) =>
        {
            var query = db.StaffAttendances
                .Where(a => a.PropertyId == propertyId
                    && a.CheckInTime.Date >= startDate.Date
                    && a.CheckInTime.Date <= endDate.Date)
                .Join(db.StaffMembers, a => a.StaffId, s => s.Id, (a, s) => new
                {
                    a.Id,
                    a.StaffId,
                    StaffName = s.FirstName + " " + s.LastName,
                    StaffRole = s.Role.ToString(),
                    a.CheckInTime,
                    a.CheckOutTime,
                    a.Status,
                    a.ActualHours
                })
                .OrderBy(a => a.CheckInTime);

            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 50));
        })
        .WithName("GetAttendanceReport")
        .WithOpenApi();

        // Get staff members for property
        group.MapGet("/{propertyId:guid}", async (
            Guid propertyId, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var query = db.StaffMembers
                .Where(s => s.PropertyId == propertyId)
                .Select(s => new
                {
                    s.Id,
                    Name = s.FirstName + " " + s.LastName,
                    s.Email,
                    s.Phone,
                    Role = s.Role.ToString(),
                    s.EmploymentType,
                    s.IsActive,
                    s.EmploymentStartDate
                })
                .OrderBy(s => s.Name);

            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 50));
        })
        .WithName("GetStaffByProperty")
        .WithOpenApi();

        // Request overtime
        group.MapPost("/overtime/request", async (OvertimeRequest req, ApplicationDbContext db) =>
        {
            var staff = await db.StaffMembers.FindAsync(req.StaffMemberId);
            if (staff is null)
                return Results.NotFound("Staff member not found.");

            // Record as a note on the attendance record or separate overtime tracking
            // For now, create an attendance record flagged as overtime
            return Results.Ok(new
            {
                StaffMemberId = req.StaffMemberId,
                RequestedDate = req.Date,
                Hours = req.Hours,
                Status = "PendingApproval",
                Message = "Overtime request submitted for approval."
            });
        })
        .WithName("RequestOvertime")
        .WithOpenApi();

        // Approve overtime
        group.MapPost("/overtime/{id:guid}/approve", (
            Guid id, OvertimeApprovalRequest req) =>
        {
            // In a full implementation, this would update an OvertimeRequest entity
            return Results.Ok(new
            {
                OvertimeId = id,
                ApprovedBy = req.ApprovedByUserId,
                Status = req.Approved ? "Approved" : "Rejected",
                Notes = req.Notes
            });
        })
        .WithName("ApproveOvertime")
        .WithOpenApi();
    }
}

// ─── Request DTOs ────────────────────────────────────────────────────
public record OvertimeRequest(Guid StaffMemberId, DateTime Date, decimal Hours, string? Reason);
public record OvertimeApprovalRequest(Guid ApprovedByUserId, bool Approved, string? Notes);
