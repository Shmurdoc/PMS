using FluentAssertions;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Shared.ValueObjects;

namespace SAFARIstack.Tests.Unit.Domain;

public class BookingTests
{
    private static readonly Guid PropertyId = Guid.NewGuid();
    private static readonly Guid GuestId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static Booking CreateValidBooking(
        DateTime? checkIn = null, DateTime? checkOut = null,
        BookingStatus? desiredStatus = null)
    {
        var booking = Booking.Create(
            PropertyId, GuestId, "BK-001",
            checkIn ?? DateTime.UtcNow.AddDays(1),
            checkOut ?? DateTime.UtcNow.AddDays(3),
            2, 0, UserId);

        if (desiredStatus == BookingStatus.CheckedIn)
            booking.CheckIn(UserId);
        else if (desiredStatus == BookingStatus.CheckedOut)
        {
            booking.CheckIn(UserId);
            booking.CheckOut(UserId);
        }

        return booking;
    }

    // ─── Create ──────────────────────────────────────────────────────
    [Fact]
    public void Create_ValidInput_SetsAllProperties()
    {
        var checkIn = DateTime.UtcNow.AddDays(1);
        var checkOut = DateTime.UtcNow.AddDays(4);
        var booking = Booking.Create(PropertyId, GuestId, "BK-TEST-001", checkIn, checkOut, 2, 1, UserId);

        booking.PropertyId.Should().Be(PropertyId);
        booking.GuestId.Should().Be(GuestId);
        booking.BookingReference.Should().Be("BK-TEST-001");
        booking.CheckInDate.Should().Be(checkIn);
        booking.CheckOutDate.Should().Be(checkOut);
        booking.AdultCount.Should().Be(2);
        booking.ChildCount.Should().Be(1);
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.Source.Should().Be(BookingSource.Direct);
        booking.Id.Should().NotBeEmpty();
        booking.CreatedByUserId.Should().Be(UserId);
    }

    [Fact]
    public void Create_RaisesBookingCreatedEvent()
    {
        var booking = CreateValidBooking();
        booking.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BookingCreatedEvent>()
            .Which.BookingReference.Should().Be("BK-001");
    }

    [Fact]
    public void Create_CheckOutBeforeCheckIn_Throws()
    {
        var act = () => Booking.Create(PropertyId, GuestId, "BK-ERR",
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(1), 1, 0, UserId);
        act.Should().Throw<ArgumentException>().WithMessage("*Check-out*after*");
    }

