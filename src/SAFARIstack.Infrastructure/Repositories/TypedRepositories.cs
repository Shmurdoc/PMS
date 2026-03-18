using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.Infrastructure.Repositories;

// ═══════════════════════════════════════════════════════════════════════
//  BOOKING REPOSITORY
// ═══════════════════════════════════════════════════════════════════════
public class BookingRepository : Repository<Booking>, IBookingRepository
{
    public BookingRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Booking?> GetByReferenceAsync(string reference, CancellationToken ct = default) =>
        await DbSet.Include(b => b.Guest).Include(b => b.Property)
            .Include(b => b.BookingRooms).ThenInclude(br => br.Room)
            .Include(b => b.BookingRooms).ThenInclude(br => br.RoomType)
            .FirstOrDefaultAsync(b => b.BookingReference == reference, ct);

    public async Task<IReadOnlyList<Booking>> GetByPropertyAsync(
        Guid propertyId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = DbSet.Include(b => b.Guest)
            .Where(b => b.PropertyId == propertyId);

        if (from.HasValue) query = query.Where(b => b.CheckOutDate >= from.Value);
        if (to.HasValue) query = query.Where(b => b.CheckInDate <= to.Value);

        return await query.OrderByDescending(b => b.CheckInDate).AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Booking>> GetArrivalsAsync(
        Guid propertyId, DateTime date, CancellationToken ct = default) =>
        await DbSet.Include(b => b.Guest).Include(b => b.BookingRooms).ThenInclude(br => br.Room)
            .Where(b => b.PropertyId == propertyId && b.CheckInDate.Date == date.Date && b.Status == BookingStatus.Confirmed)
            .AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<Booking>> GetDeparturesAsync(
        Guid propertyId, DateTime date, CancellationToken ct = default) =>
        await DbSet.Include(b => b.Guest).Include(b => b.BookingRooms).ThenInclude(br => br.Room)
            .Where(b => b.PropertyId == propertyId && b.CheckOutDate.Date == date.Date && b.Status == BookingStatus.CheckedIn)
            .AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<Booking>> GetInHouseAsync(
        Guid propertyId, CancellationToken ct = default) =>
        await DbSet.Include(b => b.Guest).Include(b => b.BookingRooms).ThenInclude(br => br.Room)
            .Where(b => b.PropertyId == propertyId && b.Status == BookingStatus.CheckedIn)
            .AsNoTracking().ToListAsync(ct);

    public async Task<int> GetOccupancyCountAsync(
        Guid propertyId, DateTime date, CancellationToken ct = default) =>
        await DbSet.Where(b => b.PropertyId == propertyId &&
                               b.CheckInDate <= date && b.CheckOutDate > date &&
                               (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn))
            .SelectMany(b => b.BookingRooms).CountAsync(ct);
}

// ═══════════════════════════════════════════════════════════════════════
//  GUEST REPOSITORY
// ═══════════════════════════════════════════════════════════════════════
public class GuestRepository : Repository<Guest>, IGuestRepository
{
    public GuestRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Guest?> GetByEmailAsync(
        Guid propertyId, string email, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(g => g.PropertyId == propertyId && g.Email == email, ct);

    public async Task<IReadOnlyList<Guest>> SearchAsync(
        Guid propertyId, string searchTerm, CancellationToken ct = default) =>
        await DbSet.Where(g => g.PropertyId == propertyId &&
            (g.FirstName.Contains(searchTerm) || g.LastName.Contains(searchTerm) ||
             (g.Email != null && g.Email.Contains(searchTerm)) ||
             (g.Phone != null && g.Phone.Contains(searchTerm))))
            .AsNoTracking().Take(50).ToListAsync(ct);

    public async Task<Guest?> GetWithPreferencesAsync(
        Guid guestId, CancellationToken ct = default) =>
        await DbSet.Include(g => g.Preferences).Include(g => g.Loyalty)
            .FirstOrDefaultAsync(g => g.Id == guestId, ct);

    public async Task<IReadOnlyList<Guest>> GetReturningGuestsAsync(
        Guid propertyId, int minimumStays = 2, CancellationToken ct = default) =>
        await DbSet.Include(g => g.Loyalty)
            .Where(g => g.PropertyId == propertyId && g.Loyalty != null && g.Loyalty.TotalStays >= minimumStays)
            .AsNoTracking().ToListAsync(ct);
}

// ═══════════════════════════════════════════════════════════════════════
//  ROOM REPOSITORY
// ═══════════════════════════════════════════════════════════════════════
public class RoomRepository : Repository<Room>, IRoomRepository
{
    public RoomRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Room>> GetAvailableRoomsAsync(
        Guid propertyId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default)
    {
        var bookedRoomIds = await Context.Set<BookingRoom>()
            .Where(br => br.Booking.PropertyId == propertyId &&
                         br.Booking.CheckInDate < checkOut && br.Booking.CheckOutDate > checkIn &&
                         br.Booking.Status != BookingStatus.Cancelled && br.Booking.Status != BookingStatus.NoShow)
            .Select(br => br.RoomId)
            .Distinct().ToListAsync(ct);

        var blockedRoomIds = await Context.Set<RoomBlock>()
            .Where(rb => rb.PropertyId == propertyId && rb.StartDate < checkOut && rb.EndDate > checkIn)
            .Select(rb => rb.RoomId)
            .Distinct().ToListAsync(ct);

        var excludedIds = bookedRoomIds.Union(blockedRoomIds).ToHashSet();

        return await DbSet.Include(r => r.RoomType)
            .Where(r => r.PropertyId == propertyId && r.IsActive &&
                        r.Status != RoomStatus.OutOfService && r.Status != RoomStatus.Maintenance &&
                        !excludedIds.Contains(r.Id))
            .AsNoTracking().ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Room>> GetByStatusAsync(
        Guid propertyId, RoomStatus status, CancellationToken ct = default) =>
        await DbSet.Include(r => r.RoomType)
            .Where(r => r.PropertyId == propertyId && r.Status == status)
            .AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<Room>> GetByFloorAsync(
        Guid propertyId, int floor, CancellationToken ct = default) =>
        await DbSet.Include(r => r.RoomType)
            .Where(r => r.PropertyId == propertyId && r.Floor == floor)
            .AsNoTracking().ToListAsync(ct);

    public async Task<bool> IsRoomAvailableAsync(
        Guid roomId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default)
    {
        var hasBooking = await Context.Set<BookingRoom>()
            .AnyAsync(br => br.RoomId == roomId &&
                            br.Booking.CheckInDate < checkOut && br.Booking.CheckOutDate > checkIn &&
                            br.Booking.Status != BookingStatus.Cancelled && br.Booking.Status != BookingStatus.NoShow, ct);

        var hasBlock = await Context.Set<RoomBlock>()
            .AnyAsync(rb => rb.RoomId == roomId && rb.StartDate < checkOut && rb.EndDate > checkIn, ct);

        return !hasBooking && !hasBlock;
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  ROOM TYPE REPOSITORY
// ═══════════════════════════════════════════════════════════════════════
public class RoomTypeRepository : Repository<RoomType>, IRoomTypeRepository
{
    public RoomTypeRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<RoomType>> GetByPropertyWithRatesAsync(
        Guid propertyId, CancellationToken ct = default) =>
        await DbSet.Include(rt => rt.Rates).Include(rt => rt.Amenities).ThenInclude(a => a.Amenity)
            .Where(rt => rt.PropertyId == propertyId)
            .AsNoTracking().ToListAsync(ct);

    public async Task<int> GetAvailableCountAsync(
        Guid roomTypeId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default)
    {
        var totalRooms = await Context.Set<Room>()
            .CountAsync(r => r.RoomTypeId == roomTypeId && r.IsActive &&
                             r.Status != RoomStatus.OutOfService && r.Status != RoomStatus.Maintenance, ct);

        var bookedRooms = await Context.Set<BookingRoom>()
            .Where(br => br.RoomTypeId == roomTypeId &&
                         br.Booking.CheckInDate < checkOut && br.Booking.CheckOutDate > checkIn &&
                         br.Booking.Status != BookingStatus.Cancelled && br.Booking.Status != BookingStatus.NoShow)
            .Select(br => br.RoomId).Distinct().CountAsync(ct);

        return Math.Max(0, totalRooms - bookedRooms);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  FOLIO REPOSITORY
// ═══════════════════════════════════════════════════════════════════════
public class FolioRepository : Repository<Folio>, IFolioRepository
{
    public FolioRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Folio?> GetByBookingAsync(
        Guid bookingId, CancellationToken ct = default) =>
        await DbSet.Include(f => f.LineItems).Include(f => f.Payments)
            .FirstOrDefaultAsync(f => f.BookingId == bookingId, ct);

    public async Task<IReadOnlyList<Folio>> GetOpenFoliosAsync(
        Guid propertyId, CancellationToken ct = default) =>
        await DbSet.Include(f => f.Guest)
            .Where(f => f.PropertyId == propertyId && f.Status == FolioStatus.Open)
            .AsNoTracking().ToListAsync(ct);

    public async Task<Folio?> GetWithLineItemsAsync(
        Guid folioId, CancellationToken ct = default) =>
        await DbSet.Include(f => f.LineItems)
            .FirstOrDefaultAsync(f => f.Id == folioId, ct);
}

// ═══════════════════════════════════════════════════════════════════════
//  PAYMENT REPOSITORY
// ═══════════════════════════════════════════════════════════════════════
public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Payment>> GetByFolioAsync(
        Guid folioId, CancellationToken ct = default) =>
        await DbSet.Where(p => p.FolioId == folioId).AsNoTracking().OrderByDescending(p => p.PaymentDate).ToListAsync(ct);

    public async Task<decimal> GetTotalRevenueAsync(
        Guid propertyId, DateTime from, DateTime to, CancellationToken ct = default) =>
        await DbSet.Where(p => p.PropertyId == propertyId &&
                               p.PaymentDate >= from && p.PaymentDate < to &&
                               p.Status == PaymentStatus.Completed && !p.IsRefund)
            .SumAsync(p => p.Amount, ct);

    public async Task<IReadOnlyList<Payment>> GetByMethodAsync(
        Guid propertyId, PaymentMethod method, DateTime from, DateTime to, CancellationToken ct = default) =>
        await DbSet.Where(p => p.PropertyId == propertyId && p.Method == method &&
                               p.PaymentDate >= from && p.PaymentDate < to)
            .AsNoTracking().OrderByDescending(p => p.PaymentDate).ToListAsync(ct);
}

// ═══════════════════════════════════════════════════════════════════════
//  INVOICE REPOSITORY
// ═══════════════════════════════════════════════════════════════════════
public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Invoice?> GetByNumberAsync(
        string invoiceNumber, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, ct);

    public async Task<IReadOnlyList<Invoice>> GetOverdueAsync(
        Guid propertyId, CancellationToken ct = default) =>
        await DbSet.Where(i => i.PropertyId == propertyId &&
                               i.Status == InvoiceStatus.Issued && i.DueDate < DateTime.UtcNow)
            .AsNoTracking().ToListAsync(ct);

    public async Task<string> GenerateNextNumberAsync(Guid propertyId, CancellationToken ct = default)
    {
        var yearStr = DateTime.UtcNow.ToString("yyyy");
        var prefix = $"INV-{yearStr}-";

        // Use PostgreSQL advisory lock to serialize invoice number generation per property
        // The lock key is derived from the property ID to allow concurrent generation across properties
        var lockKey = Math.Abs(propertyId.GetHashCode());
        await Context.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0})", new object[] { lockKey }, ct);

        // After acquiring the lock, find the highest existing invoice number for this year
        var maxNumber = await DbSet
            .Where(i => i.PropertyId == propertyId && i.InvoiceNumber.StartsWith(prefix))
            .Select(i => i.InvoiceNumber.Substring(prefix.Length))
            .MaxAsync(ct) ?? "000000";

        var nextNumber = int.Parse(maxNumber) + 1;
        return $"{prefix}{nextNumber:D6}";
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  RATE REPOSITORY
// ═══════════════════════════════════════════════════════════════════════
public class RateRepository : Repository<Rate>, IRateRepository
{
    public RateRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Rate?> GetEffectiveRateAsync(
        Guid roomTypeId, Guid ratePlanId, DateTime date, CancellationToken ct = default) =>
        await DbSet.Where(r => r.RoomTypeId == roomTypeId && r.RatePlanId == ratePlanId &&
                               r.IsActive && r.EffectiveFrom <= date && r.EffectiveTo >= date)
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Rate>> GetRatesByRoomTypeAsync(
        Guid roomTypeId, DateTime from, DateTime to, CancellationToken ct = default) =>
        await DbSet.Where(r => r.RoomTypeId == roomTypeId && r.IsActive &&
                               r.EffectiveFrom <= to && r.EffectiveTo >= from)
            .AsNoTracking().ToListAsync(ct);
}

// ═══════════════════════════════════════════════════════════════════════
//  HOUSEKEEPING REPOSITORY
// ═══════════════════════════════════════════════════════════════════════
public class HousekeepingRepository : Repository<HousekeepingTask>, IHousekeepingRepository
{
    public HousekeepingRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<HousekeepingTask>> GetPendingTasksAsync(
        Guid propertyId, DateTime date, CancellationToken ct = default) =>
        await DbSet.Include(t => t.Room).ThenInclude(r => r.RoomType)
            .Where(t => t.PropertyId == propertyId && t.ScheduledDate.Date == date.Date &&
                        (t.Status == HousekeepingTaskStatus.Pending || t.Status == HousekeepingTaskStatus.Assigned))
            .OrderByDescending(t => t.Priority).AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<HousekeepingTask>> GetByStaffAsync(
        Guid staffId, DateTime date, CancellationToken ct = default) =>
        await DbSet.Include(t => t.Room)
            .Where(t => t.AssignedToStaffId == staffId && t.ScheduledDate.Date == date.Date)
            .AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<HousekeepingTask>> GetByRoomAsync(
        Guid roomId, CancellationToken ct = default) =>
        await DbSet.Where(t => t.RoomId == roomId)
            .OrderByDescending(t => t.ScheduledDate).Take(30).AsNoTracking().ToListAsync(ct);
}

// ═══════════════════════════════════════════════════════════════════════
//  NOTIFICATION & AUDIT LOG REPOSITORIES
// ═══════════════════════════════════════════════════════════════════════
public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Notification>> GetQueuedAsync(
        int batchSize, CancellationToken ct = default) =>
        await DbSet.Where(n => n.Status == NotificationStatus.Queued)
            .OrderBy(n => n.CreatedAt).Take(batchSize).ToListAsync(ct);

    public async Task<IReadOnlyList<Notification>> GetByRecipientAsync(
        Guid? guestId, Guid? staffId, CancellationToken ct = default) =>
        await DbSet.Where(n => (guestId.HasValue && n.RecipientGuestId == guestId) ||
                               (staffId.HasValue && n.RecipientStaffId == staffId))
            .OrderByDescending(n => n.CreatedAt).Take(100).AsNoTracking().ToListAsync(ct);
}

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityType, Guid entityId, CancellationToken ct = default) =>
        await DbSet.Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt).AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<AuditLog>> GetByUserAsync(
        Guid userId, DateTime from, DateTime to, CancellationToken ct = default) =>
        await DbSet.Where(a => a.UserId == userId && a.CreatedAt >= from && a.CreatedAt < to)
            .OrderByDescending(a => a.CreatedAt).AsNoTracking().ToListAsync(ct);
}

// ═══════════════════════════════════════════════════════════════════════
//  PROPERTY SETTINGS REPOSITORY
// ═══════════════════════════════════════════════════════════════════════
public class PropertySettingsRepository : Repository<PropertySettings>, IPropertySettingsRepository
{
    public PropertySettingsRepository(ApplicationDbContext context) : base(context) { }

