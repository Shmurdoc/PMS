using MediatR;

namespace SAFARIstack.Core.Application.Bookings.Queries;

/// <summary>
/// Get booking by ID query
/// </summary>
public record GetBookingByIdQuery(Guid BookingId) : IRequest<BookingDto?>;

public record BookingDto(
    Guid Id,
    string BookingReference,
    Guid PropertyId,
    string PropertyName,
    Guid GuestId,
    string GuestName,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    string Status,
    int AdultCount,
    int ChildCount,
    decimal SubtotalAmount,
    decimal VATAmount,
    decimal TourismLevyAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount);
