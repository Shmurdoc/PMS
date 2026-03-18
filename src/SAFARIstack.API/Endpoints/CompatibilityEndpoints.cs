using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Modules.Staff.Domain.Entities;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// Compatibility endpoints â€” maps additional URL patterns that various
/// frontend clients (Blazor WASM, Portal, WPF, MAUI, PWA) expect.
/// Acts as route aliases that delegate to the same logic as existing endpoints.
/// </summary>
public static class CompatibilityEndpoints
{
    public static void MapCompatibilityEndpoints(this WebApplication app)
    {
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  AUTH ALIASES â€” frontends expect /api/auth/profile, /api/auth/refresh-token, etc.
        //  Backend canonical routes are /api/auth/me/, /api/auth/refresh, etc.
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        var authCompat = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .RequireAuthorization();

        // GET /api/auth/profile â†’ alias for GET /api/auth/me/
        authCompat.MapGet("/profile", async (HttpContext context, IAuthService authService) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();
            var user = await authService.GetUserByIdAsync(userId.Value);
            if (user is null) return Results.NotFound();
            return Results.Ok(new
            {
                user.Id, user.Email, user.FirstName, user.LastName, user.FullName,
                user.Phone, user.AvatarUrl, user.PropertyId, user.LastLoginAt,
                Roles = user.UserRoles.Select(ur => ur.Role.Name)
            });
        }).WithName("GetProfileCompat").WithOpenApi().ExcludeFromDescription();

        // POST /api/auth/refresh-token â†’ alias for POST /api/auth/refresh
        app.MapPost("/api/auth/refresh-token", async (RefreshTokenRequest request, IAuthService authService) =>
        {
            var result = await authService.RefreshTokenAsync(request.RefreshToken);
            return result.Success ? Results.Ok(result) : Results.Unauthorized();
        })
        .WithTags("Authentication")
        .AllowAnonymous()
        .WithName("RefreshTokenCompat").WithOpenApi().ExcludeFromDescription();

