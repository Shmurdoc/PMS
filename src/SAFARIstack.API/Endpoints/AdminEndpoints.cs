using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;
using System.Security.Claims;

namespace SAFARIstack.API.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization();

        // GET /api/admin/db-stats — database statistics
        group.MapGet("/db-stats", async (ApplicationDbContext db) =>
        {
            var stats = new
            {
                Properties = await db.Properties.CountAsync(),
                Guests = await db.Guests.CountAsync(),
                Bookings = await db.Bookings.CountAsync(),
                Rooms = await db.Rooms.CountAsync(),
                RoomTypes = await db.RoomTypes.CountAsync(),
                Staff = await db.StaffMembers.CountAsync(),
                Invoices = await db.Invoices.CountAsync(),
                Payments = await db.Payments.CountAsync(),
                Folios = await db.Folios.CountAsync(),
                HousekeepingTasks = await db.HousekeepingTasks.CountAsync(),
                RatePlans = await db.RatePlans.CountAsync(),
                Seasons = await db.Seasons.CountAsync(),
                ServiceRequests = await db.ServiceRequests.CountAsync(),
                MaintenanceTasks = await db.MaintenanceTasks.CountAsync(),
                GuestFeedbacks = await db.GuestFeedbacks.CountAsync(),
                OvertimeRequests = await db.OvertimeRequests.CountAsync(),
                AuditLogs = await db.AuditLogs.CountAsync(),
                GeneratedAt = DateTime.UtcNow
            };

            return Results.Ok(stats);
        }).WithName("GetDatabaseStats").WithOpenApi();

        // GET /api/admin/system-health — extended health check
        group.MapGet("/system-health", async (ApplicationDbContext db) =>
        {
            var dbConnected = false;
            try
            {
                dbConnected = await db.Database.CanConnectAsync();
            }
            catch { /* swallow */ }

            var pendingMigrations = Enumerable.Empty<string>();
            try
            {
                pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            }
            catch { /* swallow */ }

            return Results.Ok(new
            {
                Status = dbConnected ? "Healthy" : "Unhealthy",
                Database = new
                {
                    Connected = dbConnected,
                    Provider = db.Database.ProviderName,
                    PendingMigrations = pendingMigrations.Count()
                },
                Runtime = new
                {
                    Environment.MachineName,
                    DotNetVersion = Environment.Version.ToString(),
                    OS = Environment.OSVersion.ToString(),
                    ProcessMemoryMB = Math.Round(Environment.WorkingSet / 1024.0 / 1024.0, 1),
                    UptimeHours = Math.Round(Environment.TickCount64 / 1000.0 / 3600.0, 1)
                },
                Timestamp = DateTime.UtcNow
            });
        }).WithName("GetSystemHealth").WithOpenApi();

        // ── Guest CRM: Celebrations ─────────────────────────────

        var guestGroup = app.MapGroup("/api/guests")
            .WithTags("GuestCRM")
            .RequireAuthorization();

        // GET /api/guests/celebrations — upcoming birthdays/anniversaries
        guestGroup.MapGet("/celebrations", async (DateTime? from, DateTime? to, ApplicationDbContext db) =>
        {
            var start = from ?? DateTime.UtcNow;
            var end = to ?? DateTime.UtcNow.AddDays(30);

            // Get guests with DateOfBirth set, check if birthday falls within range
            var guests = await db.Guests
                .Where(g => g.DateOfBirth.HasValue)
                .Select(g => new
                {
                    g.Id,
                    g.FirstName,
                    g.LastName,
                    g.Email,
                    g.Phone,
                    g.DateOfBirth
                })
                .ToListAsync();

            var celebrations = guests
                .Select(g =>
                {
                    var dob = g.DateOfBirth!.Value;
                    var thisYearBirthday = new DateTime(start.Year, dob.Month, Math.Min(dob.Day, DateTime.DaysInMonth(start.Year, dob.Month)));
                    if (thisYearBirthday < start.Date)
                        thisYearBirthday = thisYearBirthday.AddYears(1);

                    return new
                    {
                        g.Id,
                        g.FirstName,
                        g.LastName,
                        g.Email,
                        g.Phone,
                        BirthdayDate = thisYearBirthday,
                        Age = thisYearBirthday.Year - dob.Year,
                        DaysUntil = (thisYearBirthday - DateTime.UtcNow.Date).Days,
                        Type = "Birthday"
                    };
                })
                .Where(c => c.BirthdayDate >= start.Date && c.BirthdayDate <= end.Date)
                .OrderBy(c => c.BirthdayDate)
                .ToList();

            return Results.Ok(new
            {
                Period = new { From = start, To = end },
                Count = celebrations.Count,
                Celebrations = celebrations
            });
        }).WithName("GetGuestCelebrations").WithOpenApi();
    }
}