    [Fact]
    public void Create_SameDayCheckInAndOut_Throws()
    {
        var same = DateTime.UtcNow.AddDays(1);
        var act = () => Booking.Create(PropertyId, GuestId, "BK-ERR", same, same, 1, 0, UserId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithOptionalParams_SetsCorrectly()
    {
        var booking = Booking.Create(PropertyId, GuestId, "BK-OTA",
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(3),
            2, 0, UserId,
            ratePlanId: Guid.NewGuid(),
            source: BookingSource.BookingCom,
            externalReference: "EXT-123");

        booking.Source.Should().Be(BookingSource.BookingCom);
        booking.ExternalReference.Should().Be("EXT-123");
        booking.RatePlanId.Should().NotBeNull();
    }

    [Fact]
    public void Nights_Calculated_Correctly()
    {
        var booking = Booking.Create(PropertyId, GuestId, "BK-N",
            new DateTime(2026, 6, 1), new DateTime(2026, 6, 5), 2, 0, UserId);
        booking.Nights.Should().Be(4);
    }

    // ─── CheckIn ─────────────────────────────────────────────────────
    [Fact]
    public void CheckIn_FromConfirmed_ChangesStatusAndSetsFields()
    {
        var booking = CreateValidBooking();
        booking.ClearDomainEvents();

        booking.CheckIn(UserId);

        booking.Status.Should().Be(BookingStatus.CheckedIn);
        booking.CheckedInByUserId.Should().Be(UserId);
        booking.ActualCheckInTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        booking.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<BookingCheckedInEvent>();
    }

    [Theory]
    [InlineData(BookingStatus.CheckedIn)]
    [InlineData(BookingStatus.CheckedOut)]
    [InlineData(BookingStatus.Cancelled)]
    public void CheckIn_FromInvalidStatus_Throws(BookingStatus status)
    {
        var booking = CreateValidBooking();
        if (status == BookingStatus.CheckedIn) booking.CheckIn(UserId);
        if (status == BookingStatus.CheckedOut) { booking.CheckIn(UserId); booking.CheckOut(UserId); }
        if (status == BookingStatus.Cancelled) booking.Cancel(UserId, "test");

        var act = () => booking.CheckIn(UserId);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Cannot check in*");
    }

    // ─── CheckOut ────────────────────────────────────────────────────
    [Fact]
    public void CheckOut_FromCheckedIn_ChangesStatus()
    {
        var booking = CreateValidBooking(desiredStatus: BookingStatus.CheckedIn);
        booking.ClearDomainEvents();

        booking.CheckOut(UserId);

        booking.Status.Should().Be(BookingStatus.CheckedOut);
        booking.CheckedOutByUserId.Should().Be(UserId);
        booking.ActualCheckOutTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        booking.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<BookingCheckedOutEvent>();
    }

    [Fact]
    public void CheckOut_FromConfirmed_Throws()
    {
        var booking = CreateValidBooking();
        var act = () => booking.CheckOut(UserId);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Cannot check out*");
    }

    // ─── Cancel ──────────────────────────────────────────────────────
    [Fact]
    public void Cancel_FromConfirmed_SetsStatusAndReason()
    {
        var booking = CreateValidBooking();
        booking.ClearDomainEvents();

        booking.Cancel(UserId, "Guest requested");

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancelledByUserId.Should().Be(UserId);
        booking.CancellationReason.Should().Be("Guest requested");
        booking.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<BookingCancelledEvent>();
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var booking = CreateValidBooking();
        booking.Cancel(UserId, "first");
        var act = () => booking.Cancel(UserId, "second");
        act.Should().Throw<InvalidOperationException>().WithMessage("*already cancelled*");
    }

    // ─── NoShow ──────────────────────────────────────────────────────
    [Fact]
    public void MarkNoShow_FromConfirmed_ChangesStatus()
    {
        var booking = CreateValidBooking();
        booking.ClearDomainEvents();
        booking.MarkNoShow();

        booking.Status.Should().Be(BookingStatus.NoShow);
        booking.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<BookingNoShowEvent>();
    }

    [Fact]
    public void MarkNoShow_FromCheckedIn_Throws()
    {
        var booking = CreateValidBooking(desiredStatus: BookingStatus.CheckedIn);
        var act = () => booking.MarkNoShow();
        act.Should().Throw<InvalidOperationException>();
    }

    // ─── RecordPayment ───────────────────────────────────────────────
    [Fact]
    public void RecordPayment_IncrementsPaidAmount()
    {
        var booking = CreateValidBooking();
        booking.RecordPayment(500);
        booking.RecordPayment(300);
        booking.PaidAmount.Should().Be(800);
    }

    [Fact]
    public void RecordPayment_RaisesEvent()
    {
        var booking = CreateValidBooking();
        booking.ClearDomainEvents();
        booking.RecordPayment(100);
        booking.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<BookingPaymentReceivedEvent>();
    }

    // ─── OutstandingAmount ───────────────────────────────────────────
    [Fact]
    public void OutstandingAmount_Calculated_Correctly()
    {
        var booking = CreateValidBooking();
        booking.CalculateFinancials(Money.FromZAR(1000));
        booking.RecordPayment(500);
        booking.OutstandingAmount.Should().Be(booking.TotalAmount - 500);
    }

    // ─── CalculateFinancials ─────────────────────────────────────────
    [Fact]
    public void CalculateFinancials_SetsAllAmounts()
    {
        var booking = CreateValidBooking();
        booking.CalculateFinancials(Money.FromZAR(1000), Money.FromZAR(200), Money.FromZAR(50));

        booking.SubtotalAmount.Should().BeGreaterThan(0);
        booking.VATAmount.Should().BeGreaterThan(0);
        booking.TourismLevyAmount.Should().BeGreaterThan(0);
        booking.TotalAmount.Should().BeGreaterThan(booking.SubtotalAmount);
    }

    // ─── AddRoom ─────────────────────────────────────────────────────
    [Fact]
    public void AddRoom_IncreasesBookingRooms()
    {
        var booking = CreateValidBooking();
        var br = BookingRoom.Create(booking.Id, Guid.NewGuid(), Guid.NewGuid(), 1500);
        booking.AddRoom(br);
        booking.BookingRooms.Should().HaveCount(1);
    }

    // ─── Full Lifecycle ──────────────────────────────────────────────
    [Fact]
    public void FullLifecycle_Confirmed_CheckedIn_CheckedOut()
    {
        var booking = CreateValidBooking();
        booking.Status.Should().Be(BookingStatus.Confirmed);

        booking.CheckIn(UserId);
        booking.Status.Should().Be(BookingStatus.CheckedIn);

        booking.CheckOut(UserId);
        booking.Status.Should().Be(BookingStatus.CheckedOut);

        // Should have 3 domain events: Created, CheckedIn, CheckedOut
        booking.DomainEvents.Should().HaveCount(3);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAll()
    {
        var booking = CreateValidBooking();
        booking.DomainEvents.Should().NotBeEmpty();
        booking.ClearDomainEvents();
        booking.DomainEvents.Should().BeEmpty();
    }
}

public class BookingRoomTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var bookingId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var ratePlanId = Guid.NewGuid();

        var br = BookingRoom.Create(bookingId, roomId, roomTypeId, 1500, ratePlanId);

        br.BookingId.Should().Be(bookingId);
        br.RoomId.Should().Be(roomId);
        br.RoomTypeId.Should().Be(roomTypeId);
        br.RateApplied.Should().Be(1500);
        br.RatePlanId.Should().Be(ratePlanId);
    }
}
