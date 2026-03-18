using FluentValidation;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;

namespace SAFARIstack.API.Endpoints;

// ═══════════════════════════════════════════════════════════════════════
//  AUTH VALIDATORS
// ═══════════════════════════════════════════════════════════════════════

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email address.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[!@#$%^&*(),.?""{}|<>]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100)
            .Matches(@"^[\p{L}\s'-]+$").WithMessage("First name contains invalid characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100)
            .Matches(@"^[\p{L}\s'-]+$").WithMessage("Last name contains invalid characters.");

        // PropertyId and RoleName are optional for self-registration
        // Backend assigns defaults (first property, FrontDesk role) when omitted
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[!@#$%^&*(),.?""{}|<>]").WithMessage("Password must contain at least one special character.")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from current password.");
    }
}

public class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .Matches(@"^[\d\s\+\-()]+$").WithMessage("Phone number contains invalid characters.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.AvatarUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Avatar URL must be a valid URL.")
            .When(x => !string.IsNullOrEmpty(x.AvatarUrl));
    }
}

public class AssignRoleRequestValidator : AbstractValidator<AssignRoleRequest>
{
    public AssignRoleRequestValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(50);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  GUEST VALIDATORS
// ═══════════════════════════════════════════════════════════════════════

public class CreateGuestRequestValidator : AbstractValidator<CreateGuestRequest>
{
    public CreateGuestRequestValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("Property ID is required.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100)
            .Matches(@"^[\p{L}\s'-]+$").WithMessage("First name contains invalid characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100)
            .Matches(@"^[\p{L}\s'-]+$").WithMessage("Last name contains invalid characters.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email address.")
            .MaximumLength(256)
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .Matches(@"^[\d\s\+\-()]+$").WithMessage("Phone number contains invalid characters.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.IdNumber)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.IdNumber));

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.UtcNow).WithMessage("Date of birth must be in the past.")
            .When(x => x.DateOfBirth.HasValue);

        RuleFor(x => x.CompanyVATNumber)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.CompanyVATNumber));
    }
}

public class UpdateContactRequestValidator : AbstractValidator<UpdateContactRequest>
{
    public UpdateContactRequestValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email address.")
            .MaximumLength(256)
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .Matches(@"^[\d\s\+\-()]+$").WithMessage("Phone number contains invalid characters.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.Email) || !string.IsNullOrEmpty(x.Phone))
            .WithMessage("At least one of email or phone must be provided.");
    }
}

public class BlacklistRequestValidator : AbstractValidator<BlacklistRequest>
{
    public BlacklistRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason for blacklisting is required.")
            .MaximumLength(500);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  ROOM VALIDATORS
// ═══════════════════════════════════════════════════════════════════════

public class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomRequestValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("Property ID is required.");

        RuleFor(x => x.RoomTypeId)
            .NotEmpty().WithMessage("Room type ID is required.");

        RuleFor(x => x.RoomNumber)
            .NotEmpty().WithMessage("Room number is required.")
            .MaximumLength(20);

        RuleFor(x => x.Floor)
            .InclusiveBetween(-5, 200).WithMessage("Floor must be between -5 and 200.")
            .When(x => x.Floor.HasValue);

        RuleFor(x => x.Wing)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.Wing));
    }
}

public class CreateRoomBlockRequestValidator : AbstractValidator<CreateRoomBlockRequest>
{
    public CreateRoomBlockRequestValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("Property ID is required.");

        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Room ID is required.");

        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date).WithMessage("Start date cannot be in the past.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");

        RuleFor(x => x.Reason)
            .IsInEnum().WithMessage("Invalid room block reason.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  FINANCIAL VALIDATORS
// ═══════════════════════════════════════════════════════════════════════

public class CreateFolioRequestValidator : AbstractValidator<CreateFolioRequest>
{
    public CreateFolioRequestValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("Property ID is required.");

        RuleFor(x => x.BookingId)
            .NotEmpty().WithMessage("Booking ID is required.");

        RuleFor(x => x.GuestId)
            .NotEmpty().WithMessage("Guest ID is required.");
    }
}

