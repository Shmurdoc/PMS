using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Shared.ValueObjects;

namespace SAFARIstack.Core.Domain.Services;

// ═══════════════════════════════════════════════════════════════════════
//  AVAILABILITY SERVICE — Determines room availability per date
// ═══════════════════════════════════════════════════════════════════════
public interface IAvailabilityService
{
    Task<IReadOnlyList<RoomAvailabilityResult>> GetAvailabilityAsync(
        Guid propertyId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default);
    Task<bool> IsRoomAvailableAsync(Guid roomId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default);
    Task<int> GetAvailableCountByTypeAsync(Guid roomTypeId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default);
}

public record RoomAvailabilityResult(
    Guid RoomTypeId, string RoomTypeName, int TotalRooms, int AvailableRooms,
    decimal BasePrice, decimal EffectivePrice);

public class AvailabilityService : IAvailabilityService
{
    private readonly IRoomRepository _roomRepo;
    private readonly IRoomTypeRepository _roomTypeRepo;
    private readonly IRateRepository _rateRepo;

    public AvailabilityService(
        IRoomRepository roomRepo,
        IRoomTypeRepository roomTypeRepo,
        IRateRepository rateRepo)
    {
        _roomRepo = roomRepo;
        _roomTypeRepo = roomTypeRepo;
        _rateRepo = rateRepo;
    }

    public async Task<IReadOnlyList<RoomAvailabilityResult>> GetAvailabilityAsync(
        Guid propertyId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default)
    {
        var roomTypes = await _roomTypeRepo.GetByPropertyWithRatesAsync(propertyId, ct);
        var results = new List<RoomAvailabilityResult>();

        foreach (var roomType in roomTypes)
        {
            var availableCount = await _roomTypeRepo.GetAvailableCountAsync(roomType.Id, checkIn, checkOut, ct);
            var effectivePrice = roomType.BasePrice; // Will be overridden by pricing service

            results.Add(new RoomAvailabilityResult(
                roomType.Id, roomType.Name, roomType.RoomCount,
                availableCount, roomType.BasePrice, effectivePrice));
        }

        return results;
    }

    public async Task<bool> IsRoomAvailableAsync(
        Guid roomId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default) =>
        await _roomRepo.IsRoomAvailableAsync(roomId, checkIn, checkOut, ct);

    public async Task<int> GetAvailableCountByTypeAsync(
        Guid roomTypeId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default) =>
        await _roomTypeRepo.GetAvailableCountAsync(roomTypeId, checkIn, checkOut, ct);
}

// ═══════════════════════════════════════════════════════════════════════
//  PRICING SERVICE — Calculates the correct rate for a stay
// ═══════════════════════════════════════════════════════════════════════
public interface IPricingService
{
    Task<StayPricing> CalculateStayPricingAsync(
        Guid roomTypeId, Guid? ratePlanId,
        DateTime checkIn, DateTime checkOut,
        int adults, int children, CancellationToken ct = default);
}

public record StayPricing(
    decimal SubtotalPerNight,
    decimal TotalBeforeTax,
    decimal VATAmount,
    decimal TourismLevyAmount,
    decimal GrandTotal,
    IReadOnlyList<NightlyRate> NightlyBreakdown);

public record NightlyRate(DateTime Date, decimal Amount, string? SeasonName);

public class PricingService : IPricingService
{
    private readonly IRateRepository _rateRepo;
    private readonly IRepository<Season> _seasonRepo;

    public PricingService(IRateRepository rateRepo, IRepository<Season> seasonRepo)
    {
        _rateRepo = rateRepo;
        _seasonRepo = seasonRepo;
    }

