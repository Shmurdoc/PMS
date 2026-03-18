using FluentAssertions;
using FluentValidation.TestHelper;
using SAFARIstack.API.Endpoints;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;

namespace SAFARIstack.Tests.Unit.Validators;

// ═══════════════════════════════════════════════════════════════════════
//  AUTH VALIDATORS
// ═══════════════════════════════════════════════════════════════════════
public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    private static RegisterRequest ValidRequest => new(
        Guid.NewGuid(), "user@example.com", "P@ssw0rd!", "John", "Doe", "+27123456789", "FrontDesk");

    [Fact]
    public void ValidRequest_Passes()
    {
        var result = _validator.TestValidate(ValidRequest);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    public void InvalidEmail_Fails(string? email)
    {
        var req = ValidRequest with { Email = email! };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("nouppercase1!")]
    [InlineData("NOLOWERCASE1!")]
    [InlineData("NoDigits!!")]
    [InlineData("N0Special1s")]
    public void InvalidPassword_Fails(string password)
    {
        var req = ValidRequest with { Password = password };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void EmptyFirstName_Fails()
    {
        var req = ValidRequest with { FirstName = "" };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void EmptyLastName_Fails()
    {
        var req = ValidRequest with { LastName = "" };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void InvalidNameCharacters_Fails()
    {
        var req = ValidRequest with { FirstName = "John123" };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void EmptyPropertyId_Fails()
    {
        var req = ValidRequest with { PropertyId = Guid.Empty };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.PropertyId);
    }

    [Fact]
    public void EmptyRoleName_Fails()
    {
        var req = ValidRequest with { RoleName = "" };
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.RoleName);
    }

    [Fact]
    public void NameWithHyphen_Passes()
    {
        var req = ValidRequest with { LastName = "Van der Merwe" };
        _validator.TestValidate(req).ShouldNotHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void NameWithApostrophe_Passes()
    {
        var req = ValidRequest with { LastName = "O'Brien" };
        _validator.TestValidate(req).ShouldNotHaveValidationErrorFor(x => x.LastName);
    }
}

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        var result = _validator.TestValidate(new LoginRequest("user@test.com", "password"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyEmail_Fails()
    {
        var result = _validator.TestValidate(new LoginRequest("", "password"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void InvalidEmail_Fails()
    {
        var result = _validator.TestValidate(new LoginRequest("not-email", "password"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void EmptyPassword_Fails()
    {
        var result = _validator.TestValidate(new LoginRequest("user@test.com", ""));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}

public class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        var result = _validator.TestValidate(new ChangePasswordRequest("OldP@ss1!", "NewP@ss2!"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SamePasswords_Fails()
    {
        var result = _validator.TestValidate(new ChangePasswordRequest("P@ssw0rd!", "P@ssw0rd!"));
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void WeakNewPassword_Fails()
    {
        var result = _validator.TestValidate(new ChangePasswordRequest("OldP@ss1!", "weak"));
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  GUEST VALIDATORS
// ═══════════════════════════════════════════════════════════════════════
public class CreateGuestRequestValidatorTests
{
    private readonly CreateGuestRequestValidator _validator = new();

    private static CreateGuestRequest ValidRequest => new(
        Guid.NewGuid(), "John", "Doe", "john@test.com", "+27123456789",
        null, null, null, null, null);

    [Fact]
    public void ValidRequest_Passes()
    {
        _validator.TestValidate(ValidRequest).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyFirstName_Fails()
    {
        var req = ValidRequest with { FirstName = "" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void EmptyLastName_Fails()
    {
        var req = ValidRequest with { LastName = "" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void InvalidEmail_Fails()
    {
        var req = ValidRequest with { Email = "not-email" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void NullEmail_Passes()
    {
        var req = ValidRequest with { Email = null };
        _validator.TestValidate(req).ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void FutureDOB_Fails()
    {
        var req = ValidRequest with { DateOfBirth = DateTime.UtcNow.AddDays(1) };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public void EmptyPropertyId_Fails()
    {
        var req = ValidRequest with { PropertyId = Guid.Empty };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.PropertyId);
    }
}

public class UpdateContactRequestValidatorTests
{
    private readonly UpdateContactRequestValidator _validator = new();

    [Fact]
    public void ValidEmailOnly_Passes()
    {
        _validator.TestValidate(new UpdateContactRequest("a@b.com", null)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidPhoneOnly_Passes()
    {
        _validator.TestValidate(new UpdateContactRequest(null, "+27123456789")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void BothEmpty_Fails()
    {
        _validator.TestValidate(new UpdateContactRequest(null, null)).ShouldHaveAnyValidationError();
    }

    [Fact]
    public void InvalidPhone_Fails()
    {
        _validator.TestValidate(new UpdateContactRequest("a@b.com", "not-a-phone$%")).ShouldHaveValidationErrorFor(x => x.Phone);
    }
}

public class BlacklistRequestValidatorTests
{
    private readonly BlacklistRequestValidator _validator = new();

    [Fact]
    public void ValidReason_Passes()
    {
        _validator.TestValidate(new BlacklistRequest("Property damage")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyReason_Fails()
    {
        _validator.TestValidate(new BlacklistRequest("")).ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void TooLongReason_Fails()
    {
        _validator.TestValidate(new BlacklistRequest(new string('x', 501))).ShouldHaveValidationErrorFor(x => x.Reason);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  ROOM VALIDATORS
// ═══════════════════════════════════════════════════════════════════════
public class CreateRoomRequestValidatorTests
{
    private readonly CreateRoomRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        _validator.TestValidate(new CreateRoomRequest(Guid.NewGuid(), Guid.NewGuid(), "101", 1, "North"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyRoomNumber_Fails()
    {
        _validator.TestValidate(new CreateRoomRequest(Guid.NewGuid(), Guid.NewGuid(), "", null, null))
            .ShouldHaveValidationErrorFor(x => x.RoomNumber);
    }

    [Fact]
    public void FloorOutOfRange_Fails()
    {
        _validator.TestValidate(new CreateRoomRequest(Guid.NewGuid(), Guid.NewGuid(), "101", -10, null))
            .ShouldHaveValidationErrorFor(x => x.Floor);
    }
}

public class CreateRoomBlockRequestValidatorTests
{
    private readonly CreateRoomBlockRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        _validator.TestValidate(new CreateRoomBlockRequest(
            Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(5),
            RoomBlockReason.Renovation, "Full refurb"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EndBeforeStart_Fails()
    {
        _validator.TestValidate(new CreateRoomBlockRequest(
            Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(1),
            RoomBlockReason.Maintenance, null))
            .ShouldHaveValidationErrorFor(x => x.EndDate);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  FINANCIAL VALIDATORS
// ═══════════════════════════════════════════════════════════════════════
public class AddChargeRequestValidatorTests
{
    private readonly AddChargeRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        _validator.TestValidate(new AddChargeRequest("Room Night", 1500, ChargeCategory.RoomCharge, 1))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ZeroAmount_Fails()
    {
        _validator.TestValidate(new AddChargeRequest("Test", 0, ChargeCategory.Other, 1))
            .ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void NegativeAmount_Fails()
    {
        _validator.TestValidate(new AddChargeRequest("Test", -100, ChargeCategory.Other, 1))
            .ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ExcessiveAmount_Fails()
    {
        _validator.TestValidate(new AddChargeRequest("Test", 1_000_001, ChargeCategory.Other, 1))
            .ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ZeroQuantity_Fails()
    {
        _validator.TestValidate(new AddChargeRequest("Test", 100, ChargeCategory.Other, 0))
            .ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void EmptyDescription_Fails()
    {
        _validator.TestValidate(new AddChargeRequest("", 100, ChargeCategory.Other, 1))
            .ShouldHaveValidationErrorFor(x => x.Description);
    }
}

public class RecordPaymentRequestValidatorTests
{
    private readonly RecordPaymentRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        _validator.TestValidate(new RecordPaymentRequest(Guid.NewGuid(), Guid.NewGuid(), 1000, PaymentMethod.CreditCard, null, Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ZeroAmount_Fails()
    {
        _validator.TestValidate(new RecordPaymentRequest(Guid.NewGuid(), Guid.NewGuid(), 0, PaymentMethod.Cash, null, null))
            .ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void ExcessiveAmount_Fails()
    {
        _validator.TestValidate(new RecordPaymentRequest(Guid.NewGuid(), Guid.NewGuid(), 10_000_001, PaymentMethod.Cash, null, null))
            .ShouldHaveValidationErrorFor(x => x.Amount);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  HOUSEKEEPING VALIDATORS
// ═══════════════════════════════════════════════════════════════════════
public class CreateHkTaskRequestValidatorTests
{
    private readonly CreateHkTaskRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        _validator.TestValidate(new CreateHkTaskRequest(
            Guid.NewGuid(), Guid.NewGuid(), HousekeepingTaskType.Turnover,
            DateTime.UtcNow, HousekeepingPriority.Normal))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyPropertyId_Fails()
    {
        _validator.TestValidate(new CreateHkTaskRequest(
            Guid.Empty, Guid.NewGuid(), HousekeepingTaskType.Turnover,
            DateTime.UtcNow, HousekeepingPriority.Normal))
            .ShouldHaveValidationErrorFor(x => x.PropertyId);
    }
}

public class CompleteTaskRequestValidatorTests
{
    private readonly CompleteTaskRequestValidator _validator = new();

    [Fact]
    public void AllFalse_Fails()
    {
        _validator.TestValidate(new CompleteTaskRequest(false, false, false, false, false))
            .ShouldHaveAnyValidationError();
    }

    [Fact]
    public void AtLeastOneTrue_Passes()
    {
        _validator.TestValidate(new CompleteTaskRequest(true, false, false, false, false))
            .ShouldNotHaveAnyValidationErrors();
    }
}

public class InspectTaskRequestValidatorTests
{
    private readonly InspectTaskRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        _validator.TestValidate(new InspectTaskRequest(Guid.NewGuid(), true, "Good"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyInspectorId_Fails()
    {
        _validator.TestValidate(new InspectTaskRequest(Guid.Empty, true, null))
            .ShouldHaveValidationErrorFor(x => x.InspectorStaffId);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  RATE VALIDATORS
// ═══════════════════════════════════════════════════════════════════════
public class CreateSeasonRequestValidatorTests
{
    private readonly CreateSeasonRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        _validator.TestValidate(new CreateSeasonRequest(
            Guid.NewGuid(), "Peak", "PEAK", SeasonType.Peak,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(3), 1.3m, 10))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EndBeforeStart_Fails()
    {
        _validator.TestValidate(new CreateSeasonRequest(
            Guid.NewGuid(), "Peak", "PEAK", SeasonType.Peak,
            DateTime.UtcNow.AddDays(10), DateTime.UtcNow, 1.3m, 10))
            .ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void ZeroMultiplier_Fails()
    {
        _validator.TestValidate(new CreateSeasonRequest(
            Guid.NewGuid(), "Peak", "PEAK", SeasonType.Peak,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1), 0, 10))
            .ShouldHaveValidationErrorFor(x => x.PriceMultiplier);
    }

    [Fact]
    public void ExcessiveMultiplier_Fails()
    {
        _validator.TestValidate(new CreateSeasonRequest(
            Guid.NewGuid(), "Peak", "PEAK", SeasonType.Peak,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1), 11m, 10))
            .ShouldHaveValidationErrorFor(x => x.PriceMultiplier);
    }

    [Fact]
    public void InvalidCode_LowerCase_Fails()
    {
        _validator.TestValidate(new CreateSeasonRequest(
            Guid.NewGuid(), "Peak", "peak", SeasonType.Peak,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1), 1.3m, 10))
            .ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void PriorityOutOfRange_Fails()
    {
        _validator.TestValidate(new CreateSeasonRequest(
            Guid.NewGuid(), "Peak", "PEAK", SeasonType.Peak,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1), 1.3m, 101))
            .ShouldHaveValidationErrorFor(x => x.Priority);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  RFID VALIDATORS
// ═══════════════════════════════════════════════════════════════════════
public class RfidCheckInRequestValidatorTests
{
    private readonly RfidCheckInRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        _validator.TestValidate(new RfidCheckInRequest("ABC123DEF", Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyCardUid_Fails()
    {
        _validator.TestValidate(new RfidCheckInRequest("", null)).ShouldHaveValidationErrorFor(x => x.CardUid);
    }
}

public class RfidHeartbeatRequestValidatorTests
{
    private readonly RfidHeartbeatRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        _validator.TestValidate(new RfidHeartbeatRequest(Guid.NewGuid(), "SN-001", "Active"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyReaderId_Fails()
    {
        _validator.TestValidate(new RfidHeartbeatRequest(Guid.Empty, "SN-001", "Active"))
            .ShouldHaveValidationErrorFor(x => x.ReaderId);
    }

    [Fact]
    public void EmptyStatus_Fails()
    {
        _validator.TestValidate(new RfidHeartbeatRequest(Guid.NewGuid(), "SN-001", ""))
            .ShouldHaveValidationErrorFor(x => x.Status);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  BOOKING VALIDATORS
// ═══════════════════════════════════════════════════════════════════════
public class CheckInRequestValidatorTests
{
    private readonly CheckInRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        _validator.TestValidate(new CheckInRequest(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyUserId_Fails()
    {
        _validator.TestValidate(new CheckInRequest(Guid.Empty)).ShouldHaveValidationErrorFor(x => x.UserId);
    }
}

public class CheckOutRequestValidatorTests
{
    private readonly CheckOutRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        _validator.TestValidate(new CheckOutRequest(Guid.NewGuid())).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyUserId_Fails()
    {
        _validator.TestValidate(new CheckOutRequest(Guid.Empty)).ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
