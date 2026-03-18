using MediatR;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Shared.ValueObjects;

namespace SAFARIstack.Core.Application.Bookings.Commands;

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, CreateBookingResult>
{
    private readonly IUnitOfWork _uow;

    public CreateBookingCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<CreateBookingResult> Handle(
        CreateBookingCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var bookingReference = GenerateBookingReference();

            var booking = Booking.Create(
                request.PropertyId,
                request.GuestId,
                bookingReference,
                request.CheckInDate,
                request.CheckOutDate,
                request.AdultCount,
                request.ChildCount,
                request.CreatedByUserId);

            decimal subtotal = 0;
            foreach (var room in request.Rooms)
            {
                var bookingRoom = BookingRoom.Create(
                    booking.Id,
                    room.RoomId,
                    room.RoomTypeId,
                    room.RateApplied);

                booking.AddRoom(bookingRoom);
                subtotal += room.RateApplied;
            }

            var subtotalMoney = Money.FromZAR(subtotal);
            booking.CalculateFinancials(subtotalMoney);

            await _uow.Bookings.AddAsync(booking, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return new CreateBookingResult(
                booking.Id,
                bookingReference,
                booking.TotalAmount,
                true);
        }
        catch (Exception ex)
        {
            return new CreateBookingResult(
                Guid.Empty,
                string.Empty,
                0,
                false,
                ex.Message);
        }
    }

    private string GenerateBookingReference()
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = new Random().Next(1000, 9999);
        return $"BK-{datePart}-{randomPart}";
    }
}
