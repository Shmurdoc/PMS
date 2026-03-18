using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace SAFARIstack.Infrastructure.Services;

/// <summary>
/// Gift Card Commerce Engine — Creation, balance checks, multi-property redemption, voiding
/// </summary>
public class GiftCardService : IGiftCardService
{
    private readonly ApplicationDbContext _db;

    public GiftCardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<GiftCardDto> CreateGiftCardAsync(CreateGiftCardRequestDto request)
    {
        var cardNumber = GiftCard.GenerateCardNumber();
        var pin = GiftCard.GeneratePin();
        var pinHash = HashPin(pin);

        var card = GiftCard.Create(
            request.PropertyId,
            cardNumber,
            pinHash,
            request.Amount,
            request.RecipientName,
            request.RecipientEmail,
            request.SenderName,
            request.SenderEmail,
            request.ScheduledDeliveryDate,
            request.ExpiryDate,
            request.DesignTemplate,
            request.PersonalMessage,
            request.IsMultiPropertyRedeemable);

        await _db.Set<GiftCard>().AddAsync(card);
        await _db.SaveChangesAsync();

        return new GiftCardDto(
            card.Id, cardNumber, card.InitialBalance,
            card.CurrentBalance, card.Status.ToString(),
            card.RecipientName, card.RecipientEmail,
            card.ExpiryDate, card.CreatedAt);
    }

    public async Task<GiftCardBalanceDto> CheckBalanceAsync(string cardNumber, string pin)
    {
        var pinHash = HashPin(pin);
        var card = await _db.Set<GiftCard>()
            .FirstOrDefaultAsync(c => c.CardNumber == cardNumber && c.PinHash == pinHash)
            ?? throw new InvalidOperationException("Invalid card number or PIN.");

        return new GiftCardBalanceDto(
            card.CardNumber, card.CurrentBalance,
            card.Status.ToString(), card.ExpiryDate);
    }

    public async Task<GiftCardRedemptionResultDto> RedeemAsync(
        string cardNumber, string pin, decimal amount,
        Guid propertyId, Guid? bookingId = null, Guid? folioId = null)
    {
        try
        {
            var pinHash = HashPin(pin);
            var card = await _db.Set<GiftCard>()
                .Include(c => c.Redemptions)
                .FirstOrDefaultAsync(c => c.CardNumber == cardNumber && c.PinHash == pinHash)
                ?? throw new InvalidOperationException("Invalid card number or PIN.");

            if (!card.IsMultiPropertyRedeemable && card.PropertyId != propertyId)
                throw new InvalidOperationException("This gift card can only be redeemed at the issuing property.");

            var redemption = card.Redeem(propertyId, amount, bookingId, folioId);

            _db.Set<GiftCard>().Update(card);
            await _db.SaveChangesAsync();

            return new GiftCardRedemptionResultDto(true, redemption.Amount, card.CurrentBalance, null);
        }
        catch (Exception ex)
        {
            return new GiftCardRedemptionResultDto(false, 0, 0, ex.Message);
        }
    }

    public async Task<IEnumerable<GiftCardDto>> GetGiftCardsByPropertyAsync(Guid propertyId)
    {
        return await _db.Set<GiftCard>()
            .Where(c => c.PropertyId == propertyId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new GiftCardDto(
                c.Id, c.CardNumber, c.InitialBalance,
                c.CurrentBalance, c.Status.ToString(),
                c.RecipientName, c.RecipientEmail,
                c.ExpiryDate, c.CreatedAt))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task VoidGiftCardAsync(Guid giftCardId, string reason)
    {
        var card = await _db.Set<GiftCard>().FindAsync(giftCardId)
            ?? throw new InvalidOperationException($"Gift card {giftCardId} not found.");

        card.Void(reason);
        _db.Set<GiftCard>().Update(card);
        await _db.SaveChangesAsync();
    }

    private static string HashPin(string pin)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(pin));
        return Convert.ToBase64String(bytes);
    }
}