public class AddChargeRequestValidator : AbstractValidator<AddChargeRequest>
{
    public AddChargeRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Charge description is required.")
            .MaximumLength(250);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Charge amount must be positive.")
            .LessThanOrEqualTo(1_000_000).WithMessage("Charge amount exceeds maximum.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid charge category.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1.")
            .LessThanOrEqualTo(1000).WithMessage("Quantity exceeds maximum.");
    }
}

public class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("Property ID is required.");

        RuleFor(x => x.FolioId)
            .NotEmpty().WithMessage("Folio ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Payment amount must be positive.")
            .LessThanOrEqualTo(10_000_000).WithMessage("Payment amount exceeds maximum.");

        RuleFor(x => x.Method)
            .IsInEnum().WithMessage("Invalid payment method.");

        RuleFor(x => x.TransactionReference)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.TransactionReference));
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  HOUSEKEEPING VALIDATORS
// ═══════════════════════════════════════════════════════════════════════

public class CreateHkTaskRequestValidator : AbstractValidator<CreateHkTaskRequest>
{
    public CreateHkTaskRequestValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("Property ID is required.");

        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Room ID is required.");

        RuleFor(x => x.TaskType)
            .IsInEnum().WithMessage("Invalid housekeeping task type.");

        RuleFor(x => x.ScheduledDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date.AddDays(-1))
            .WithMessage("Scheduled date cannot be more than one day in the past.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority.");
    }
}

public class CompleteTaskRequestValidator : AbstractValidator<CompleteTaskRequest>
{
    public CompleteTaskRequestValidator()
    {
        // All booleans are valid — no specific rules needed beyond type safety
        // but at least one area should be checked off
        RuleFor(x => x)
            .Must(x => x.LinenChanged || x.BathroomCleaned || x.FloorsCleaned
                        || x.MinibarRestocked || x.AmenitiesReplenished)
            .WithMessage("At least one cleaning area must be marked as completed.");
    }
}

public class InspectTaskRequestValidator : AbstractValidator<InspectTaskRequest>
{
    public InspectTaskRequestValidator()
    {
        RuleFor(x => x.InspectorStaffId)
            .NotEmpty().WithMessage("Inspector staff ID is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  RATE VALIDATORS
// ═══════════════════════════════════════════════════════════════════════

public class CreateSeasonRequestValidator : AbstractValidator<CreateSeasonRequest>
{
    public CreateSeasonRequestValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("Property ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Season name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Season code is required.")
            .MaximumLength(20)
            .Matches(@"^[A-Z0-9_]+$").WithMessage("Season code must be uppercase alphanumeric with underscores.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid season type.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");

        RuleFor(x => x.PriceMultiplier)
            .GreaterThan(0).WithMessage("Price multiplier must be positive.")
            .LessThanOrEqualTo(10).WithMessage("Price multiplier exceeds maximum of 10x.");

        RuleFor(x => x.Priority)
            .InclusiveBetween(0, 100).WithMessage("Priority must be between 0 and 100.");
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  RFID VALIDATORS
// ═══════════════════════════════════════════════════════════════════════

public class RfidCheckInRequestValidator : AbstractValidator<RfidCheckInRequest>
{
    public RfidCheckInRequestValidator()
    {
        RuleFor(x => x.CardUid)
            .NotEmpty().WithMessage("Card UID is required.")
            .MaximumLength(50);
    }
}

public class RfidCheckOutRequestValidator : AbstractValidator<RfidCheckOutRequest>
{
    public RfidCheckOutRequestValidator()
    {
        RuleFor(x => x.CardUid)
            .NotEmpty().WithMessage("Card UID is required.")
            .MaximumLength(50);
    }
}

public class RfidHeartbeatRequestValidator : AbstractValidator<RfidHeartbeatRequest>
{
    public RfidHeartbeatRequestValidator()
    {
        RuleFor(x => x.ReaderId)
            .NotEmpty().WithMessage("Reader ID is required.");

        RuleFor(x => x.ReaderSerial)
            .NotEmpty().WithMessage("Reader serial is required.")
            .MaximumLength(100);

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .MaximumLength(50);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  BOOKING VALIDATORS
// ═══════════════════════════════════════════════════════════════════════

public class CheckInRequestValidator : AbstractValidator<CheckInRequest>
{
    public CheckInRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}

public class CheckOutRequestValidator : AbstractValidator<CheckOutRequest>
{
    public CheckOutRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
