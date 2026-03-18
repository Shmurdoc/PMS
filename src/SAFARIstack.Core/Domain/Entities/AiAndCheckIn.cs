using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════
// AI CONCIERGE — Interaction Tracking & Learning Loop
// ═══════════════════════════════════════════════════════════════════════

public class AiInteraction : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid? GuestId { get; private set; }
    public Guid? BookingId { get; private set; }
    public string Query { get; private set; } = string.Empty;
    public string Response { get; private set; } = string.Empty;
    public decimal ConfidenceScore { get; private set; }
    public string? IntentCategory { get; private set; }
    public AiInteractionOutcome Outcome { get; private set; } = AiInteractionOutcome.Pending;
    public bool WasApproved { get; private set; }
    public bool WasEdited { get; private set; }
    public string? EditedResponse { get; private set; }
    public Guid? ReviewedByStaffId { get; private set; }
    public int? GuestSatisfaction { get; private set; } // 1-5
    public int ProcessingTimeMs { get; private set; }
    public int TokensUsed { get; private set; }
    public decimal Cost { get; private set; }
    public string? ModelUsed { get; private set; }
    public AiInteractionSource Source { get; private set; }

    // Navigation
    public Guest? Guest { get; private set; }
    public Booking? Booking { get; private set; }

    private AiInteraction() { }

    public static AiInteraction Create(
        Guid propertyId, string query, string response,
        decimal confidenceScore, string? intentCategory,
        int processingTimeMs, int tokensUsed, decimal cost,
        string modelUsed, AiInteractionSource source,
        Guid? guestId = null, Guid? bookingId = null)
    {
        return new AiInteraction
        {
            PropertyId = propertyId,
            Query = query,
            Response = response,
            ConfidenceScore = confidenceScore,
            IntentCategory = intentCategory,
            ProcessingTimeMs = processingTimeMs,
            TokensUsed = tokensUsed,
            Cost = cost,
            ModelUsed = modelUsed,
            Source = source,
            GuestId = guestId,
            BookingId = bookingId
        };
    }

    public void Approve(Guid staffId)
    {
        WasApproved = true;
        ReviewedByStaffId = staffId;
        Outcome = AiInteractionOutcome.Approved;
    }

    public void EditAndApprove(string editedResponse, Guid staffId)
    {
        WasApproved = true;
        WasEdited = true;
        EditedResponse = editedResponse;
        ReviewedByStaffId = staffId;
        Outcome = AiInteractionOutcome.Edited;
    }

    public void Reject(Guid staffId)
    {
        WasApproved = false;
        ReviewedByStaffId = staffId;
        Outcome = AiInteractionOutcome.Rejected;
    }

    public void SetAutoSent() => Outcome = AiInteractionOutcome.AutoSent;

    public void RecordGuestSatisfaction(int score)
    {
        if (score < 1 || score > 5) throw new ArgumentException("Score must be 1-5.");
        GuestSatisfaction = score;
    }
}

public enum AiInteractionOutcome { Pending, AutoSent, Approved, Edited, Rejected, Escalated }
public enum AiInteractionSource { WebChat, SMS, WhatsApp, GuestApp, StaffInbox }

// ═══════════════════════════════════════════════════════════════════════
// CONTACTLESS CHECK-IN — Digital Registration & Mobile Key
// ═══════════════════════════════════════════════════════════════════════

public class DigitalCheckIn : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid GuestId { get; private set; }
    public string Token { get; private set; } = string.Empty; // Secure check-in link token
    public DateTime TokenExpiry { get; private set; }
    public DigitalCheckInStatus Status { get; private set; } = DigitalCheckInStatus.Invited;

    // Identity Verification
    public bool IdVerified { get; private set; }
    public string? IdDocumentType { get; private set; }
    public string? IdDocumentHash { get; private set; } // POPIA: hash only
    public decimal? IdVerificationConfidence { get; private set; }
    public DateTime? IdVerifiedAt { get; private set; }

    // Registration Card
    public string? SignatureData { get; private set; } // Base64 signature
    public DateTime? SignedAt { get; private set; }
    public string? SignedFromIpAddress { get; private set; }
    public string? ConsentVersion { get; private set; }
    public bool PopiaConsentGiven { get; private set; }
    public bool MarketingConsentGiven { get; private set; }

    // Room Selection
    public Guid? SelectedRoomId { get; private set; }
    public bool RoomUpgradeSelected { get; private set; }
    public decimal? UpgradeAmount { get; private set; }

    // Mobile Key
    public string? MobileKeyId { get; private set; }
    public DateTime? MobileKeyValidFrom { get; private set; }
    public DateTime? MobileKeyValidTo { get; private set; }
    public MobileKeyStatus MobileKeyStatus { get; private set; } = MobileKeyStatus.NotProvisioned;

    public DateTime? CompletedAt { get; private set; }

    // Navigation
    public Booking Booking { get; private set; } = null!;
    public Guest Guest { get; private set; } = null!;

    private DigitalCheckIn() { }

    public static DigitalCheckIn Create(Guid propertyId, Guid bookingId, Guid guestId, int tokenExpiryHours = 72)
    {
        return new DigitalCheckIn
        {
            PropertyId = propertyId,
            BookingId = bookingId,
            GuestId = guestId,
            Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            TokenExpiry = DateTime.UtcNow.AddHours(tokenExpiryHours),
            ConsentVersion = "POPIA-2025-v2"
        };
    }

    public void VerifyIdentity(string documentType, string documentHash, decimal confidence)
    {
        IdDocumentType = documentType;
        IdDocumentHash = documentHash;
        IdVerificationConfidence = confidence;
        IdVerified = confidence >= 0.85m;
        IdVerifiedAt = DateTime.UtcNow;
        Status = DigitalCheckInStatus.IdentityVerified;
    }

    public void SignRegistrationCard(string signatureData, string ipAddress, bool popiaConsent, bool marketingConsent)
    {
        SignatureData = signatureData;
        SignedAt = DateTime.UtcNow;
        SignedFromIpAddress = ipAddress;
        PopiaConsentGiven = popiaConsent;
        MarketingConsentGiven = marketingConsent;
        Status = DigitalCheckInStatus.RegistrationSigned;
    }

    public void SelectRoom(Guid roomId, bool isUpgrade = false, decimal upgradeAmount = 0)
    {
        SelectedRoomId = roomId;
        RoomUpgradeSelected = isUpgrade;
        UpgradeAmount = isUpgrade ? upgradeAmount : 0;
        Status = DigitalCheckInStatus.RoomSelected;
    }

    public void ProvisionMobileKey(string keyId, DateTime validFrom, DateTime validTo)
    {
        MobileKeyId = keyId;
        MobileKeyValidFrom = validFrom;
        MobileKeyValidTo = validTo;
        MobileKeyStatus = MobileKeyStatus.Active;
    }

    public void RevokeMobileKey()
    {
        MobileKeyStatus = MobileKeyStatus.Revoked;
    }

    public void Complete()
    {
        Status = DigitalCheckInStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        if (Status != DigitalCheckInStatus.Completed)
            Status = DigitalCheckInStatus.Expired;
    }
}

public enum DigitalCheckInStatus
{
    Invited,
    Started,
    IdentityVerified,
    RegistrationSigned,
    RoomSelected,
    Completed,
    Expired,
    Cancelled
}

public enum MobileKeyStatus { NotProvisioned, Active, Revoked, Expired }
