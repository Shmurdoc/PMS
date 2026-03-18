using Microsoft.EntityFrameworkCore;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Modules.Staff.Domain.Entities;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// RFID Card &amp; Reader Management endpoints (admin-facing CRUD)
/// </summary>
public static class RfidManagementEndpoints
{
    public static void MapRfidManagementEndpoints(this WebApplication app)
    {
        // ═══════════════════════════════════════════════════════════════
        //  RFID CARDS — Issue / Deactivate / Lost / Stolen / Query
        // ═══════════════════════════════════════════════════════════════
        var cards = app.MapGroup("/api/rfid/cards")
            .WithTags("RFID Card Management")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        // List all cards for a property
        cards.MapGet("/{propertyId:guid}", async (
            Guid propertyId, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var query = db.RfidCards
                .Where(c => c.PropertyId == propertyId)
                .Join(db.StaffMembers, c => c.StaffId, s => s.Id, (c, s) => new
                {
                    c.Id,
                    c.StaffId,
                    StaffName = s.FirstName + " " + s.LastName,
                    StaffRole = s.Role.ToString(),
                    c.CardUid,
                    CardType = c.CardType.ToString(),
                    Status = c.Status.ToString(),
                    c.IssueDate,
                    c.ExpiryDate,
                    c.LastUsedAt,
                    c.Notes
                })
                .OrderByDescending(c => c.IssueDate);

            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 50));
        })
        .WithName("GetRfidCards")
        .WithOpenApi();

        // Get a single card by ID
        cards.MapGet("/detail/{cardId:guid}", async (Guid cardId, ApplicationDbContext db) =>
        {
            var card = await db.RfidCards
                .Where(c => c.Id == cardId)
                .Join(db.StaffMembers, c => c.StaffId, s => s.Id, (c, s) => new
                {
                    c.Id,
                    c.StaffId,
                    StaffName = s.FirstName + " " + s.LastName,
                    StaffRole = s.Role.ToString(),
                    c.CardUid,
                    CardType = c.CardType.ToString(),
                    Status = c.Status.ToString(),
                    c.IssueDate,
                    c.ExpiryDate,
                    c.LastUsedAt,
                    c.Notes
                })
                .FirstOrDefaultAsync();

            return card is null ? Results.NotFound() : Results.Ok(card);
        })
        .WithName("GetRfidCardById")
        .WithOpenApi();

        // Issue a new RFID card
        cards.MapPost("/", async (IssueRfidCardRequest request, ApplicationDbContext db) =>
        {
            var staff = await db.StaffMembers.FindAsync(request.StaffId);
            if (staff is null) return Results.NotFound("Staff member not found.");

            // Check for duplicate UID
            var existing = await db.RfidCards
                .AnyAsync(c => c.CardUid == request.CardUid.ToUpperInvariant());
            if (existing) return Results.Conflict("A card with this UID already exists.");

            var cardType = Enum.Parse<RfidCardType>(request.CardType, true);
            var card = RfidCard.Create(request.StaffId, request.CardUid, cardType, request.PropertyId);

            db.RfidCards.Add(card);
            await db.SaveChangesAsync();

            return Results.Created($"/api/rfid/cards/detail/{card.Id}", new
            {
                card.Id,
                card.StaffId,
                StaffName = staff.FullName,
                card.CardUid,
                CardType = card.CardType.ToString(),
                Status = card.Status.ToString(),
                card.IssueDate
            });
        })
        .WithName("IssueRfidCard")
        .WithOpenApi();

        // Deactivate a card
        cards.MapPost("/{cardId:guid}/deactivate", async (
            Guid cardId, DeactivateCardRequest request, ApplicationDbContext db) =>
        {
            var card = await db.RfidCards.FindAsync(cardId);
            if (card is null) return Results.NotFound();

            card.Deactivate(request.Reason);
            await db.SaveChangesAsync();

            return Results.Ok(new { card.Id, Status = card.Status.ToString(), Message = "Card deactivated." });
        })
        .WithName("DeactivateRfidCard")
        .WithOpenApi();

        // Report card lost
        cards.MapPost("/{cardId:guid}/report-lost", async (Guid cardId, ApplicationDbContext db) =>
        {
            var card = await db.RfidCards.FindAsync(cardId);
            if (card is null) return Results.NotFound();

            card.ReportLost();
            await db.SaveChangesAsync();

            return Results.Ok(new { card.Id, Status = card.Status.ToString(), Message = "Card reported as lost." });
        })
        .WithName("ReportRfidCardLost")
        .WithOpenApi();

        // Report card stolen
        cards.MapPost("/{cardId:guid}/report-stolen", async (Guid cardId, ApplicationDbContext db) =>
        {
            var card = await db.RfidCards.FindAsync(cardId);
            if (card is null) return Results.NotFound();

            card.ReportStolen();
            await db.SaveChangesAsync();

            return Results.Ok(new { card.Id, Status = card.Status.ToString(), Message = "Card reported as stolen." });
        })
        .WithName("ReportRfidCardStolen")
        .WithOpenApi();

        // Delete card (hard delete — use deactivate for soft)
        cards.MapDelete("/{cardId:guid}", async (Guid cardId, ApplicationDbContext db) =>
        {
            var card = await db.RfidCards.FindAsync(cardId);
            if (card is null) return Results.NotFound();

            db.RfidCards.Remove(card);
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "Card deleted." });
        })
        .WithName("DeleteRfidCard")
        .WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  RFID READERS — Register / Update / Monitor
        // ═══════════════════════════════════════════════════════════════
        var readers = app.MapGroup("/api/rfid/readers")
            .WithTags("RFID Reader Management")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        // List all readers for a property
        readers.MapGet("/{propertyId:guid}", async (
            Guid propertyId, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var query = db.RfidReaders
                .Where(r => r.PropertyId == propertyId)
                .Select(r => new
                {
                    r.Id,
                    r.ReaderSerial,
                    r.ReaderName,
                    ReaderType = r.ReaderType.ToString(),
                    r.LocationDescription,
                    r.IpAddress,
                    r.MacAddress,
                    r.LastSeenAt,
                    Status = r.Status.ToString(),
                    IsOnline = r.LastSeenAt.HasValue &&
                        r.LastSeenAt.Value > DateTime.UtcNow.AddMinutes(-2)
                })
                .OrderBy(r => r.ReaderName);

            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 50));
        })
        .WithName("GetRfidReaders")
        .WithOpenApi();

        // Register a new reader
        readers.MapPost("/", async (RegisterReaderRequest request, ApplicationDbContext db) =>
        {
            var existing = await db.RfidReaders
                .AnyAsync(r => r.ReaderSerial == request.ReaderSerial);
            if (existing) return Results.Conflict("Reader with this serial already exists.");

            var readerType = Enum.Parse<RfidReaderType>(request.ReaderType, true);
            var reader = RfidReader.Create(
                request.PropertyId,
                request.ReaderSerial,
                request.ReaderName,
                readerType,
                Guid.NewGuid().ToString("N")); // Generate API key

            db.RfidReaders.Add(reader);
            await db.SaveChangesAsync();

            return Results.Created($"/api/rfid/readers/detail/{reader.Id}", new
            {
                reader.Id,
                reader.ReaderSerial,
                reader.ReaderName,
                ReaderType = reader.ReaderType.ToString(),
                reader.ApiKey, // Return once — client must store this
                Status = reader.Status.ToString()
            });
        })
        .WithName("RegisterRfidReader")
        .WithOpenApi();

        // Get reader detail
        readers.MapGet("/detail/{readerId:guid}", async (Guid readerId, ApplicationDbContext db) =>
        {
            var reader = await db.RfidReaders
                .Where(r => r.Id == readerId)
                .Select(r => new
                {
                    r.Id,
                    r.ReaderSerial,
                    r.ReaderName,
                    ReaderType = r.ReaderType.ToString(),
                    r.LocationDescription,
                    r.IpAddress,
                    r.MacAddress,
                    r.LastSeenAt,
                    Status = r.Status.ToString(),
                    IsOnline = r.LastSeenAt.HasValue &&
                        r.LastSeenAt.Value > DateTime.UtcNow.AddMinutes(-2)
                })
                .FirstOrDefaultAsync();

            return reader is null ? Results.NotFound() : Results.Ok(reader);
        })
        .WithName("GetRfidReaderById")
        .WithOpenApi();

        // Mark reader offline (manual override)
        readers.MapPost("/{readerId:guid}/offline", async (Guid readerId, ApplicationDbContext db) =>
        {
            var reader = await db.RfidReaders.FindAsync(readerId);
            if (reader is null) return Results.NotFound();

            reader.MarkOffline();
            await db.SaveChangesAsync();

            return Results.Ok(new { reader.Id, Status = reader.Status.ToString() });
        })
        .WithName("MarkReaderOffline")
        .WithOpenApi();

        // Delete reader
        readers.MapDelete("/{readerId:guid}", async (Guid readerId, ApplicationDbContext db) =>
        {
            var reader = await db.RfidReaders.FindAsync(readerId);
            if (reader is null) return Results.NotFound();

            db.RfidReaders.Remove(reader);
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "Reader deleted." });
        })
        .WithName("DeleteRfidReader")
        .WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  ACCESS LOGS — Security & Audit Trail
        // ═══════════════════════════════════════════════════════════════
        var logs = app.MapGroup("/api/rfid/access-logs")
            .WithTags("RFID Access Logs")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        // Query access logs
        logs.MapGet("/{propertyId:guid}", async (
            Guid propertyId,
            DateTime? from,
            DateTime? to,
            string? status,
            int? page,
            int? pageSize,
            ApplicationDbContext db) =>
        {
            var fromDate = from ?? DateTime.UtcNow.Date;
            var toDate = to ?? DateTime.UtcNow;

            var query = db.StaffAttendances
                .Where(a => a.PropertyId == propertyId
                    && a.CheckInTime >= fromDate
                    && a.CheckInTime <= toDate)
                .Join(db.StaffMembers, a => a.StaffId, s => s.Id, (a, s) => new
                {
                    a.Id,
                    a.StaffId,
                    StaffName = s.FirstName + " " + s.LastName,
                    StaffRole = s.Role.ToString(),
                    a.CardUid,
                    a.ReaderId,
                    AccessTime = a.CheckInTime,
                    AccessType = "granted",
                    a.LocationType,
                    Status = a.Status.ToString()
                })
                .OrderByDescending(a => a.AccessTime);

            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 100));
        })
        .WithName("GetAccessLogs")
        .WithOpenApi();
    }
}

// ─── Request DTOs ────────────────────────────────────────────────
public record IssueRfidCardRequest(Guid PropertyId, Guid StaffId, string CardUid, string CardType, string? Notes);
public record DeactivateCardRequest(string Reason);
public record RegisterReaderRequest(Guid PropertyId, string ReaderSerial, string ReaderName, string ReaderType, string? LocationDescription, string? IpAddress);
