using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.Infrastructure.Services;

/// <summary>
/// Upsell Automation Engine — Personalized offers, dynamic pricing, purchase tracking
/// </summary>
public class UpsellEngine : IUpsellEngine
{
    private readonly ApplicationDbContext _db;
    private readonly IUnitOfWork _uow;

    public UpsellEngine(ApplicationDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<IEnumerable<UpsellOfferDto>> GeneratePersonalizedOffersAsync(
        Guid bookingId, Guid guestId, Guid propertyId)
    {
        var booking = await _db.Bookings.FindAsync(bookingId)
            ?? throw new InvalidOperationException($"Booking {bookingId} not found.");

        var loyalty = await _db.GuestLoyalties
            .FirstOrDefaultAsync(l => l.GuestId == guestId);

        var offers = await _db.Set<UpsellOffer>()
            .Where(o => o.PropertyId == propertyId && o.IsActive)
            .AsNoTracking()
            .ToListAsync();

        var eligible = offers
            .Where(o => o.IsEligible(booking, loyalty))
            .Select(o => new UpsellOfferDto(
                o.Id, o.Title, o.Description,
                o.OfferType.ToString(), o.OriginalPrice, o.OfferPrice,
                o.Savings, o.ImageUrl, o.ValidTo))
            .ToList();

        return eligible;
    }

    public async Task<UpsellPurchaseResultDto> PurchaseUpsellAsync(
        Guid offerId, Guid bookingId, Guid guestId, int quantity = 1)
    {
        try
        {
            var offer = await _db.Set<UpsellOffer>()
                .Include(o => o.Transactions)
                .FirstOrDefaultAsync(o => o.Id == offerId)
                ?? throw new InvalidOperationException($"Upsell offer {offerId} not found.");

            var transaction = offer.Purchase(bookingId, guestId, quantity);

            _db.Set<UpsellOffer>().Update(offer);
            await _db.SaveChangesAsync();

            return new UpsellPurchaseResultDto(true, transaction.Id, transaction.TotalAmount, null);
        }
        catch (Exception ex)
        {
            return new UpsellPurchaseResultDto(false, null, 0, ex.Message);
        }
    }

    public async Task<UpsellAnalyticsDto> GetUpsellAnalyticsAsync(
        Guid propertyId, DateTime startDate, DateTime endDate)
    {
        var transactions = await _db.Set<UpsellTransaction>()
            .Include(t => t.Offer)
            .Where(t => t.Offer.PropertyId == propertyId
                && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var offers = await _db.Set<UpsellOffer>()
            .Where(o => o.PropertyId == propertyId)
            .AsNoTracking()
            .ToListAsync();

        var totalRevenue = transactions.Sum(t => t.UnitPrice * t.Quantity);
        var totalTransactions = transactions.Count;

        var offerPerformance = offers.Select(o =>
        {
            var offerTx = transactions.Where(t => t.OfferId == o.Id).ToList();
            return new UpsellOfferPerformanceDto(
                o.Id, o.Title, o.OfferType.ToString(),
                0, // Views - would need tracking table
                offerTx.Count,
                offerTx.Sum(t => t.UnitPrice * t.Quantity),
                0); // Conversion - needs view data
        }).Where(p => p.Purchases > 0).ToList();

        return new UpsellAnalyticsDto(
            totalRevenue, totalTransactions,
            0, // Conversion rate - needs view tracking
            totalTransactions > 0 ? totalRevenue / totalTransactions : 0,
            offerPerformance);
    }
}
