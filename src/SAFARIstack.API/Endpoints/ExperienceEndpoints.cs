using Microsoft.AspNetCore.Mvc;
using SAFARIstack.Core.Domain.Interfaces;

namespace SAFARIstack.API.Endpoints;

public static class ExperienceEndpoints
{
    public static void MapExperienceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/experiences")
            .WithTags("Experiences")
            .RequireAuthorization();

        // Get available experiences for a date
        group.MapGet("/available/{propertyId:guid}", async (
            Guid propertyId,
            [FromQuery] DateTime date,
            [FromQuery] int participants,
            IExperienceBookingService svc) =>
        {
            var result = await svc.GetAvailableExperiencesAsync(propertyId, date, participants);
            return Results.Ok(result);
        })
        .WithName("GetAvailableExperiences")
        .WithOpenApi()
        .Produces<IEnumerable<ExperienceDto>>(StatusCodes.Status200OK);

        // Book an experience
        group.MapPost("/book", async (
            BookExperienceRequestDto request,
            IExperienceBookingService svc) =>
        {
            var result = await svc.BookExperienceAsync(request);
            return result.Success
                ? Results.Created($"/api/experiences/bookings/{result.ExperienceBookingId}", result)
                : Results.BadRequest(result);
        })
        .WithName("BookExperience")
        .WithOpenApi()
        .Produces<ExperienceBookingResultDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // Cancel experience booking
        group.MapPost("/bookings/{experienceBookingId:guid}/cancel", async (
            Guid experienceBookingId,
            ExperienceCancelRequest request,
            IExperienceBookingService svc) =>
        {
            var result = await svc.CancelExperienceBookingAsync(experienceBookingId, request.Reason);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("CancelExperienceBooking")
        .WithOpenApi()
        .Produces<ExperienceBookingResultDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // Record feedback
        group.MapPost("/bookings/{experienceBookingId:guid}/feedback", async (
            Guid experienceBookingId,
            ExperienceFeedbackRequest request,
            IExperienceBookingService svc) =>
        {
            await svc.RecordFeedbackAsync(experienceBookingId, request.Score, request.Notes);
            return Results.NoContent();
        })
        .WithName("RecordExperienceFeedback")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent);

        // Experience analytics
        group.MapGet("/analytics/{propertyId:guid}", async (
            Guid propertyId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            IExperienceBookingService svc) =>
        {
            var result = await svc.GetExperienceAnalyticsAsync(propertyId, startDate, endDate);
            return Results.Ok(result);
        })
        .WithName("GetExperienceAnalytics")
        .WithOpenApi()
        .RequireAuthorization("ManagerOrAbove")
        .Produces<ExperienceAnalyticsDto>(StatusCodes.Status200OK);

        // CRUD: Create experience (admin)
        group.MapPost("/", async (
            CreateExperienceRequest request,
            SAFARIstack.Infrastructure.Data.ApplicationDbContext db) =>
        {
            var experience = SAFARIstack.Core.Domain.Entities.Experience.Create(
                request.PropertyId, request.Name, request.Category,
                request.DurationMinutes, request.MaxGuests, request.BasePrice,
                request.Description, request.PricePerPerson,
                request.Location, request.DifficultyLevel, request.ImageUrl);

            if (request.IncludedItems != null || request.ExcludedItems != null)
                experience.SetIncludedExcluded(request.IncludedItems, request.ExcludedItems, request.WhatToBring);

            if (request.IsThirdParty && request.ThirdPartyOperator != null && request.CommissionRate.HasValue)
                experience.SetThirdParty(request.ThirdPartyOperator, request.CommissionRate.Value);

            await db.Set<SAFARIstack.Core.Domain.Entities.Experience>().AddAsync(experience);
            await db.SaveChangesAsync();

            return Results.Created($"/api/experiences/{experience.Id}", new { experience.Id, experience.Name });
        })
        .WithName("CreateExperience")
        .WithOpenApi()
        .RequireAuthorization("ManagerOrAbove")
        .Produces(StatusCodes.Status201Created);

        // List all experiences for property
        group.MapGet("/property/{propertyId:guid}", async (
            Guid propertyId,
            SAFARIstack.Infrastructure.Data.ApplicationDbContext db) =>
        {
            var experiences = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .ToListAsync(
                    db.Set<SAFARIstack.Core.Domain.Entities.Experience>()
                        .Where(e => e.PropertyId == propertyId)
                        .OrderBy(e => e.Name));

            return Results.Ok(experiences.Select(e => new ExperienceDto(
                e.Id, e.Name, e.Description,
                e.Category.ToString(), e.DurationMinutes,
                e.BasePrice, e.PricePerPerson,
                e.MaxGuests, e.ImageUrl,
                e.DifficultyLevel.ToString(), e.Location)));
        })
        .WithName("GetExperiencesByProperty")
        .WithOpenApi()
        .Produces<IEnumerable<ExperienceDto>>(StatusCodes.Status200OK);
    }
}

// ─── Request DTOs ────────────────────────────────────────────────────
public record ExperienceCancelRequest(string Reason);
public record ExperienceFeedbackRequest(int Score, string? Notes);

public record CreateExperienceRequest(
    Guid PropertyId,
    string Name,
    SAFARIstack.Core.Domain.Entities.ExperienceCategory Category,
    int DurationMinutes,
    int MaxGuests,
    decimal BasePrice,
    string? Description = null,
    bool PricePerPerson = true,
    string? Location = null,
    SAFARIstack.Core.Domain.Entities.DifficultyLevel DifficultyLevel = SAFARIstack.Core.Domain.Entities.DifficultyLevel.Easy,
    string? ImageUrl = null,
    string? IncludedItems = null,
    string? ExcludedItems = null,
    string? WhatToBring = null,
    bool IsThirdParty = false,
    string? ThirdPartyOperator = null,
    decimal? CommissionRate = null);
