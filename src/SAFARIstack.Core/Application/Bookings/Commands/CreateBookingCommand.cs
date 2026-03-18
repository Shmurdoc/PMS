using MediatR;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Core.Application.Bookings.Commands;

/// <summary>
/// Create booking command
/// </summary>
public record CreateBookingCommand(
    Guid PropertyId,
    Guid GuestId,
    DateTime CheckInDate,
    DateTime CheckOutDate,
    int AdultCount,
    int ChildCount,
    List<RoomBookingDto> Rooms,
    string? SpecialRequests,
    Guid? CreatedByUserId) : IRequest<CreateBookingResult>;

public record RoomBookingDto(Guid RoomId, Guid RoomTypeId, decimal RateApplied);

public record CreateBookingResult(
    Guid BookingId,
    string BookingReference,
    decimal TotalAmount,
    bool Success,
    string? ErrorMessage = null);
