using FluentAssertions;
using SAFARIstack.Modules.Staff.Domain.Entities;

namespace SAFARIstack.Tests.Unit.Domain;

public class StaffMemberTests
{
    private static readonly Guid PropertyId = Guid.NewGuid();

    [Fact]
    public void Create_SetsAllProperties()
    {
        var staff = StaffMember.Create(PropertyId, "staff@lodge.com", "Thabo", "Mokoena", StaffRole.Receptionist);

        staff.PropertyId.Should().Be(PropertyId);
        staff.Email.Should().Be("staff@lodge.com");
        staff.FirstName.Should().Be("Thabo");
        staff.LastName.Should().Be("Mokoena");
        staff.FullName.Should().Be("Thabo Mokoena");
        staff.Role.Should().Be(StaffRole.Receptionist);
        staff.IsActive.Should().BeTrue();
        staff.EmploymentType.Should().Be(EmploymentType.Permanent);
    }

    [Fact]
    public void Create_RaisesEvent()
    {
        var staff = StaffMember.Create(PropertyId, "s@s.com", "A", "B", StaffRole.Manager);
        staff.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StaffMemberCreatedEvent>();
    }

    [Fact]
    public void AssignRfidCard_IncreasesCards()
    {
        var staff = StaffMember.Create(PropertyId, "s@s.com", "A", "B", StaffRole.Security);
        var card = RfidCard.Create(staff.Id, "ABC123DEF456", RfidCardType.Card, PropertyId);
        staff.AssignRfidCard(card);
        staff.RfidCards.Should().HaveCount(1);
    }
}

public class StaffAttendanceTests
{
    private static readonly Guid StaffId = Guid.NewGuid();
    private static readonly Guid PropertyId = Guid.NewGuid();

    private static StaffAttendance CreateCheckedInAttendance() =>
        StaffAttendance.CheckIn(StaffId, PropertyId, "ABC123", Guid.NewGuid(), ShiftType.Morning, 9.0m, 50.0m);

    [Fact]
    public void CheckIn_SetsAllProperties()
    {
        var att = CreateCheckedInAttendance();

        att.StaffId.Should().Be(StaffId);
        att.PropertyId.Should().Be(PropertyId);
        att.CardUid.Should().Be("ABC123");
        att.ShiftType.Should().Be(ShiftType.Morning);
        att.ScheduledHours.Should().Be(9.0m);
        att.HourlyRate.Should().Be(50.0m);
        att.OvertimeRate.Should().Be(75.0m); // 1.5x SA law
        att.Status.Should().Be(AttendanceStatus.CheckedIn);
        att.CheckInTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        att.CheckOutTime.Should().BeNull();
    }

    [Fact]
    public void CheckIn_RaisesEvent()
    {
        var att = CreateCheckedInAttendance();
        att.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StaffCheckedInEvent>();
    }

    [Fact]
    public void CheckOut_SetsCheckOutAndCalculatesWages()
    {
        var att = CreateCheckedInAttendance();
        att.ClearDomainEvents();
        att.CheckOut(Guid.NewGuid());

        att.Status.Should().Be(AttendanceStatus.CheckedOut);
        att.CheckOutTime.Should().NotBeNull();
        att.ActualHours.Should().NotBeNull();
        att.TotalWage.Should().BeGreaterThanOrEqualTo(0);
        att.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StaffCheckedOutEvent>();
    }

    [Fact]
    public void CheckOut_AlreadyCheckedOut_Throws()
    {
        var att = CreateCheckedInAttendance();
        att.CheckOut(null);
        var act = () => att.CheckOut(null);
        act.Should().Throw<InvalidOperationException>().WithMessage("*already checked out*");
    }

    [Fact]
    public void StartBreak_SetsBreakStartAndStatus()
    {
        var att = CreateCheckedInAttendance();
        att.StartBreak();
        att.BreakStart.Should().NotBeNull();
        att.Status.Should().Be(AttendanceStatus.OnBreak);
    }

    [Fact]
    public void StartBreak_AlreadyOnBreak_Throws()
    {
        var att = CreateCheckedInAttendance();
        att.StartBreak();
        var act = () => att.StartBreak();
        act.Should().Throw<InvalidOperationException>().WithMessage("*already started*");
    }

    [Fact]
    public void EndBreak_CalculatesBreakDuration()
    {
        var att = CreateCheckedInAttendance();
        att.StartBreak();
        att.EndBreak();

        att.BreakEnd.Should().NotBeNull();
        att.BreakDuration.Should().BeGreaterThanOrEqualTo(0);
        att.Status.Should().Be(AttendanceStatus.CheckedIn);
    }

    [Fact]
    public void EndBreak_NoActiveBreak_Throws()
    {
        var att = CreateCheckedInAttendance();
        var act = () => att.EndBreak();
        act.Should().Throw<InvalidOperationException>().WithMessage("*No active break*");
    }

    [Fact]
    public void EndBreak_AlreadyEnded_Throws()
    {
        var att = CreateCheckedInAttendance();
        att.StartBreak();
        att.EndBreak();
        var act = () => att.EndBreak();
        act.Should().Throw<InvalidOperationException>().WithMessage("*already ended*");
    }

    [Fact]
    public void SetMobileCheckInLocation_SetsCoords()
    {
        var att = CreateCheckedInAttendance();
        att.SetMobileCheckInLocation(-25.7479m, 28.2293m);
        att.CheckInLatitude.Should().Be(-25.7479m);
        att.CheckInLongitude.Should().Be(28.2293m);
    }