    public async Task<StayPricing> CalculateStayPricingAsync(
        Guid roomTypeId, Guid? ratePlanId,
        DateTime checkIn, DateTime checkOut,
        int adults, int children, CancellationToken ct = default)
    {
        var nights = (checkOut.Date - checkIn.Date).Days;
        if (nights <= 0) throw new ArgumentException("Stay must be at least one night.");

        // Bulk-fetch all seasons once (fixes N+1 query)
        var allSeasons = (await _seasonRepo.ListAllAsync(ct))
            .OfType<Season>()
            .Where(s => s.IsActive)
            .ToList();

        var nightlyRates = new List<NightlyRate>();
        decimal totalBeforeTax = 0;

        for (var date = checkIn.Date; date < checkOut.Date; date = date.AddDays(1))
        {
            decimal nightAmount;
            string? seasonName = null;

            if (ratePlanId.HasValue)
            {
                var rate = await _rateRepo.GetEffectiveRateAsync(roomTypeId, ratePlanId.Value, date, ct);
                nightAmount = rate?.AmountPerNight ?? 0;
            }
            else
            {
                // Fall back to room type rates for the date range
                var rates = await _rateRepo.GetRatesByRoomTypeAsync(roomTypeId, date, date.AddDays(1), ct);
                nightAmount = rates.FirstOrDefault()?.AmountPerNight ?? 0;
            }

            // Apply season multiplier from pre-fetched list
            var activeSeason = allSeasons
                .Where(s => s.CoversDate(date))
                .OrderByDescending(s => s.Priority)
                .FirstOrDefault();

            if (activeSeason != null)
            {
                nightAmount *= activeSeason.PriceMultiplier;
                seasonName = activeSeason.Name;
            }

            nightlyRates.Add(new NightlyRate(date, nightAmount, seasonName));
            totalBeforeTax += nightAmount;
        }

        var subtotalPerNight = nights > 0 ? totalBeforeTax / nights : 0;
        var vatAmount = Money.FromZAR(totalBeforeTax).CalculateVAT().Amount;
        var tourismLevy = Money.FromZAR(totalBeforeTax).CalculateTourismLevy().Amount;
        var grandTotal = totalBeforeTax + vatAmount + tourismLevy;

        return new StayPricing(subtotalPerNight, totalBeforeTax, vatAmount, tourismLevy, grandTotal, nightlyRates);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  INVOICE SERVICE — Tax-compliant invoice generation
// ═══════════════════════════════════════════════════════════════════════
public interface IInvoiceService
{
    Task<Invoice> GenerateInvoiceAsync(Guid folioId, Guid? issuedByUserId, CancellationToken ct = default);
}

public class InvoiceService : IInvoiceService
{
    private readonly IFolioRepository _folioRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IRepository<Property> _propertyRepo;
    private readonly IUnitOfWork _uow;

    public InvoiceService(
        IFolioRepository folioRepo,
        IInvoiceRepository invoiceRepo,
        IRepository<Property> propertyRepo,
        IUnitOfWork uow)
    {
        _folioRepo = folioRepo;
        _invoiceRepo = invoiceRepo;
        _propertyRepo = propertyRepo;
        _uow = uow;
    }

    public async Task<Invoice> GenerateInvoiceAsync(Guid folioId, Guid? issuedByUserId, CancellationToken ct = default)
    {
        var folio = await _folioRepo.GetWithLineItemsAsync(folioId, ct)
            ?? throw new InvalidOperationException($"Folio {folioId} not found.");

        var property = await _propertyRepo.GetByIdAsync(folio.PropertyId, ct)
            ?? throw new InvalidOperationException($"Property {folio.PropertyId} not found.");

        var invoiceNumber = await _invoiceRepo.GenerateNextNumberAsync(folio.PropertyId, ct);

        var subtotal = folio.TotalCharges;
        var vatAmount = decimal.Round(subtotal * property.VATRate, 2);
        var tourismLevy = decimal.Round(subtotal * property.TourismLevyRate, 2);

        var invoice = Invoice.Create(
            folio.PropertyId, folioId, folio.GuestId,
            invoiceNumber, DateTime.UtcNow, DateTime.UtcNow.AddDays(30),
            subtotal, vatAmount, tourismLevy);

        if (issuedByUserId.HasValue) invoice.SetCreatedBy(issuedByUserId.Value);

        await _invoiceRepo.AddAsync(invoice, ct);
        await _uow.SaveChangesAsync(ct);

        return invoice;
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  HOUSEKEEPING SCHEDULER — Auto-generates cleaning tasks
// ═══════════════════════════════════════════════════════════════════════
public interface IHousekeepingScheduler
{
    Task GenerateTasksForDeparturesAsync(Guid propertyId, DateTime date, CancellationToken ct = default);
    Task GenerateStayOverTasksAsync(Guid propertyId, DateTime date, CancellationToken ct = default);
}

public class HousekeepingScheduler : IHousekeepingScheduler
{
    private readonly IBookingRepository _bookingRepo;
    private readonly IHousekeepingRepository _hkRepo;
    private readonly IUnitOfWork _uow;

    public HousekeepingScheduler(
        IBookingRepository bookingRepo,
        IHousekeepingRepository hkRepo,
        IUnitOfWork uow)
    {
        _bookingRepo = bookingRepo;
        _hkRepo = hkRepo;
        _uow = uow;
    }

    public async Task GenerateTasksForDeparturesAsync(Guid propertyId, DateTime date, CancellationToken ct = default)
    {
        var departures = await _bookingRepo.GetDeparturesAsync(propertyId, date, ct);

        foreach (var booking in departures)
        {
            foreach (var br in booking.BookingRooms)
            {
                var task = HousekeepingTask.Create(
                    propertyId, br.RoomId, HousekeepingTaskType.Turnover, date, HousekeepingPriority.High);
                await _hkRepo.AddAsync(task, ct);
            }
        }

        await _uow.SaveChangesAsync(ct);
    }

    public async Task GenerateStayOverTasksAsync(Guid propertyId, DateTime date, CancellationToken ct = default)
    {
        var inHouse = await _bookingRepo.GetInHouseAsync(propertyId, ct);

        foreach (var booking in inHouse)
        {
            foreach (var br in booking.BookingRooms)
            {
                var task = HousekeepingTask.Create(
                    propertyId, br.RoomId, HousekeepingTaskType.StayOver, date);
                await _hkRepo.AddAsync(task, ct);
            }
        }

        await _uow.SaveChangesAsync(ct);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  DOMAIN EVENT PUBLISHER — Interface for dispatching events
// ═══════════════════════════════════════════════════════════════════════
public interface IDomainEventPublisher
{
    Task PublishEventsAsync(IEnumerable<Shared.Domain.IDomainEvent> events, CancellationToken ct = default);
}
