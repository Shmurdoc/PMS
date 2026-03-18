using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

public static class HousekeepingEndpoints
{
    public static void MapHousekeepingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/housekeeping")
            .WithTags("Housekeeping")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        group.MapGet("/tasks/{propertyId:guid}", async (
            Guid propertyId, DateTime? date, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var targetDate = (date ?? DateTime.UtcNow).Date;
            var query = db.HousekeepingTasks
                .Include(t => t.Room)
                .Where(t => t.PropertyId == propertyId && t.ScheduledDate.Date == targetDate &&
                            (t.Status == HousekeepingTaskStatus.Pending || t.Status == HousekeepingTaskStatus.Assigned))
                .AsNoTracking()
                .OrderByDescending(t => t.Priority)
                .Select(t => new
                {
                    t.Id, t.RoomId, RoomNumber = t.Room.RoomNumber,
                    t.TaskType, t.Priority, t.Status,
                    t.ScheduledDate, t.AssignedToStaffId
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithName("GetPendingTasks").WithOpenApi();

        group.MapGet("/staff/{staffId:guid}", async (
            Guid staffId, DateTime? date, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var targetDate = (date ?? DateTime.UtcNow).Date;
            var query = db.HousekeepingTasks
                .Include(t => t.Room)
                .Where(t => t.AssignedToStaffId == staffId && t.ScheduledDate.Date == targetDate)
                .AsNoTracking()
                .OrderByDescending(t => t.Priority)
                .Select(t => new
                {
                    t.Id, t.RoomId, RoomNumber = t.Room.RoomNumber,
                    t.TaskType, t.Priority, t.Status
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithName("GetStaffTasks").WithOpenApi();

        group.MapPost("/", async (CreateHkTaskRequest req, IUnitOfWork uow) =>
        {
            var task = HousekeepingTask.Create(req.PropertyId, req.RoomId, req.TaskType, req.ScheduledDate, req.Priority);
            if (req.AssignedToStaffId.HasValue)
                task.AssignTo(req.AssignedToStaffId.Value);

            await uow.Housekeeping.AddAsync(task);
            await uow.SaveChangesAsync();
            return Results.Created($"/api/housekeeping/{task.Id}", new { task.Id });
        })
        .WithName("CreateHousekeepingTask").WithOpenApi();

        group.MapPost("/{taskId:guid}/assign", async (Guid taskId, Guid staffId, IUnitOfWork uow) =>
        {
            var task = await uow.Housekeeping.GetByIdAsync(taskId);
            if (task is null) return Results.NotFound();
            task.AssignTo(staffId);
            uow.Housekeeping.Update(task);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("AssignTask").WithOpenApi();

        group.MapPost("/{taskId:guid}/start", async (Guid taskId, IUnitOfWork uow) =>
        {
            var task = await uow.Housekeeping.GetByIdAsync(taskId);
            if (task is null) return Results.NotFound();
            task.Start();
            uow.Housekeeping.Update(task);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("StartTask").WithOpenApi();

        group.MapPost("/{taskId:guid}/complete", async (Guid taskId, CompleteTaskRequest req, IUnitOfWork uow) =>
        {
            var task = await uow.Housekeeping.GetByIdAsync(taskId);
            if (task is null) return Results.NotFound();
            task.Complete(req.LinenChanged, req.BathroomCleaned, req.FloorsCleaned,
                          req.MinibarRestocked, req.AmenitiesReplenished);

            // Also update room HK status
            var room = await uow.Rooms.GetByIdAsync(task.RoomId);
            room?.MarkClean(DateTime.UtcNow);
            if (room is not null) uow.Rooms.Update(room);

            uow.Housekeeping.Update(task);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("CompleteTask").WithOpenApi();

        group.MapPost("/{taskId:guid}/inspect", async (Guid taskId, InspectTaskRequest req, IUnitOfWork uow) =>
        {
            var task = await uow.Housekeeping.GetByIdAsync(taskId);
            if (task is null) return Results.NotFound();
            task.Inspect(req.InspectorStaffId, req.Passed, req.Notes);
            uow.Housekeeping.Update(task);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("InspectTask").WithOpenApi();
    }
}

public record CreateHkTaskRequest(Guid PropertyId, Guid RoomId, HousekeepingTaskType TaskType,
    DateTime ScheduledDate, HousekeepingPriority Priority = HousekeepingPriority.Normal,
    Guid? AssignedToStaffId = null);
public record CompleteTaskRequest(bool LinenChanged, bool BathroomCleaned, bool FloorsCleaned,
    bool MinibarRestocked, bool AmenitiesReplenished);
public record InspectTaskRequest(Guid InspectorStaffId, bool Passed, string? Notes);
