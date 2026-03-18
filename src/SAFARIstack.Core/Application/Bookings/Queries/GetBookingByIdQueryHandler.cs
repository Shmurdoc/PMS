using MediatR;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;

namespace SAFARIstack.Core.Application.Bookings.Queries;

public class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, BookingDto?>
{
    private readonly IUnitOfWork _uow;

    public GetBookingByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<BookingDto?> Handle(
        GetBookingByIdQuery request,
        CancellationToken cancellationToken)
    {
        var booking = await _uow.Bookings.GetByIdAsync(request.BookingId, cancellationToken);

        if (booking == null)
            return null;

        return new BookingDto(
            booking.Id,
            booking.BookingReference,
            booking.PropertyId,
            booking.Property?.Name ?? string.Empty,
            booking.GuestId,
            booking.Guest?.FullName ?? string.Empty,
            booking.CheckInDate,
            booking.CheckOutDate,
            booking.Status.ToString(),
            booking.AdultCount,
            booking.ChildCount,
            booking.SubtotalAmount,
            booking.VATAmount,
            booking.TourismLevyAmount,
            booking.TotalAmount,
            booking.PaidAmount,
            booking.OutstandingAmount);
    }
}