        // POST /api/auth/change-password â†’ alias for POST /api/auth/me/change-password
        authCompat.MapPost("/change-password", async (
            ChangePasswordRequest request, HttpContext context, IAuthService authService) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();
            var success = await authService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword);
            return success ? Results.NoContent() : Results.BadRequest(new { Error = "Current password is incorrect." });
        }).WithName("ChangePasswordCompat").WithOpenApi().ExcludeFromDescription();

        // PUT /api/auth/profile â†’ alias for PUT /api/auth/me/profile
        authCompat.MapPut("/profile", async (
            UpdateUserProfileRequest request, HttpContext context, IAuthService authService) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();
            var success = await authService.UpdateProfileAsync(
                userId.Value, request.FirstName, request.LastName, request.Phone, request.AvatarUrl);
            return success ? Results.NoContent() : Results.NotFound();
        }).WithName("UpdateProfileCompat").WithOpenApi().ExcludeFromDescription();

        // POST /api/auth/logout â†’ alias for POST /api/auth/me/logout
        authCompat.MapPost("/logout", async (HttpContext context, IAuthService authService) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();
            await authService.RevokeTokenAsync(userId.Value);
            return Results.NoContent();
        }).WithName("LogoutCompat").WithOpenApi().ExcludeFromDescription();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  ROOMS â€” GET /api/rooms/property/{propertyId} (get ALL rooms for a property)
        //  The canonical endpoints only have /available/ and /status/ filtered views.
        //  Frontends expect a general "get all rooms" endpoint.
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        app.MapGet("/api/rooms/property/{propertyId:guid}", async (
            Guid propertyId, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var query = db.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.PropertyId == propertyId)
                .AsNoTracking()
                .OrderBy(r => r.RoomNumber)
                .Select(r => new
                {
                    r.Id, r.RoomNumber, r.Floor, r.Wing,
                    Status = r.Status.ToString(),
                    HkStatus = r.HkStatus.ToString(),
                    r.IsActive, r.Notes,
                    RoomType = r.RoomType == null ? null : new { r.RoomType.Id, r.RoomType.Name, r.RoomType.Code, r.RoomType.BasePrice, r.RoomType.MaxGuests }
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 50));
        })
        .WithTags("Rooms")
        .RequireAuthorization()
        .WithName("GetAllRoomsByProperty").WithOpenApi();

        // GET /api/rooms/{id} â€” get a single room by ID
        app.MapGet("/api/rooms/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var room = await db.Rooms
                .Include(r => r.RoomType)
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new
                {
                    r.Id, r.RoomNumber, r.Floor, r.Wing,
                    Status = r.Status.ToString(),
                    HkStatus = r.HkStatus.ToString(),
                    r.IsActive, r.Notes, r.PropertyId,
                    RoomType = r.RoomType == null ? null : new { r.RoomType.Id, r.RoomType.Name, r.RoomType.Code, r.RoomType.BasePrice, r.RoomType.MaxGuests }
                })
                .FirstOrDefaultAsync();
            return room is null ? Results.NotFound() : Results.Ok(room);
        })
        .WithTags("Rooms")
        .RequireAuthorization()
        .WithName("GetRoomById").WithOpenApi();

        // PUT /api/rooms/{id}/status â€” alias for PATCH /api/rooms/{id}/status
        app.MapPut("/api/rooms/{id:guid}/status", async (Guid id, RoomStatusUpdateRequest req, IUnitOfWork uow) =>
        {
            var room = await uow.Rooms.GetByIdAsync(id);
            if (room is null) return Results.NotFound();
            if (Enum.TryParse<RoomStatus>(req.Status, true, out var status))
            {
                room.UpdateStatus(status);
                uow.Rooms.Update(room);
                await uow.SaveChangesAsync();
            }
            return Results.NoContent();
        })
        .WithTags("Rooms")
        .RequireAuthorization()
        .WithName("UpdateRoomStatusPut").WithOpenApi().ExcludeFromDescription();

        // PUT /api/rooms/{id}/housekeeping-status â€” update HK status
        app.MapPut("/api/rooms/{id:guid}/housekeeping-status", async (Guid id, HkStatusUpdateRequest req, IUnitOfWork uow) =>
        {
            var room = await uow.Rooms.GetByIdAsync(id);
            if (room is null) return Results.NotFound();
            if (req.HkStatus?.Equals("Clean", StringComparison.OrdinalIgnoreCase) == true)
                room.MarkClean(DateTime.UtcNow);
            else
                room.MarkDirty();
            uow.Rooms.Update(room);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithTags("Rooms")
        .RequireAuthorization()
        .WithName("UpdateHousekeepingStatus").WithOpenApi();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  SETTINGS â€” Frontend expects /api/settings/property/{pid}
        //  Backend has /api/properties/{pid}/settings
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        app.MapGet("/api/settings/property/{propertyId:guid}", async (Guid propertyId, IUnitOfWork uow) =>
        {
            var settings = await uow.PropertySettings.GetByPropertyIdAsync(propertyId);
            if (settings is null)
            {
                settings = PropertySettings.Create(propertyId);
                await uow.PropertySettings.AddAsync(settings);
                await uow.SaveChangesAsync();
            }
            return Results.Ok(new PropertySettingsResponse(settings));
        })
        .WithTags("Property Settings")
        .RequireAuthorization()
        .WithName("GetSettingsCompat").WithOpenApi().ExcludeFromDescription();

        app.MapPut("/api/settings/property/{propertyId:guid}/operational", async (
            Guid propertyId, UpdateOperationalSettingsRequest req, IUnitOfWork uow) =>
        {
            var settings = await uow.PropertySettings.GetByPropertyIdAsync(propertyId);
            if (settings is null)
            {
                settings = PropertySettings.Create(propertyId);
                await uow.PropertySettings.AddAsync(settings);
            }
            settings.UpdateOperationalSettings(
                req.CheckInTime, req.CheckOutTime, req.VATRate, req.TourismLevyRate,
                req.DefaultCurrency, req.Timezone, req.MaxAdvanceBookingDays,
                req.DefaultCancellationHours, req.LateCancellationPenaltyPercent,
                req.NoShowPenaltyPercent);
            await uow.SaveChangesAsync();
            return Results.Ok(new PropertySettingsResponse(settings));
        })
        .WithTags("Property Settings")
        .RequireAuthorization()
        .WithName("UpdateSettingsCompat").WithOpenApi().ExcludeFromDescription();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  NOTIFICATIONS â€” Frontend expects /api/notifications/{pid}
        //  Backend has /api/properties/{pid}/notifications
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        app.MapGet("/api/notifications/{propertyId:guid}", async (
            Guid propertyId, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var query = db.Notifications
                .Where(n => n.PropertyId == propertyId)
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id, n.PropertyId,
                    Type = n.Type.ToString(),
                    Channel = n.Channel.ToString(),
                    Status = n.Status.ToString(),
                    n.RecipientAddress, n.Subject, n.CreatedAt, n.SentAt
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithTags("Notifications")
        .RequireAuthorization()
        .WithName("GetNotificationsCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/notifications/detail/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var notification = await db.Notifications
                .AsNoTracking()
                .Where(n => n.Id == id)
                .Select(n => new
                {
                    n.Id, n.PropertyId,
                    Type = n.Type.ToString(),
                    Channel = n.Channel.ToString(),
                    Status = n.Status.ToString(),
                    n.RecipientAddress, n.Subject, n.CreatedAt, n.SentAt
                })
                .FirstOrDefaultAsync();
            return notification is null ? Results.NotFound() : Results.Ok(notification);
        })
        .WithTags("Notifications")
        .RequireAuthorization()
        .WithName("GetNotificationDetailCompat").WithOpenApi().ExcludeFromDescription();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  HEALTH â€” Frontend expects /api/health
        //  Backend has /health (no /api prefix)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        app.MapGet("/api/health", () => Results.Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "2.1.0"
        }))
        .AllowAnonymous()
        .WithTags("Health")
        .WithName("HealthCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/health/detailed", async (ApplicationDbContext db) =>
        {
            var canConnect = await db.Database.CanConnectAsync();
            return Results.Ok(new
            {
                Status = canConnect ? "Healthy" : "Degraded",
                Database = canConnect ? "Connected" : "Disconnected",
                Timestamp = DateTime.UtcNow,
                Version = "2.1.0"
            });
        })
        .AllowAnonymous()
        .WithTags("Health")
        .WithName("HealthDetailedCompat").WithOpenApi().ExcludeFromDescription();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  EMAIL TEMPLATES â€” Frontend expects /api/email-templates
        //  Backend has /api/properties/{pid}/email-templates
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        app.MapGet("/api/email-templates", async (Guid propertyId, ApplicationDbContext db) =>
        {
            var templates = await db.EmailTemplates
                .Where(t => t.PropertyId == propertyId)
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => new
                {
                    t.Id, t.PropertyId, t.Name,
                    Type = t.NotificationType.ToString(),
                    Subject = t.SubjectTemplate,
                    HtmlBody = t.BodyHtmlTemplate,
                    t.IsActive, t.CreatedAt
                })
                .ToListAsync();
            return Results.Ok(templates);
        })
        .WithTags("Email Templates")
        .RequireAuthorization()
        .WithName("GetEmailTemplatesCompat").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/email-templates", async (CreateEmailTemplateCompatRequest req, ApplicationDbContext db) =>
        {
            var template = EmailTemplate.Create(
                req.PropertyId, req.Type, req.Name, req.Subject, req.HtmlBody);
            await db.EmailTemplates.AddAsync(template);
            await db.SaveChangesAsync();
            return Results.Created($"/api/email-templates/{template.Id}", new { template.Id, template.Name });
        })
        .WithTags("Email Templates")
        .RequireAuthorization()
        .WithName("CreateEmailTemplateCompat").WithOpenApi().ExcludeFromDescription();

        app.MapPut("/api/email-templates/{id:guid}", async (Guid id, UpdateEmailTemplateCompatRequest req, ApplicationDbContext db) =>
        {
            var template = await db.EmailTemplates.FindAsync(id);
            if (template is null) return Results.NotFound();
            template.Update(req.Name ?? template.Name, req.Subject, req.HtmlBody);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithTags("Email Templates")
        .RequireAuthorization()
        .WithName("UpdateEmailTemplateCompat").WithOpenApi().ExcludeFromDescription();

        app.MapDelete("/api/email-templates/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var template = await db.EmailTemplates.FindAsync(id);
            if (template is null) return Results.NotFound();
            db.EmailTemplates.Remove(template);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithTags("Email Templates")
        .RequireAuthorization()
        .WithName("DeleteEmailTemplateCompat").WithOpenApi().ExcludeFromDescription();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  HOUSEKEEPING â€” Frontend expects /api/housekeeping/tasks as prefix
        //  Backend has /api/housekeeping/{taskId}/assign (etc.) without /tasks prefix
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        app.MapGet("/api/housekeeping/tasks/detail/{taskId:guid}", async (Guid taskId, ApplicationDbContext db) =>
        {
            var task = await db.HousekeepingTasks
                .AsNoTracking()
                .Where(t => t.Id == taskId)
                .Select(t => new
                {
                    t.Id, t.RoomId, t.PropertyId, t.AssignedToStaffId,
                    Status = t.Status.ToString(),
                    TaskType = t.TaskType.ToString(),
                    Priority = t.Priority.ToString(),
                    t.Notes, t.StartedAt, t.CompletedAt, t.InspectedByStaffId, t.CreatedAt
                })
                .FirstOrDefaultAsync();
            return task is null ? Results.NotFound() : Results.Ok(task);
        })
        .WithTags("Housekeeping")
        .RequireAuthorization()
        .WithName("GetHousekeepingTaskDetail").WithOpenApi().ExcludeFromDescription();

        app.MapPut("/api/housekeeping/tasks/{taskId:guid}/assign", async (
            Guid taskId, AssignHousekeepingRequest req, IUnitOfWork uow) =>
        {
            var task = await uow.Housekeeping.GetByIdAsync(taskId);
            if (task is null) return Results.NotFound();
            task.AssignTo(req.StaffId);
            uow.Housekeeping.Update(task);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithTags("Housekeeping")
        .RequireAuthorization()
        .WithName("AssignHousekeepingTaskCompat").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/housekeeping/tasks", async (CreateHousekeepingTaskRequest req, IUnitOfWork uow) =>
        {
            var task = HousekeepingTask.Create(req.PropertyId, req.RoomId, req.TaskType, DateTime.UtcNow, req.Priority);
            await uow.Housekeeping.AddAsync(task);
            await uow.SaveChangesAsync();
            return Results.Created($"/api/housekeeping/tasks/{task.Id}", new { task.Id });
        })
        .WithTags("Housekeeping")
        .RequireAuthorization()
        .WithName("CreateHousekeepingTaskCompat").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/housekeeping/tasks/{taskId:guid}/start", async (Guid taskId, IUnitOfWork uow) =>
        {
            var task = await uow.Housekeeping.GetByIdAsync(taskId);
            if (task is null) return Results.NotFound();
            task.Start();
            uow.Housekeeping.Update(task);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithTags("Housekeeping")
        .RequireAuthorization()
        .WithName("StartHousekeepingTaskCompat").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/housekeeping/tasks/{taskId:guid}/complete", async (
            Guid taskId, CompleteHousekeepingRequest? req, IUnitOfWork uow) =>
        {
            var task = await uow.Housekeeping.GetByIdAsync(taskId);
            if (task is null) return Results.NotFound();
            task.Complete(
                req?.LinenChanged ?? true,
                req?.BathroomCleaned ?? true,
                req?.FloorsCleaned ?? true,
                req?.MinibarRestocked ?? true,
                req?.AmenitiesReplenished ?? true);
            uow.Housekeeping.Update(task);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithTags("Housekeeping")
        .RequireAuthorization()
        .WithName("CompleteHousekeepingTaskCompat").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/housekeeping/tasks/{taskId:guid}/inspect", async (
            Guid taskId, InspectHousekeepingRequest? req, IUnitOfWork uow) =>
        {
            var task = await uow.Housekeeping.GetByIdAsync(taskId);
            if (task is null) return Results.NotFound();
            task.Inspect(req?.InspectorId ?? Guid.Empty, req?.Passed ?? true, req?.Notes);
            uow.Housekeeping.Update(task);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithTags("Housekeeping")
        .RequireAuthorization()
        .WithName("InspectHousekeepingTaskCompat").WithOpenApi().ExcludeFromDescription();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  STAFF OVERTIME â€” Frontend expects /api/staff/overtime 
        //  Backend has /api/staff/overtime/request
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // Overtime â€” stub (no OvertimeRequests table yet)
        app.MapGet("/api/staff/overtime", (Guid? propertyId) =>
            Results.Ok(new { Items = Array.Empty<object>(), TotalCount = 0, Page = 1, PageSize = 25 }))
        .WithTags("Staff")
        .RequireAuthorization()
        .WithName("GetOvertimeRequestsCompat").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/staff/overtime", (CreateOvertimeRequestCompat req) =>
            Results.Ok(new
            {
                StaffMemberId = req.StaffMemberId,
                RequestedDate = req.RequestedDate,
                Hours = req.RequestedHours,
                Status = "PendingApproval",
                Message = "Overtime request submitted for approval."
            }))
        .WithTags("Staff")
        .RequireAuthorization()
        .WithName("CreateOvertimeCompat").WithOpenApi().ExcludeFromDescription();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  USER MANAGEMENT â€” Frontend expects /api/auth/users, /api/auth/roles
        //  Backend has /api/users, /api/roles
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        app.MapGet("/api/auth/users", async (Guid propertyId, int? page, int? pageSize,
            IAuthService authService) =>
        {
            var users = await authService.GetUsersByPropertyAsync(propertyId);
            var items = users.Select(u => new
            {
                u.Id, u.Email, u.FirstName, u.LastName, u.FullName,
                u.Phone, u.IsActive, u.IsLocked, u.LastLoginAt,
                Roles = u.UserRoles.Select(ur => ur.Role.Name)
            }).ToList();
            return Results.Ok(new { Items = items, TotalCount = items.Count, Page = 1, PageSize = items.Count });
        })
        .WithTags("Authentication")
        .RequireAuthorization()
        .WithName("GetUsersCompat").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/auth/users", async (AdminRegisterCompatRequest req, IAuthService authService) =>
        {
            var result = await authService.AdminRegisterAsync(new RegisterRequest(
                req.PropertyId, req.Email, req.Password, req.FirstName, req.LastName, req.Phone, req.Role));
            return result.Success
                ? Results.Created($"/api/users/{result.UserId}", result)
                : Results.BadRequest(result);
        })
        .WithTags("Authentication")
        .RequireAuthorization()
        .WithName("CreateUserCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/auth/roles", async (ApplicationDbContext db) =>
        {
            var roles = await db.Roles
                .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .AsNoTracking()
                .OrderBy(r => r.SortOrder)
                .Select(r => new
                {
                    r.Id, r.Name, r.Description, r.IsSystemRole, r.SortOrder,
                    Permissions = r.RolePermissions.Select(rp => rp.Permission.Name)
                })
                .ToListAsync();
            return Results.Ok(roles);
        })
        .WithTags("Authentication")
        .RequireAuthorization()
        .WithName("GetRolesCompat").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/auth/users/{userId:guid}/deactivate", async (Guid userId, IAuthService authService) =>
        {
            var success = await authService.DeactivateUserAsync(userId);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithTags("Authentication")
        .RequireAuthorization()
        .WithName("DeactivateUserCompat").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/auth/users/{userId:guid}/reactivate", async (Guid userId, IAuthService authService) =>
        {
            var success = await authService.ReactivateUserAsync(userId);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithTags("Authentication")
        .RequireAuthorization()
        .WithName("ReactivateUserCompat").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/auth/users/{userId:guid}/unlock", async (Guid userId, IAuthService authService) =>
        {
            var success = await authService.UnlockUserAsync(userId);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithTags("Authentication")
        .RequireAuthorization()
        .WithName("UnlockUserCompat").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/auth/users/{userId:guid}/roles", async (Guid userId, AssignRoleRequest req, IAuthService authService) =>
        {
            var success = await authService.AssignRoleAsync(userId, req.RoleName);
            return success ? Results.NoContent() : Results.BadRequest();
        })
        .WithTags("Authentication")
        .RequireAuthorization()
        .WithName("AssignUserRoleCompat").WithOpenApi().ExcludeFromDescription();

        app.MapDelete("/api/auth/users/{userId:guid}/roles/{roleId:guid}", async (
            Guid userId, Guid roleId, ApplicationDbContext db) =>
        {
            var userRole = await db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
            if (userRole is null) return Results.NotFound();
            db.UserRoles.Remove(userRole);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithTags("Authentication")
        .RequireAuthorization()
        .WithName("RemoveUserRoleCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/auth/permissions", async (ApplicationDbContext db) =>
        {
            var permissions = await db.Permissions
                .OrderBy(p => p.Module).ThenBy(p => p.Name)
                .Select(p => new { p.Id, p.Name, p.Module, p.Description })
                .ToListAsync();
            return Results.Ok(permissions);
        })
        .WithTags("Authentication")
        .RequireAuthorization()
        .WithName("GetPermissionsCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/auth/permissions/all", async (ApplicationDbContext db) =>
        {
            var names = await db.Permissions.Select(p => p.Name).ToListAsync();
            return Results.Ok(names);
        })
        .WithTags("Authentication")
        .RequireAuthorization()
        .WithName("GetAllPermissionNamesCompat").WithOpenApi().ExcludeFromDescription();

        app.MapPut("/api/auth/roles/{roleId:guid}/permissions", async (
            Guid roleId, UpdateRolePermissionsRequest req, ApplicationDbContext db) =>
        {
            var existing = await db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
            db.RolePermissions.RemoveRange(existing);
            var allPerms = await db.Permissions.ToListAsync();
            foreach (var permName in req.Permissions)
            {
                var perm = allPerms.FirstOrDefault(p => p.Name == permName);
                if (perm is not null)
                    await db.RolePermissions.AddAsync(RolePermission.Create(roleId, perm.Id));
            }
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithTags("Authentication")
        .RequireAuthorization()
        .WithName("UpdateRolePermissionsCompat").WithOpenApi().ExcludeFromDescription();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  INVOICE â€” Frontend expects /api/invoices/generate/{folioId}
        //  and /api/invoices/{id} (by GUID). Backend has /api/invoices/{invoiceNumber}
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        app.MapPost("/api/invoices/generate/{folioId:guid}", async (Guid folioId, ApplicationDbContext db) =>
        {
            var folio = await db.Folios.Include(f => f.LineItems).FirstOrDefaultAsync(f => f.Id == folioId);
            if (folio is null) return Results.NotFound();
            var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            var subtotal = folio.TotalCharges;
            var vatAmount = subtotal * 0.15m; // 15% VAT (SA default)
            var tourismLevy = subtotal * 0.01m; // 1% tourism levy
            var invoice = Invoice.Create(
                folio.PropertyId, folio.Id, folio.GuestId, invoiceNumber,
                DateTime.UtcNow, DateTime.UtcNow.AddDays(30),
                subtotal, vatAmount, tourismLevy);
            await db.Invoices.AddAsync(invoice);
            await db.SaveChangesAsync();
            return Results.Created($"/api/invoices/{invoice.Id}", new
            {
                invoice.Id, invoice.InvoiceNumber, invoice.TotalAmount,
                Status = invoice.Status.ToString(),
                invoice.OutstandingAmount,
                invoice.CreatedAt
            });
        })
        .WithTags("Financial")
        .RequireAuthorization()
        .WithName("GenerateInvoiceCompat").WithOpenApi().ExcludeFromDescription();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  PAYMENTS â€” Frontend expects /api/payments/booking/{bookingId}
        //  Backend only has /api/payments/folio/{folioId}
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        app.MapGet("/api/payments/booking/{bookingId:guid}", async (
            Guid bookingId, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            // Find folio for booking, then get payments
            var folio = await db.Folios.FirstOrDefaultAsync(f => f.BookingId == bookingId);
            if (folio is null)
                return Results.Ok(new { Items = Array.Empty<object>(), TotalCount = 0, Page = 1, PageSize = 25 });

            var query = db.Payments
                .Where(p => p.FolioId == folio.Id)
                .AsNoTracking()
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new
                {
                    p.Id, p.FolioId, p.Amount,
                    Method = p.Method.ToString(),
                    p.TransactionReference, p.PaymentDate, p.CreatedAt
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithTags("Financial")
        .RequireAuthorization()
        .WithName("GetPaymentsByBookingCompat").WithOpenApi().ExcludeFromDescription();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  PORTAL-SPECIFIC â€” Portal calls api/portal/dashboard
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        app.MapGet("/api/portal/dashboard", async (HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId))
                return Results.Unauthorized();

            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var totalRooms = await db.Rooms.CountAsync(r => r.PropertyId == propertyId && r.IsActive);
            var occupiedRooms = await db.Rooms.CountAsync(r => r.PropertyId == propertyId && r.Status == RoomStatus.Occupied);
            var todayCheckIns = await db.Bookings.CountAsync(b => b.PropertyId == propertyId && b.CheckInDate.Date == today && b.Status == BookingStatus.Confirmed);
            var monthRevenue = await db.Payments
                .Where(p => p.Folio!.PropertyId == propertyId && p.PaymentDate >= monthStart)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            return Results.Ok(new
            {
                TotalRooms = totalRooms,
                OccupiedRooms = occupiedRooms,
                OccupancyRate = totalRooms > 0 ? Math.Round((decimal)occupiedRooms / totalRooms * 100, 1) : 0,
                TodayCheckIns = todayCheckIns,
                MonthRevenue = monthRevenue,
                OpenTasks = await db.HousekeepingTasks.CountAsync(t => t.PropertyId == propertyId && (t.Status == HousekeepingTaskStatus.Pending || t.Status == HousekeepingTaskStatus.Assigned)),
                PendingBookings = await db.Bookings.CountAsync(b => b.PropertyId == propertyId && b.Status == BookingStatus.Confirmed && b.CheckInDate >= today)
            });
        })
        .WithTags("Portal")
        .RequireAuthorization()
        .WithName("PortalDashboard").WithOpenApi();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  PWA guest-specific endpoints
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        // GET /api/feedback â€” stub for guest feedback
        app.MapPost("/api/feedback", () => Results.Ok(new { Message = "Feedback received" }))
            .WithTags("Guest").RequireAuthorization()
            .WithName("SubmitFeedback").WithOpenApi().ExcludeFromDescription();

        // Service Requests
        app.MapPost("/api/service-requests", () =>
            Results.Created("/api/service-requests/1", new { Id = Guid.NewGuid(), Status = "Submitted" }))
            .WithTags("Guest").RequireAuthorization()
            .WithName("CreateServiceRequest").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/service-requests/my", () =>
            Results.Ok(Array.Empty<object>()))
            .WithTags("Guest").RequireAuthorization()
            .WithName("GetMyServiceRequests").WithOpenApi().ExcludeFromDescription();

        // Activities & Dining
        app.MapGet("/api/activities/{propertyId:guid}", async (Guid propertyId, ApplicationDbContext db) =>
        {
            var experiences = await db.Experiences
                .Where(e => e.PropertyId == propertyId && e.IsActive)
                .AsNoTracking()
                .Select(e => new { e.Id, e.Name, e.Description, e.BasePrice, e.MaxGuests, e.DurationMinutes })
                .ToListAsync();
            return Results.Ok(experiences);
        })
        .WithTags("Guest").RequireAuthorization()
        .WithName("GetGuestActivities").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/dining/{propertyId:guid}", async (Guid propertyId, ApplicationDbContext db) =>
        {
            var venues = await db.DiningVenues
                .Where(d => d.PropertyId == propertyId && d.IsActive)
                .AsNoTracking()
                .Select(d => new
                {
                    d.Id, d.Name, d.Description, d.CuisineType,
                    VenueType = d.VenueType.ToString(),
                    d.OpeningTime, d.ClosingTime, d.MenuUrl,
                    d.ImageUrl, d.Capacity
                }).ToListAsync();
            return Results.Ok(venues);
        })
            .WithTags("Guest").RequireAuthorization()
            .WithName("GetDiningOptions").WithOpenApi().ExcludeFromDescription();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  PORTAL analytic endpoints
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        app.MapGet("/api/analytics/occupancy", async (
            DateTime? from, DateTime? to, HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var totalRooms = await db.Rooms.CountAsync(r => r.PropertyId == propertyId && r.IsActive);
            var occupied = await db.Rooms.CountAsync(r => r.PropertyId == propertyId && r.Status == RoomStatus.Occupied);
            return Results.Ok(new
            {
                TotalRooms = totalRooms,
                OccupiedRooms = occupied,
                OccupancyRate = totalRooms > 0 ? Math.Round((decimal)occupied / totalRooms * 100, 1) : 0,
                Period = new { From = from ?? DateTime.UtcNow.AddDays(-30), To = to ?? DateTime.UtcNow }
            });
        })
        .WithTags("Analytics").RequireAuthorization()
        .WithName("AnalyticsOccupancy").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/analytics/revenue", async (
            DateTime? from, DateTime? to, HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var start = from ?? DateTime.UtcNow.AddDays(-30);
            var end = to ?? DateTime.UtcNow;
            var revenue = await db.Payments
                .Where(p => p.Folio!.PropertyId == propertyId && p.PaymentDate >= start && p.PaymentDate <= end)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
            return Results.Ok(new { TotalRevenue = revenue, Period = new { From = start, To = end } });
        })
        .WithTags("Analytics").RequireAuthorization()
        .WithName("AnalyticsRevenue").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/analytics/booking-sources", async (HttpContext context, DateTime? from, DateTime? to, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var start = from ?? DateTime.UtcNow.AddDays(-90);
            var end = to ?? DateTime.UtcNow;
            var sources = await db.Bookings
                .Where(b => b.PropertyId == propertyId && b.CreatedAt >= start && b.CreatedAt <= end)
                .GroupBy(b => b.Source)
                .Select(g => new { Source = g.Key.ToString(), Count = g.Count(), Revenue = g.Sum(b => b.TotalAmount) })
                .OrderByDescending(x => x.Count).ToListAsync();
            var total = sources.Sum(s => s.Count);
            var result = sources.Select(s => new { s.Source, s.Count, s.Revenue, Percentage = total > 0 ? Math.Round((decimal)s.Count / total * 100, 1) : 0 });
            return Results.Ok(new { Sources = result, TotalBookings = total, Period = new { From = start, To = end } });
        })
            .WithTags("Analytics").RequireAuthorization()
            .WithName("AnalyticsBookingSources").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/analytics/guest-demographics", async (HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var byNationality = await db.Guests
                .Where(g => g.PropertyId == propertyId && g.Nationality != null)
                .GroupBy(g => g.Nationality!)
                .Select(g => new { Nationality = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).Take(20).ToListAsync();
            var byType = await db.Guests
                .Where(g => g.PropertyId == propertyId)
                .GroupBy(g => g.GuestType)
                .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
                .OrderByDescending(x => x.Count).ToListAsync();
            var totalGuests = await db.Guests.CountAsync(g => g.PropertyId == propertyId);
            return Results.Ok(new { TotalGuests = totalGuests, ByNationality = byNationality, ByType = byType });
        })
            .WithTags("Analytics").RequireAuthorization()
            .WithName("AnalyticsGuestDemographics").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/analytics/seasonal", async (HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            // Monthly booking & revenue breakdown for the past 12 months
            var yearAgo = DateTime.UtcNow.AddMonths(-12);
            var monthly = await db.Bookings
                .Where(b => b.PropertyId == propertyId && b.CreatedAt >= yearAgo)
                .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Bookings = g.Count(), Revenue = g.Sum(b => b.TotalAmount) })
                .OrderBy(x => x.Year).ThenBy(x => x.Month).ToListAsync();
            var seasons = await db.Seasons
                .Where(s => s.PropertyId == propertyId).AsNoTracking()
                .Select(s => new { s.Name, s.StartDate, s.EndDate, s.PriceMultiplier }).ToListAsync();
            return Results.Ok(new { Monthly = monthly, Seasons = seasons });
        })
            .WithTags("Analytics").RequireAuthorization()
            .WithName("AnalyticsSeasonal").WithOpenApi().ExcludeFromDescription();

        // Portal financial endpoints
        app.MapGet("/api/financial/revenue", async (
            DateTime? from, DateTime? to, HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var start = from ?? DateTime.UtcNow.AddDays(-30);
            var end = to ?? DateTime.UtcNow;
            var revenue = await db.Payments
                .Where(p => p.Folio!.PropertyId == propertyId && p.PaymentDate >= start && p.PaymentDate <= end)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
            return Results.Ok(new { TotalRevenue = revenue, Period = new { From = start, To = end } });
        })
        .WithTags("Financial").RequireAuthorization()
        .WithName("FinancialRevenue").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/financial/folios", async (
            HttpContext context, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var query = db.Folios.Where(f => f.PropertyId == propertyId).AsNoTracking()
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new { f.Id, f.BookingId, f.GuestId, TotalAmount = f.TotalCharges, Balance = f.Balance, Status = f.Status.ToString(), f.CreatedAt });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithTags("Financial").RequireAuthorization()
        .WithName("FinancialFolios").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/financial/invoices", async (
            HttpContext context, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var query = db.Invoices.Where(i => i.PropertyId == propertyId).AsNoTracking()
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new { i.Id, i.InvoiceNumber, i.TotalAmount, i.PaidAmount, Status = i.Status.ToString(), i.CreatedAt, OutstandingAmount = i.TotalAmount - i.PaidAmount });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithTags("Financial").RequireAuthorization()
        .WithName("FinancialInvoices").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/financial/payments", async (
            HttpContext context, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var query = db.Payments
                .Where(p => p.Folio!.PropertyId == propertyId).AsNoTracking()
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new { p.Id, p.FolioId, p.Amount, Method = p.Method.ToString(), p.TransactionReference, p.PaymentDate });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithTags("Financial").RequireAuthorization()
        .WithName("FinancialPayments").WithOpenApi().ExcludeFromDescription();

        // Portal report endpoints — real aggregated data
        app.MapGet("/api/reports/daily", async (HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var today = DateTime.UtcNow.Date;
            var todayEnd = today.AddDays(1);
            var totalRooms = await db.Rooms.CountAsync(r => r.PropertyId == propertyId && r.IsActive);
            var occupied = await db.Rooms.CountAsync(r => r.PropertyId == propertyId && r.Status == RoomStatus.Occupied);
            var checkIns = await db.Bookings.CountAsync(b => b.PropertyId == propertyId && b.CheckInDate.Date == today && b.Status == BookingStatus.CheckedIn);
            var checkOuts = await db.Bookings.CountAsync(b => b.PropertyId == propertyId && b.CheckOutDate.Date == today && b.Status == BookingStatus.CheckedOut);
            var revenue = await db.Payments.Where(p => p.Folio!.PropertyId == propertyId && p.PaymentDate >= today && p.PaymentDate < todayEnd).SumAsync(p => (decimal?)p.Amount) ?? 0;
            var newBookings = await db.Bookings.CountAsync(b => b.PropertyId == propertyId && b.CreatedAt >= today && b.CreatedAt < todayEnd);
            return Results.Ok(new
            {
                PropertyId = propertyId, Date = today, Status = "Generated",
                TotalRooms = totalRooms, OccupiedRooms = occupied,
                OccupancyRate = totalRooms > 0 ? Math.Round((decimal)occupied / totalRooms * 100, 1) : 0,
                CheckIns = checkIns, CheckOuts = checkOuts, Revenue = revenue, NewBookings = newBookings
            });
        })
        .WithTags("Reports").RequireAuthorization()
        .WithName("DailyReportCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/reports/monthly", async (DateTime? from, DateTime? to, HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var start = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = to ?? DateTime.UtcNow;
            var totalBookings = await db.Bookings.CountAsync(b => b.PropertyId == propertyId && b.CreatedAt >= start && b.CreatedAt <= end);
            var revenue = await db.Payments.Where(p => p.Folio!.PropertyId == propertyId && p.PaymentDate >= start && p.PaymentDate <= end).SumAsync(p => (decimal?)p.Amount) ?? 0;
            var avgDailyRate = await db.Bookings.Where(b => b.PropertyId == propertyId && b.CreatedAt >= start && b.CreatedAt <= end && b.TotalAmount > 0).AverageAsync(b => (decimal?)b.TotalAmount) ?? 0;
            var cancellations = await db.Bookings.CountAsync(b => b.PropertyId == propertyId && b.Status == BookingStatus.Cancelled && b.UpdatedAt >= start && b.UpdatedAt <= end);
            return Results.Ok(new
            {
                PropertyId = propertyId, Period = new { From = start, To = end }, Status = "Generated",
                TotalBookings = totalBookings, Revenue = revenue,
                AverageDailyRate = Math.Round(avgDailyRate, 2), Cancellations = cancellations
            });
        })
        .WithTags("Reports").RequireAuthorization()
        .WithName("MonthlyReportCompat").WithOpenApi().ExcludeFromDescription();

        // Portal bookings/rooms/staff/rates/compliance/tax stubs
        app.MapGet("/api/bookings", async (HttpContext context, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var query = db.Bookings.Where(b => b.PropertyId == propertyId).AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new { b.Id, b.GuestId, b.CheckInDate, b.CheckOutDate, Status = b.Status.ToString(), b.TotalAmount, b.CreatedAt });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithTags("Bookings").RequireAuthorization()
        .WithName("GetBookingsCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/bookings/summary", async (HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var today = DateTime.UtcNow.Date;
            var totalBookings = await db.Bookings.CountAsync(b => b.PropertyId == propertyId);
            var upcomingCheckIns = await db.Bookings.CountAsync(b => b.PropertyId == propertyId && b.CheckInDate.Date == today && b.Status == BookingStatus.Confirmed);
            var upcomingCheckOuts = await db.Bookings.CountAsync(b => b.PropertyId == propertyId && b.CheckOutDate.Date == today && b.Status == BookingStatus.CheckedIn);
            var pendingBookings = await db.Bookings.CountAsync(b => b.PropertyId == propertyId && b.Status == BookingStatus.Confirmed && b.CheckInDate > today);
            var cancelledToday = await db.Bookings.CountAsync(b => b.PropertyId == propertyId && b.Status == BookingStatus.Cancelled && b.UpdatedAt.Date == today);
            return Results.Ok(new { TotalBookings = totalBookings, UpcomingCheckIns = upcomingCheckIns, UpcomingCheckOuts = upcomingCheckOuts, PendingBookings = pendingBookings, CancelledToday = cancelledToday });
        })
            .WithTags("Bookings").RequireAuthorization()
            .WithName("BookingSummaryCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/rooms", async (HttpContext context, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var query = db.Rooms.Include(r => r.RoomType).Where(r => r.PropertyId == propertyId).AsNoTracking()
                .OrderBy(r => r.RoomNumber)
                .Select(r => new { r.Id, r.RoomNumber, r.Floor, Status = r.Status.ToString(), HkStatus = r.HkStatus.ToString(), r.IsActive, RoomType = r.RoomType == null ? null : new { r.RoomType.Name } });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 50));
        })
        .WithTags("Rooms").RequireAuthorization()
        .WithName("GetRoomsCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/rooms/availability", async (HttpContext context, DateTime? from, DateTime? to, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var startDate = from ?? DateTime.UtcNow.Date;
            var endDate = to ?? startDate.AddDays(30);
            // Get all active rooms with their types and current status
            var rooms = await db.Rooms.Include(r => r.RoomType)
                .Where(r => r.PropertyId == propertyId && r.IsActive)
                .AsNoTracking()
                .Select(r => new
                {
                    r.Id, r.RoomNumber, r.Floor, r.Wing,
                    Status = r.Status.ToString(),
                    HkStatus = r.HkStatus.ToString(),
                    RoomType = r.RoomType == null ? null : new { r.RoomType.Id, r.RoomType.Name, r.RoomType.BasePrice, r.RoomType.MaxGuests }
                }).ToListAsync();
            // Get bookings that overlap the requested range
            var bookedRoomIds = await db.BookingRooms
                .Where(br => br.Booking!.PropertyId == propertyId
                    && br.Booking.Status != BookingStatus.Cancelled
                    && br.Booking.Status != BookingStatus.NoShow
                    && br.Booking.CheckInDate < endDate
                    && br.Booking.CheckOutDate > startDate)
                .Select(br => br.RoomId)
                .Distinct().ToListAsync();
            var available = rooms.Where(r => !bookedRoomIds.Contains(r.Id)).ToList();
            return Results.Ok(new { Available = available, Total = rooms.Count, AvailableCount = available.Count, Period = new { From = startDate, To = endDate } });
        })
            .WithTags("Rooms").RequireAuthorization()
            .WithName("RoomAvailabilityCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/rooms/types", async (HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Ok(Array.Empty<object>());
            var types = await db.RoomTypes.Where(rt => rt.PropertyId == propertyId).AsNoTracking()
                .Select(rt => new { rt.Id, rt.Name, rt.Code, rt.BasePrice, rt.MaxGuests }).ToListAsync();
            return Results.Ok(types);
        })
        .WithTags("Rooms").RequireAuthorization()
        .WithName("RoomTypesCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/staff", async (HttpContext context, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var query = db.StaffMembers.Where(s => s.PropertyId == propertyId).AsNoTracking()
                .OrderBy(s => s.LastName)
                .Select(s => new { s.Id, s.FirstName, s.LastName, s.Email, Role = s.Role.ToString(), s.IsActive });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithTags("Staff").RequireAuthorization()
        .WithName("GetStaffCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/staff/attendance", async (HttpContext context, DateTime? from, DateTime? to, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var start = from ?? DateTime.UtcNow.Date.AddDays(-7);
            var end = to ?? DateTime.UtcNow;
            var query = db.StaffAttendances
                .Where(a => a.PropertyId == propertyId && a.CheckInTime >= start && a.CheckInTime <= end)
                .AsNoTracking()
                .Join(db.StaffMembers, a => a.StaffId, s => s.Id, (a, s) => new
                {
                    a.Id, a.StaffId, StaffName = s.FirstName + " " + s.LastName,
                    Role = s.Role.ToString(), a.CheckInTime, a.CheckOutTime,
                    Status = a.Status.ToString(), ShiftType = a.ShiftType.ToString(),
                    a.ActualHours, a.OvertimeHours, a.TotalWage, a.BreakDuration,
                    a.CardUid, a.CreatedAt
                })
                .OrderByDescending(a => a.CheckInTime);
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
            .WithTags("Staff").RequireAuthorization()
            .WithName("StaffAttendanceCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/staff/compliance", async (HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var issues = new List<object>();
            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            // Check for staff exceeding max weekly hours (BCEA: 45 hours)
            var weeklyHours = await db.StaffAttendances
                .Where(a => a.PropertyId == propertyId && a.CheckInTime >= weekStart && a.CheckOutTime.HasValue)
                .GroupBy(a => a.StaffId)
                .Select(g => new { StaffId = g.Key, TotalHours = g.Sum(a => a.ActualHours) })
                .Where(x => x.TotalHours > 45)
                .ToListAsync();
            foreach (var w in weeklyHours)
                issues.Add(new { Type = "ExcessiveWeeklyHours", w.StaffId, Hours = w.TotalHours, Limit = 45, Severity = "Warning" });
            // Check for missed check-outs (checked in > 16 hours ago)
            var stuckCheckIns = await db.StaffAttendances
                .CountAsync(a => a.PropertyId == propertyId && a.Status == AttendanceStatus.CheckedIn && a.CheckInTime < DateTime.UtcNow.AddHours(-16));
            if (stuckCheckIns > 0)
                issues.Add(new { Type = "MissedCheckOut", Count = stuckCheckIns, Severity = "Critical" });
            return Results.Ok(new { IsCompliant = issues.Count == 0, Issues = issues, CheckedAt = DateTime.UtcNow });
        })
            .WithTags("Staff").RequireAuthorization()
            .WithName("StaffComplianceCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/guests", async (HttpContext context, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var query = db.Guests.Where(g => g.PropertyId == propertyId).AsNoTracking()
                .OrderBy(g => g.LastName)
                .Select(g => new { g.Id, g.FirstName, g.LastName, g.Email, g.Phone, g.GuestType, g.IsBlacklisted });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithTags("Guests").RequireAuthorization()
        .WithName("GetGuestsCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/guests/returning", async (int? top, HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var limit = top ?? 20;
            var returningGuests = await db.Bookings
                .Where(b => b.PropertyId == propertyId && (b.Status == BookingStatus.CheckedOut || b.Status == BookingStatus.Confirmed))
                .GroupBy(b => b.GuestId)
                .Where(g => g.Count() > 1)
                .Select(g => new { GuestId = g.Key, StayCount = g.Count(), LastStay = g.Max(b => b.CheckOutDate), TotalSpent = g.Sum(b => b.TotalAmount) })
                .OrderByDescending(g => g.StayCount)
                .Take(limit)
                .ToListAsync();
            var guestIds = returningGuests.Select(r => r.GuestId).ToList();
            var guests = await db.Guests.Where(g => guestIds.Contains(g.Id)).AsNoTracking()
                .Select(g => new { g.Id, g.FirstName, g.LastName, g.Email, g.Phone }).ToListAsync();
            var result = returningGuests.Select(r => {
                var g = guests.FirstOrDefault(x => x.Id == r.GuestId);
                return new { r.GuestId, GuestName = g != null ? $"{g.FirstName} {g.LastName}" : "Unknown", g?.Email, r.StayCount, r.LastStay, r.TotalSpent };
            });
            return Results.Ok(result);
        })
            .WithTags("Guests").RequireAuthorization()
            .WithName("ReturningGuestsCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/rates/seasons", async (HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Ok(Array.Empty<object>());
            var seasons = await db.Seasons.Where(s => s.PropertyId == propertyId).AsNoTracking()
                .Select(s => new { s.Id, s.Name, s.StartDate, s.EndDate, s.PriceMultiplier }).ToListAsync();
            return Results.Ok(seasons);
        })
        .WithTags("Rates").RequireAuthorization()
        .WithName("SeasonsCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/rates/effective", async (HttpContext context, DateTime? date, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var targetDate = date ?? DateTime.UtcNow.Date;
            // Get the active season for the target date
            var activeSeason = await db.Seasons
                .Where(s => s.PropertyId == propertyId && s.StartDate <= targetDate && s.EndDate >= targetDate)
                .AsNoTracking().FirstOrDefaultAsync();
            var multiplier = activeSeason?.PriceMultiplier ?? 1.0m;
            // Get all room types with effective rate = base * season multiplier
            var rates = await db.RoomTypes
                .Where(rt => rt.PropertyId == propertyId)
                .AsNoTracking()
                .Select(rt => new
                {
                    rt.Id, rt.Name, rt.Code, BasePrice = rt.BasePrice,
                    SeasonMultiplier = multiplier,
                    EffectiveRate = rt.BasePrice * multiplier,
                    Season = activeSeason != null ? activeSeason.Name : "Standard",
                    Date = targetDate
                }).ToListAsync();
            return Results.Ok(rates);
        })
            .WithTags("Rates").RequireAuthorization()
            .WithName("EffectiveRatesCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/settings/property", async (HttpContext context, IUnitOfWork uow) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var settings = await uow.PropertySettings.GetByPropertyIdAsync(propertyId);
            if (settings is null)
            {
                settings = PropertySettings.Create(propertyId);
                await uow.PropertySettings.AddAsync(settings);
                await uow.SaveChangesAsync();
            }
            return Results.Ok(new PropertySettingsResponse(settings));
        })
        .WithTags("Settings").RequireAuthorization()
        .WithName("SettingsPropertyCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/compliance/tax", async (HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthRevenue = await db.Payments
                .Where(p => p.Folio!.PropertyId == propertyId && p.PaymentDate >= monthStart)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
            var vatDue = monthRevenue * 0.15m;
            var tourismLevyDue = monthRevenue * 0.01m;
            var lastInvoice = await db.Invoices
                .Where(i => i.PropertyId == propertyId)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => i.CreatedAt).FirstOrDefaultAsync();
            return Results.Ok(new
            {
                Status = "Compliant", PropertyId = propertyId,
                CurrentMonth = new { Revenue = monthRevenue, VatDue = vatDue, TourismLevyDue = tourismLevyDue },
                LastInvoiceDate = lastInvoice, CheckedAt = DateTime.UtcNow
            });
        })
            .WithTags("Compliance").RequireAuthorization()
            .WithName("TaxComplianceCompat").WithOpenApi().ExcludeFromDescription();

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  MAUI Staff Dashboard & Mobile-specific endpoints
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        app.MapGet("/api/staff/dashboard", async (HttpContext context, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var userId = GetUserId(context);
            var today = DateTime.UtcNow.Date;
            var pendingTasks = await db.HousekeepingTasks.CountAsync(t => t.PropertyId == propertyId && t.Status == HousekeepingTaskStatus.Pending);
            var myTasksToday = userId.HasValue
                ? await db.HousekeepingTasks.CountAsync(t => t.AssignedToStaffId == userId.Value && t.CreatedAt.Date == today && t.Status != HousekeepingTaskStatus.Completed && t.Status != HousekeepingTaskStatus.Inspected)
                : 0;
            var staffOnDuty = await db.StaffAttendances.CountAsync(a => a.PropertyId == propertyId && a.CheckInTime.Date == today && a.Status == AttendanceStatus.CheckedIn);
            var openMaintenanceTasks = await db.MaintenanceTasks.CountAsync(m => m.PropertyId == propertyId && (m.Status == MaintenanceStatus.Open || m.Status == MaintenanceStatus.Assigned || m.Status == MaintenanceStatus.InProgress));
            var pendingOvertimeRequests = await db.OvertimeRequests.CountAsync(o => o.PropertyId == propertyId && o.Status == OvertimeRequestStatus.PendingApproval);
            return Results.Ok(new { PendingTasks = pendingTasks, MyTasksToday = myTasksToday, StaffOnDuty = staffOnDuty, OpenMaintenanceTasks = openMaintenanceTasks, PendingOvertimeRequests = pendingOvertimeRequests });
        })
        .WithTags("Staff").RequireAuthorization()
        .WithName("StaffDashboard").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/staff/my-attendance", async (HttpContext context, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();
            // Find staff member by email matching the auth user's email
            var email = context.User.FindFirstValue(ClaimTypes.Email) ?? context.User.FindFirstValue("email");
            var staff = email != null ? await db.StaffMembers.FirstOrDefaultAsync(s => s.Email == email) : null;
            if (staff is null) return Results.Ok(Array.Empty<object>());
            var query = db.StaffAttendances
                .Where(a => a.StaffId == staff.Id)
                .AsNoTracking()
                .OrderByDescending(a => a.CheckInTime)
                .Select(a => new
                {
                    a.Id, a.CheckInTime, a.CheckOutTime, Status = a.Status.ToString(),
                    ShiftType = a.ShiftType.ToString(), a.ActualHours, a.OvertimeHours,
                    a.TotalWage, a.BreakDuration, a.CreatedAt
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
            .WithTags("Staff").RequireAuthorization()
            .WithName("MyAttendanceCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/staff/my-attendance/today", async (HttpContext context, ApplicationDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();
            var email = context.User.FindFirstValue(ClaimTypes.Email) ?? context.User.FindFirstValue("email");
            var staff = email != null ? await db.StaffMembers.FirstOrDefaultAsync(s => s.Email == email) : null;
            if (staff is null) return Results.Ok(new { CheckedIn = false });
            var today = DateTime.UtcNow.Date;
            var attendance = await db.StaffAttendances
                .Where(a => a.StaffId == staff.Id && a.CheckInTime.Date == today)
                .OrderByDescending(a => a.CheckInTime)
                .Select(a => new
                {
                    a.Id, CheckedIn = true, a.CheckInTime, a.CheckOutTime,
                    Status = a.Status.ToString(), ShiftType = a.ShiftType.ToString(),
                    a.ActualHours, a.BreakStart, a.BreakEnd, a.BreakDuration
                })
                .FirstOrDefaultAsync();
            return Results.Ok(attendance ?? (object)new { CheckedIn = false });
        })
            .WithTags("Staff").RequireAuthorization()
            .WithName("MyAttendanceTodayCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/staff/my-schedule", async (DateTime? from, DateTime? to, HttpContext context, ApplicationDbContext db) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();
            var email = context.User.FindFirstValue(ClaimTypes.Email) ?? context.User.FindFirstValue("email");
            var staff = email != null ? await db.StaffMembers.FirstOrDefaultAsync(s => s.Email == email) : null;
            if (staff is null) return Results.Ok(Array.Empty<object>());
            var start = from ?? DateTime.UtcNow.Date;
            var end = to ?? start.AddDays(14);
            // Return attendance history as a schedule view
            var schedule = await db.StaffAttendances
                .Where(a => a.StaffId == staff.Id && a.CheckInTime.Date >= start.Date && a.CheckInTime.Date <= end.Date)
                .AsNoTracking()
                .OrderBy(a => a.CheckInTime)
                .Select(a => new
                {
                    Date = a.CheckInTime.Date, ShiftType = a.ShiftType.ToString(),
                    Start = a.CheckInTime, End = a.CheckOutTime,
                    a.ScheduledHours, a.ActualHours, Status = a.Status.ToString()
                }).ToListAsync();
            return Results.Ok(schedule);
        })
            .WithTags("Staff").RequireAuthorization()
            .WithName("MyScheduleCompat").WithOpenApi().ExcludeFromDescription();

        app.MapGet("/api/housekeeping/my-tasks", async (HttpContext context, ApplicationDbContext db) =>
        {
            var userIdStr = context.User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();
            var tasks = await db.HousekeepingTasks.Where(t => t.AssignedToStaffId == userId)
                .AsNoTracking().OrderByDescending(t => t.CreatedAt)
                .Select(t => new { t.Id, t.RoomId, Status = t.Status.ToString(), TaskType = t.TaskType.ToString(), t.Notes, t.CreatedAt })
                .ToListAsync();
            return Results.Ok(tasks);
        })
        .WithTags("Housekeeping").RequireAuthorization()
        .WithName("MyHousekeepingTasks").WithOpenApi().ExcludeFromDescription();

        // Attendance RFID/Manual endpoints for MAUI — real StaffAttendance creation
        app.MapPost("/api/attendance/rfid/check-in", async (CompatRfidCheckInRequest req, HttpContext context, ApplicationDbContext db) =>
        {
            var card = await db.RfidCards.Include(c => c.StaffMember).FirstOrDefaultAsync(c => c.CardUid == req.CardUid && c.Status == RfidCardStatus.Active);
            if (card is null) return Results.NotFound(new { Error = "No active RFID card found." });
            var staff = card.StaffMember;
            // Prevent duplicate check-in
            var today = DateTime.UtcNow.Date;
            var existing = await db.StaffAttendances.FirstOrDefaultAsync(a => a.StaffId == staff.Id && a.CheckInTime.Date == today && a.Status == AttendanceStatus.CheckedIn);
            if (existing is not null) return Results.Conflict(new { Error = "Already checked in", AttendanceId = existing.Id });
            var attendance = StaffAttendance.CheckIn(staff.Id, staff.PropertyId, req.CardUid, req.ReaderId, req.ShiftType, req.ScheduledHours, staff.HourlyRate ?? 0);
            await db.StaffAttendances.AddAsync(attendance);
            await db.SaveChangesAsync();
            return Results.Ok(new { attendance.Id, Status = "Checked In", attendance.CheckInTime, StaffName = staff.FullName });
        })
            .WithTags("Attendance").RequireAuthorization()
            .WithName("AttendanceRfidCheckIn").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/attendance/rfid/check-out", async (CompatRfidCheckOutRequest req, ApplicationDbContext db) =>
        {
            var card = await db.RfidCards.FirstOrDefaultAsync(c => c.CardUid == req.CardUid && c.Status == RfidCardStatus.Active);
            if (card is null) return Results.NotFound(new { Error = "No active RFID card found." });
            var attendance = await db.StaffAttendances
                .Where(a => a.StaffId == card.StaffId && a.Status == AttendanceStatus.CheckedIn)
                .OrderByDescending(a => a.CheckInTime).FirstOrDefaultAsync();
            if (attendance is null) return Results.NotFound(new { Error = "No active check-in found." });
            attendance.CheckOut(req.ReaderId);
            await db.SaveChangesAsync();
            return Results.Ok(new { attendance.Id, Status = "Checked Out", attendance.CheckOutTime, attendance.ActualHours, attendance.TotalWage });
        })
            .WithTags("Attendance").RequireAuthorization()
            .WithName("AttendanceRfidCheckOut").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/attendance/manual/check-in", async (ManualCheckInRequest req, HttpContext context, ApplicationDbContext db) =>
        {
            var staff = await db.StaffMembers.FindAsync(req.StaffId);
            if (staff is null) return Results.NotFound(new { Error = "Staff member not found." });
            var today = DateTime.UtcNow.Date;
            var existing = await db.StaffAttendances.FirstOrDefaultAsync(a => a.StaffId == staff.Id && a.CheckInTime.Date == today && a.Status == AttendanceStatus.CheckedIn);
            if (existing is not null) return Results.Conflict(new { Error = "Already checked in", AttendanceId = existing.Id });
            var attendance = StaffAttendance.CheckIn(staff.Id, staff.PropertyId, "MANUAL", null, req.ShiftType, req.ScheduledHours, staff.HourlyRate ?? 0);
            await db.StaffAttendances.AddAsync(attendance);
            await db.SaveChangesAsync();
            return Results.Ok(new { attendance.Id, Status = "Checked In", attendance.CheckInTime, StaffName = staff.FullName });
        })
            .WithTags("Attendance").RequireAuthorization()
            .WithName("AttendanceManualCheckIn").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/attendance/manual/check-out", async (ManualCheckOutRequest req, ApplicationDbContext db) =>
        {
            var attendance = await db.StaffAttendances
                .Where(a => a.StaffId == req.StaffId && a.Status == AttendanceStatus.CheckedIn)
                .OrderByDescending(a => a.CheckInTime).FirstOrDefaultAsync();
            if (attendance is null) return Results.NotFound(new { Error = "No active check-in found." });
            attendance.CheckOut(null);
            await db.SaveChangesAsync();
            return Results.Ok(new { attendance.Id, Status = "Checked Out", attendance.CheckOutTime, attendance.ActualHours, attendance.TotalWage });
        })
            .WithTags("Attendance").RequireAuthorization()
            .WithName("AttendanceManualCheckOut").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/attendance/{id:guid}/break/start", async (Guid id, ApplicationDbContext db) =>
        {
            var attendance = await db.StaffAttendances.FindAsync(id);
            if (attendance is null) return Results.NotFound();
            attendance.StartBreak();
            await db.SaveChangesAsync();
            return Results.Ok(new { attendance.Id, Status = "Break Started", attendance.BreakStart });
        })
            .WithTags("Attendance").RequireAuthorization()
            .WithName("AttendanceBreakStart").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/attendance/{id:guid}/break/end", async (Guid id, ApplicationDbContext db) =>
        {
            var attendance = await db.StaffAttendances.FindAsync(id);
            if (attendance is null) return Results.NotFound();
            attendance.EndBreak();
            await db.SaveChangesAsync();
            return Results.Ok(new { attendance.Id, Status = "Break Ended", attendance.BreakEnd, attendance.BreakDuration });
        })
            .WithTags("Attendance").RequireAuthorization()
            .WithName("AttendanceBreakEnd").WithOpenApi().ExcludeFromDescription();

        // Maintenance — real queries
        app.MapGet("/api/maintenance", async (HttpContext context, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var propertyIdClaim = context.User.FindFirstValue("propertyId");
            if (!Guid.TryParse(propertyIdClaim, out var propertyId)) return Results.Unauthorized();
            var query = db.MaintenanceTasks
                .Where(m => m.PropertyId == propertyId)
                .AsNoTracking()
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id, m.Title, m.Description,
                    Category = m.Category.ToString(), Priority = m.Priority.ToString(),
                    Status = m.Status.ToString(), m.RoomId, m.AssignedToStaffId,
                    m.ScheduledDate, m.StartedAt, m.CompletedAt,
                    m.EstimatedCost, m.ActualCost, m.VendorName, m.IsRecurring, m.CreatedAt
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
            .WithTags("Maintenance").RequireAuthorization()
            .WithName("GetMaintenanceTasks").WithOpenApi().ExcludeFromDescription();

        app.MapPost("/api/maintenance", async (CreateMaintenanceTaskRequest req, HttpContext context, ApplicationDbContext db) =>
        {
            var userId = GetUserId(context);
            var task = MaintenanceTask.Create(
                req.PropertyId, req.Title, req.Description,
                req.Category, req.Priority, req.RoomId, userId);
            await db.MaintenanceTasks.AddAsync(task);
            await db.SaveChangesAsync();
            return Results.Created($"/api/maintenance/{task.Id}",
                new { task.Id, Status = task.Status.ToString(), task.CreatedAt });
        })
            .WithTags("Maintenance").RequireAuthorization()
            .WithName("CreateMaintenanceTask").WithOpenApi().ExcludeFromDescription();
    }

    private static Guid? GetUserId(HttpContext context)
    {
        var sub = context.User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                  ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

// â”€â”€â”€ DTO records for compatibility endpoints â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
public record RoomStatusUpdateRequest(string Status);
public record HkStatusUpdateRequest(string? HkStatus);
public record AssignHousekeepingRequest(Guid StaffId);
public record CreateHousekeepingTaskRequest(Guid PropertyId, Guid RoomId, HousekeepingTaskType TaskType, HousekeepingPriority Priority);
public record CompleteHousekeepingRequest(bool? LinenChanged, bool? BathroomCleaned, bool? FloorsCleaned, bool? MinibarRestocked, bool? AmenitiesReplenished);
public record InspectHousekeepingRequest(Guid? InspectorId, bool? Passed, string? Notes);
public record CreateOvertimeRequestCompat(Guid PropertyId, Guid StaffMemberId, DateTime RequestedDate, decimal RequestedHours, string Reason);
public record AdminRegisterCompatRequest(Guid PropertyId, string Email, string Password, string FirstName, string LastName, string? Phone, string? Role);
public record UpdateRolePermissionsRequest(List<string> Permissions);
public record CreateEmailTemplateCompatRequest(Guid PropertyId, string Name, NotificationType Type, string Subject, string HtmlBody);
public record UpdateEmailTemplateCompatRequest(string? Name, string Subject, string HtmlBody);

// — New production DTO records —
public record SubmitFeedbackRequest(
    Guid PropertyId, decimal OverallRating, string? Comment,
    Guid? GuestId = null, Guid? BookingId = null,
    decimal? CleanlinessRating = null, decimal? ServiceRating = null,
    decimal? LocationRating = null, decimal? ValueRating = null,
    decimal? AmenitiesRating = null);

public record CreateServiceRequestCompat(
    Guid PropertyId, ServiceRequestType RequestType, string Description,
    Guid? GuestId = null, Guid? BookingId = null, Guid? RoomId = null,
    ServiceRequestPriority Priority = ServiceRequestPriority.Normal);

public record CompatRfidCheckInRequest(
    string CardUid, Guid? ReaderId = null,
    ShiftType ShiftType = ShiftType.Morning, decimal ScheduledHours = 8);

public record CompatRfidCheckOutRequest(string CardUid, Guid? ReaderId = null);

public record ManualCheckInRequest(
    Guid StaffId, ShiftType ShiftType = ShiftType.Morning, decimal ScheduledHours = 8);

public record ManualCheckOutRequest(Guid StaffId);

public record CreateMaintenanceTaskRequest(
    Guid PropertyId, string Title, string Description,
    MaintenanceCategory Category, MaintenancePriority Priority = MaintenancePriority.Medium,
    Guid? RoomId = null);
