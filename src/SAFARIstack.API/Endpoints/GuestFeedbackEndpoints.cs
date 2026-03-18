using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// Guest feedback and review endpoints.
/// Allows guests to submit feedback, staff to manage responses.
/// </summary>
public static class GuestFeedbackEndpoints
{
    public static void MapGuestFeedbackEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/feedback")
            .WithTags("Guests: Feedback");

        // POST /api/feedback — Submit anonymous guest feedback
        group.MapPost("", async (
            GuestFeedbackRequest req,
            Guid propertyId,
            ApplicationDbContext db) =>
        {
            if (req.Rating < 1 || req.Rating > 5)
                return Results.BadRequest("Rating must be between 1 and 5.");

            var feedback = GuestFeedback.Create(
                propertyId,
                req.GuestId ?? Guid.Empty,
                req.Rating,
                req.Comment,
                req.BookingId,
                req.GuestName,
                req.GuestEmail);

            await db.GuestFeedbacks.AddAsync(feedback);
            await db.SaveChangesAsync();

            return Results.Created($"/api/feedback/{feedback.Id}", new
            {
                feedback.Id,
                Message = "Thank you for your feedback!",
                Status = feedback.Status.ToString()
            });
        })
        .AllowAnonymous()
        .WithName("SubmitFeedback")
        .WithOpenApi();

        // Management endpoints (require authorization)
        var managementGroup = app.MapGroup("/api/feedback")
            .WithTags("Guests: Feedback Management")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        // GET /api/feedback — List feedback
        managementGroup.MapGet("", async (
            Guid propertyId,
            string? status,
            bool? requiresAction,
            int? page,
            int? pageSize,
            ApplicationDbContext db) =>
        {
            var query = db.GuestFeedbacks
                .Where(f => f.PropertyId == propertyId)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<FeedbackStatus>(status, out var parsedStatus))
                query = query.Where(f => f.Status == parsedStatus);

            if (requiresAction.HasValue)
                query = query.Where(f => f.RequiresAction == requiresAction.Value);

            var result = await PaginationHelpers.PaginateAsync(
                query
                    .OrderByDescending(f => f.SubmittedAt)
                    .Select(f => new
                    {
                        f.Id,
                        f.GuestName,
                        f.OverallRating,
                        Sentiment = f.Sentiment.ToString(),
                        Status = f.Status.ToString(),
                        f.RequiresAction,
                        f.SubmittedAt,
                        f.Comment
                    }),
                page ?? 1,
                pageSize ?? 50);

            return Results.Ok(result);
        })
        .WithName("ListFeedback")
        .WithOpenApi();

        // GET /api/feedback/{id} — Get single feedback
        managementGroup.MapGet("/{id:guid}", async (
            Guid id,
            Guid propertyId,
            ApplicationDbContext db) =>
        {
            var feedback = await db.GuestFeedbacks
                .Where(f => f.Id == id && f.PropertyId == propertyId)
                .AsNoTracking()
                .Select(f => new
                {
                    f.Id,
                    f.GuestName,
                    f.GuestEmail,
                    f.OverallRating,
                    f.RoomCleanliness,
                    f.RoomComfort,
                    f.FrontDeskService,
                    f.AmenityQuality,
                    f.ValueForMoney,
                    f.Comment,
                    Category = f.Category.ToString(),
                    Sentiment = f.Sentiment.ToString(),
                    Status = f.Status.ToString(),
                    f.RequiresAction,
                    f.ManagerResponse,
                    f.SubmittedAt,
                    f.ResponseDate,
                    f.IsPublished
                })
                .FirstOrDefaultAsync();

            if (feedback is null)
                return Results.NotFound();

            return Results.Ok(feedback);
        })
        .WithName("GetFeedback")
        .WithOpenApi();

        // POST /api/feedback/{id}/respond — Add manager response
        managementGroup.MapPost("/{id:guid}/respond", async (
            Guid id,
            RespondFeedbackRequest req,
            Guid propertyId,
            Guid userId,
            ApplicationDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Response))
                return Results.BadRequest("Response cannot be empty.");

            var feedback = await db.GuestFeedbacks
                .FirstOrDefaultAsync(f => f.Id == id && f.PropertyId == propertyId);

            if (feedback is null)
                return Results.NotFound();

            feedback.AddResponse(req.Response, userId);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                feedback.Id,
                Status = feedback.Status.ToString(),
                feedback.ResponseDate
            });
        })
        .WithName("RespondToFeedback")
        .WithOpenApi();

        // POST /api/feedback/{id}/publish — Publish feedback
        managementGroup.MapPost("/{id:guid}/publish", async (
            Guid id,
            Guid propertyId,
            ApplicationDbContext db) =>
        {
            var feedback = await db.GuestFeedbacks
                .FirstOrDefaultAsync(f => f.Id == id && f.PropertyId == propertyId);

            if (feedback is null)
                return Results.NotFound();

            feedback.Publish();
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                feedback.Id,
                IsPublished = true
            });
        })
        .WithName("PublishFeedback")
        .WithOpenApi();

        // POST /api/feedback/{id}/unpublish — Unpublish feedback
        managementGroup.MapPost("/{id:guid}/unpublish", async (
            Guid id,
            Guid propertyId,
            ApplicationDbContext db) =>
        {
            var feedback = await db.GuestFeedbacks
                .FirstOrDefaultAsync(f => f.Id == id && f.PropertyId == propertyId);

            if (feedback is null)
                return Results.NotFound();

            feedback.Unpublish();
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                feedback.Id,
                IsPublished = false
            });
        })
        .WithName("UnpublishFeedback")
        .WithOpenApi();

        // GET /api/feedback/analytics/summary — Get feedback analytics
        managementGroup.MapGet("/analytics/summary", async (
            Guid propertyId,
            ApplicationDbContext db) =>
        {
            var feedback = await db.GuestFeedbacks
                .Where(f => f.PropertyId == propertyId)
                .AsNoTracking()
                .ToListAsync();

            var total = feedback.Count;
            var avgRating = total > 0 ? feedback.Average(f => f.OverallRating) : 0;
            var negative = feedback.Count(f => f.Sentiment == FeedbackSentiment.Negative);
            var needsAction = feedback.Count(f => f.RequiresAction);

            return Results.Ok(new
            {
                TotalFeedback = total,
                AverageRating = Math.Round(avgRating, 2),
                NegativeFeedback = negative,
                ActionRequired = needsAction,
                RatingDistribution = new
                {
                    FiveStars = feedback.Count(f => f.OverallRating == 5),
                    FourStars = feedback.Count(f => f.OverallRating == 4),
                    ThreeStars = feedback.Count(f => f.OverallRating == 3),
                    TwoStars = feedback.Count(f => f.OverallRating == 2),
                    OneStar = feedback.Count(f => f.OverallRating == 1)
                },
                SentimentDistribution = new
                {
                    Positive = feedback.Count(f => f.Sentiment == FeedbackSentiment.Positive),
                    Neutral = feedback.Count(f => f.Sentiment == FeedbackSentiment.Neutral),
                    Negative = feedback.Count(f => f.Sentiment == FeedbackSentiment.Negative)
                }
            });
        })
        .WithName("GetFeedbackAnalytics")
        .WithOpenApi();
    }
}

public record GuestFeedbackRequest(
    int Rating,
    string? Comment = null,
    Guid? BookingId = null,
    Guid? GuestId = null,
    string? GuestName = null,
    string? GuestEmail = null);

public record RespondFeedbackRequest(
    string Response);