    [Fact]
    public void SetMobileCheckOutLocation_SetsCoords()
    {
        var att = CreateCheckedInAttendance();
        att.SetMobileCheckOutLocation(-25.7479m, 28.2293m);
        att.CheckOutLatitude.Should().Be(-25.7479m);
        att.CheckOutLongitude.Should().Be(28.2293m);
    }
}

public class RfidCardTests
{
    private static readonly Guid StaffId = Guid.NewGuid();
    private static readonly Guid PropertyId = Guid.NewGuid();

    [Fact]
    public void Create_SetsAllProperties()
    {
        var card = RfidCard.Create(StaffId, "abc123def456", RfidCardType.Card, PropertyId);

        card.StaffId.Should().Be(StaffId);
        card.CardUid.Should().Be("ABC123DEF456"); // Uppercased
        card.CardType.Should().Be(RfidCardType.Card);
        card.PropertyId.Should().Be(PropertyId);
        card.Status.Should().Be(RfidCardStatus.Active);
        card.IssueDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_RaisesEvent()
    {
        var card = RfidCard.Create(StaffId, "ABC", RfidCardType.Wristband, PropertyId);
        card.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RfidCardIssuedEvent>();
    }

    [Fact]
    public void Deactivate_SetsStatusAndNotes()
    {
        var card = RfidCard.Create(StaffId, "ABC", RfidCardType.Card, PropertyId);
        card.Deactivate("Staff left");
        card.Status.Should().Be(RfidCardStatus.Deactivated);
        card.Notes.Should().Be("Staff left");
    }

    [Fact]
    public void ReportLost_SetsStatusAndRaisesEvent()
    {
        var card = RfidCard.Create(StaffId, "ABC", RfidCardType.Card, PropertyId);
        card.ClearDomainEvents();
        card.ReportLost();
        card.Status.Should().Be(RfidCardStatus.Lost);
        card.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RfidCardLostEvent>();
    }

    [Fact]
    public void ReportStolen_SetsStatusAndRaisesEvent()
    {
        var card = RfidCard.Create(StaffId, "ABC", RfidCardType.Card, PropertyId);
        card.ClearDomainEvents();
        card.ReportStolen();
        card.Status.Should().Be(RfidCardStatus.Stolen);
        card.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RfidCardStolenEvent>();
    }

    [Fact]
    public void UpdateLastUsed_SetsTimestamp()
    {
        var card = RfidCard.Create(StaffId, "ABC", RfidCardType.Keyfob, PropertyId);
        card.UpdateLastUsed();
        card.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

public class RfidReaderTests
{
    private static readonly Guid PropertyId = Guid.NewGuid();

    [Fact]
    public void Create_SetsAllProperties()
    {
        var reader = RfidReader.Create(PropertyId, "SN-001", "Main Entrance", RfidReaderType.Fixed, "api-key-123");

        reader.PropertyId.Should().Be(PropertyId);
        reader.ReaderSerial.Should().Be("SN-001");
        reader.ReaderName.Should().Be("Main Entrance");
        reader.ReaderType.Should().Be(RfidReaderType.Fixed);
        reader.ApiKey.Should().Be("api-key-123");
        reader.Status.Should().Be(ReaderStatus.Active);
    }

    [Fact]
    public void Create_RaisesEvent()
    {
        var reader = RfidReader.Create(PropertyId, "SN-001", "Test", RfidReaderType.Mobile, "key");
        reader.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RfidReaderRegisteredEvent>();
    }

    [Fact]
    public void UpdateLastSeen_SetsTimestamp()
    {
        var reader = RfidReader.Create(PropertyId, "SN-001", "Test", RfidReaderType.Fixed, "key");
        reader.UpdateLastSeen();
        reader.LastSeenAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdateLastSeen_WhenOffline_TransitionsToActive()
    {
        var reader = RfidReader.Create(PropertyId, "SN-001", "Test", RfidReaderType.Fixed, "key");
        reader.MarkOffline();
        reader.ClearDomainEvents();
        reader.UpdateLastSeen();

        reader.Status.Should().Be(ReaderStatus.Active);
        reader.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RfidReaderOnlineEvent>();
    }

    [Fact]
    public void RecordHeartbeat_DelegatesToUpdateLastSeen()
    {
        var reader = RfidReader.Create(PropertyId, "SN-001", "Test", RfidReaderType.Fixed, "key");
        reader.RecordHeartbeat();
        reader.LastSeenAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkOffline_SetsStatusAndRaisesEvent()
    {
        var reader = RfidReader.Create(PropertyId, "SN-001", "Test", RfidReaderType.Fixed, "key");
        reader.ClearDomainEvents();
        reader.MarkOffline();

        reader.Status.Should().Be(ReaderStatus.Offline);
        reader.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RfidReaderOfflineEvent>();
    }

    [Fact]
    public void ValidateApiKey_CorrectKey_ReturnsTrue()
    {
        var reader = RfidReader.Create(PropertyId, "SN-001", "Test", RfidReaderType.Fixed, "secret-key");
        reader.ValidateApiKey("secret-key").Should().BeTrue();
    }

    [Fact]
    public void ValidateApiKey_WrongKey_ReturnsFalse()
    {
        var reader = RfidReader.Create(PropertyId, "SN-001", "Test", RfidReaderType.Fixed, "secret-key");
        reader.ValidateApiKey("wrong-key").Should().BeFalse();
    }
}
