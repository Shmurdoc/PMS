using FluentValidation;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Application.Bookings.Commands;

namespace SAFARIstack.Core.Application.Bookings;

// ═══════════════════════════════════════════════════════════════════════
//  FLUENT VALIDATORS — Only validators for LIVE commands/queries
// ═══════════════════════════════════════════════════════════════════════
public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.GuestId).NotEmpty();
        RuleFor(x => x.CheckInDate)
            .GreaterThan(DateTime.UtcNow.Date.AddDays(-1))
            .WithMessage("Check-in date cannot be in the past.");
        RuleFor(x => x.CheckOutDate)
            .GreaterThan(x => x.CheckInDate)
            .WithMessage("Check-out must be after check-in.");
        RuleFor(x => x.AdultCount).InclusiveBetween(1, 20);
        RuleFor(x => x.ChildCount).InclusiveBetween(0, 20);
    }
}