    public async Task<PropertySettings?> GetByPropertyIdAsync(
        Guid propertyId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(ps => ps.PropertyId == propertyId, ct);
}

// ═══════════════════════════════════════════════════════════════════════
//  EMAIL TEMPLATE REPOSITORY
// ═══════════════════════════════════════════════════════════════════════
public class EmailTemplateRepository : Repository<EmailTemplate>, IEmailTemplateRepository
{
    public EmailTemplateRepository(ApplicationDbContext context) : base(context) { }

    public async Task<EmailTemplate?> GetActiveTemplateAsync(
        Guid propertyId, NotificationType type, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(et =>
            et.PropertyId == propertyId && et.NotificationType == type && et.IsActive, ct);

    public async Task<IReadOnlyList<EmailTemplate>> GetByPropertyAsync(
        Guid propertyId, CancellationToken ct = default) =>
        await DbSet.Where(et => et.PropertyId == propertyId)
            .OrderBy(et => et.NotificationType).AsNoTracking().ToListAsync(ct);
}

// ═══════════════════════════════════════════════════════════════════════
//  MERCHANT CONFIGURATION REPOSITORY
// ═══════════════════════════════════════════════════════════════════════
public class MerchantConfigurationRepository : Repository<MerchantConfiguration>, IMerchantConfigurationRepository
{
    public MerchantConfigurationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<MerchantConfiguration>> GetByPropertyAsync(
        Guid propertyId, CancellationToken ct = default) =>
        await DbSet.Where(mc => mc.PropertyId == propertyId)
            .OrderBy(mc => mc.ProviderType).AsNoTracking().ToListAsync(ct);

    public async Task<MerchantConfiguration?> GetActiveByProviderAsync(
        Guid propertyId, MerchantProviderType providerType, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(mc =>
            mc.PropertyId == propertyId && mc.ProviderType == providerType && mc.IsActive, ct);
}
