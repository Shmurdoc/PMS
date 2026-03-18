using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════
// GIFT CARD COMMERCE ENGINE — Digital Gift Card Sales & Redemption
// ═══════════════════════════════════════════════════════════════════════

public class GiftCard : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public string CardNumber { get; private set; } = string.Empty;
    public string PinHash { get; private set; } = string.Empty;
    public decimal InitialBalance { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public string Currency { get; private set; } = "ZAR";
    public string? RecipientName { get; private set; }
    public string? RecipientEmail { get; private set; }
    public string? SenderName { get; private set; }
    public string? SenderEmail { get; private set; }
    public DateTime? ScheduledDeliveryDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public GiftCardStatus Status { get; private set; } = GiftCardStatus.Active;
    public string? DesignTemplate { get; private set; }
    public string? PersonalMessage { get; private set; }
    public bool IsMultiPropertyRedeemable { get; private set; } = true;

    // Navigation
    private readonly List<GiftCardRedemption> _redemptions = new();
    public IReadOnlyCollection<GiftCardRedemption> Redemptions => _redemptions.AsReadOnly();

    private GiftCard() { }

    public static GiftCard Create(
        Guid propertyId,
        string cardNumber,
        string pinHash,
        decimal amount,
        string? recipientName = null,
        string? recipientEmail = null,
        string? senderName = null,
        string? senderEmail = null,
        DateTime? scheduledDeliveryDate = null,
        DateTime? expiryDate = null,
        string? designTemplate = null,
        string? personalMessage = null,
        bool isMultiPropertyRedeemable = true)
    {
        if (amount < 50)
            throw new ArgumentException("Gift card minimum value is R50.", nameof(amount));
        if (amount > 50000)
            throw new ArgumentException("Gift card maximum value is R50,000.", nameof(amount));

        return new GiftCard
        {
            PropertyId = propertyId,
            CardNumber = cardNumber,
            PinHash = pinHash,
            InitialBalance = amount,
            CurrentBalance = amount,
            RecipientName = recipientName?.Trim(),
            RecipientEmail = recipientEmail?.Trim(),
            SenderName = senderName?.Trim(),
            SenderEmail = senderEmail?.Trim(),
            ScheduledDeliveryDate = scheduledDeliveryDate,
            ExpiryDate = expiryDate,
            DesignTemplate = designTemplate,
            PersonalMessage = personalMessage?.Trim(),
            IsMultiPropertyRedeemable = isMultiPropertyRedeemable
        };
    }

    public GiftCardRedemption Redeem(Guid propertyId, decimal amount, Guid? bookingId = null, Guid? folioId = null)
    {
        if (Status != GiftCardStatus.Active)
            throw new InvalidOperationException($"Gift card is {Status} and cannot be redeemed.");
        if (ExpiryDate.HasValue && DateTime.UtcNow > ExpiryDate.Value)
            throw new InvalidOperationException("Gift card has expired.");
        if (amount <= 0)
            throw new ArgumentException("Redemption amount must be positive.", nameof(amount));
        if (amount > CurrentBalance)
            throw new InvalidOperationException($"Insufficient balance. Available: {CurrentBalance:C}");

        CurrentBalance -= amount;

        var redemption = GiftCardRedemption.Create(Id, propertyId, amount, CurrentBalance, bookingId, folioId);
        _redemptions.Add(redemption);

        if (CurrentBalance == 0)
            Status = GiftCardStatus.FullyRedeemed;

        return redemption;
    }

    public void Void(string reason)
    {
        Status = GiftCardStatus.Voided;
        AddDomainEvent(new GiftCardVoidedEvent(Id, CardNumber, reason));
    }

    public void Expire()
    {
        if (Status == GiftCardStatus.Active)
            Status = GiftCardStatus.Expired;
    }

    public static string GenerateCardNumber()
    {
        // Format: SFRI-XXXX-XXXX-XXXX (SAFARIstack prefix)
        var random = new Random();
        var part1 = random.Next(1000, 9999);
        var part2 = random.Next(1000, 9999);
        var part3 = random.Next(1000, 9999);
        return $"SFRI-{part1}-{part2}-{part3}";
    }

    public static string GeneratePin()
    {
        var random = new Random();
        return random.Next(1000, 9999).ToString();
    }
}

public enum GiftCardStatus { Active, FullyRedeemed, Expired, Voided, Suspended }

// Domain Events
public record GiftCardVoidedEvent(Guid GiftCardId, string CardNumber, string Reason) : DomainEvent;

public class GiftCardRedemption : Entity
{
    public Guid GiftCardId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid? BookingId { get; private set; }
    public Guid? FolioId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal RemainingBalance { get; private set; }
    public bool ReceiptSent { get; private set; }

    // Navigation
    public GiftCard GiftCard { get; private set; } = null!;

    private GiftCardRedemption() { }

    public static GiftCardRedemption Create(Guid giftCardId, Guid propertyId, decimal amount,
        decimal remainingBalance, Guid? bookingId = null, Guid? folioId = null)
    {
        return new GiftCardRedemption
        {
            GiftCardId = giftCardId,
            PropertyId = propertyId,
            Amount = amount,
            RemainingBalance = remainingBalance,
            BookingId = bookingId,
            FolioId = folioId
        };
    }

    public void MarkReceiptSent() => ReceiptSent = true;
}
