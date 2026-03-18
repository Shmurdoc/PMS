using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Application.Bookings.Commands;
using SAFARIstack.Core.Application.Bookings.Queries;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.API.Endpoints;

public static class BookingEndpoints
{
    public static void MapBookingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/bookings")
            .WithTags("Bookings")
            .RequireAuthorization()
            .RequireTenantValidation()
            .WithAutoValidation();

        // Create booking
        group.MapPost("/", async (CreateBookingCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.Success
                ? Results.Created($"/api/bookings/{result.BookingId}", result)
                : Results.BadRequest(result);
        })
        .WithName("CreateBooking")
        .WithOpenApi()
        .Produces<CreateBookingResult>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);

        // Get booking by ID
        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var query = new GetBookingByIdQuery(id);
            var result = await mediator.Send(query);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetBookingById")
        .WithOpenApi()
        .Produces<BookingDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Get bookings by property — SERVER-SIDE PAGINATION
        group.MapGet("/property/{propertyId:guid}", async (
            Guid propertyId, DateTime? from, DateTime? to, int? page, int? pageSize, ApplicationDbContext db) =>
        {
            var query = db.Bookings
                .Where(b => b.PropertyId == propertyId)
                .AsNoTracking();

            if (from.HasValue) query = query.Where(b => b.CheckOutDate >= from.Value);
            if (to.HasValue) query = query.Where(b => b.CheckInDate <= to.Value);

            var projected = query
                .OrderByDescending(b => b.CheckInDate)
                .Select(b => new
                {
                    b.Id, b.BookingReference, b.Status, b.Source,
                    b.CheckInDate, b.CheckOutDate, b.Nights,
                    b.AdultCount, b.ChildCount,
                    b.TotalAmount, b.PaidAmount, b.OutstandingAmount,
                    GuestName = b.Guest != null ? b.Guest.FirstName + " " + b.Guest.LastName : null
                });

            return Results.Ok(await PaginationHelpers.PaginateAsync(projected, page ?? 1, pageSize ?? 25));
        })
        .WithName("GetBookingsByProperty")
        .WithOpenApi();

        // Check-in booking
        group.MapPost("/{id:guid}/check-in", async (Guid id, CheckInRequest req, IUnitOfWork uow) =>
        {
            var booking = await uow.Bookings.GetByIdAsync(id);
            if (booking is null) return Results.NotFound();

            booking.CheckIn(req.UserId);
            uow.Bookings.Update(booking);
            await uow.SaveChangesAsync();
            return Results.Ok(new { booking.Id, booking.BookingReference, booking.Status, booking.ActualCheckInTime });
        })
        .WithName("CheckInBooking")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Check-out booking
        group.MapPost("/{id:guid}/check-out", async (Guid id, CheckOutRequest req, IUnitOfWork uow) =>
        {
            var booking = await uow.Bookings.GetByIdAsync(id);
            if (booking is null) return Results.NotFound();

            booking.CheckOut(req.UserId);
            uow.Bookings.Update(booking);
            await uow.SaveChangesAsync();
            return Results.Ok(new { booking.Id, booking.BookingReference, booking.Status, booking.ActualCheckOutTime });
        })
        .WithName("CheckOutBooking")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}

// ─── Request DTOs ────────────────────────────────────────────────────
public record CheckInRequest(Guid UserId);
public record CheckOutRequest(Guid UserId);
