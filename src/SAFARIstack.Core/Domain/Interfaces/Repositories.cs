using System.Linq.Expressions;
using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Interfaces;

// ═══════════════════════════════════════════════════════════════════════
//  SPECIFICATION PATTERN — Type-safe query composition
// ═══════════════════════════════════════════════════════════════════════
/// <summary>
/// Encapsulates a query predicate and optional includes, ordering, and paging
/// </summary>
public interface ISpecification<T> where T : Entity
{
    Expression<Func<T, bool>>? Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int? Take { get; }
    int? Skip { get; }
    bool AsNoTracking { get; }
    bool IgnoreQueryFilters { get; }
}

public abstract class Specification<T> : ISpecification<T> where T : Entity
{
    public Expression<Func<T, bool>>? Criteria { get; protected set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; protected set; }
    public Expression<Func<T, object>>? OrderByDescending { get; protected set; }
    public int? Take { get; protected set; }
    public int? Skip { get; protected set; }
    public bool AsNoTracking { get; protected set; } = true;
    public bool IgnoreQueryFilters { get; protected set; }

    protected void AddInclude(Expression<Func<T, object>> includeExpression) =>
        Includes.Add(includeExpression);

    protected void AddInclude(string includeString) =>
        IncludeStrings.Add(includeString);

    protected void ApplyPaging(int skip, int take) { Skip = skip; Take = take; }
    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy) => OrderBy = orderBy;
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDesc) => OrderByDescending = orderByDesc;
}

// ═══════════════════════════════════════════════════════════════════════
//  GENERIC REPOSITORY — Core data access abstraction
// ═══════════════════════════════════════════════════════════════════════
public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<int> CountAsync(ISpecification<T>? spec = null, CancellationToken ct = default);
    Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
}

// ═══════════════════════════════════════════════════════════════════════
//  UNIT OF WORK — Transactional boundary
// ═══════════════════════════════════════════════════════════════════════
public interface IUnitOfWork : IDisposable
{
    IBookingRepository Bookings { get; }
    IGuestRepository Guests { get; }
    IRoomRepository Rooms { get; }
    IRoomTypeRepository RoomTypes { get; }
    IFolioRepository Folios { get; }
    IPaymentRepository Payments { get; }
    IInvoiceRepository Invoices { get; }
    IRateRepository Rates { get; }
    IHousekeepingRepository Housekeeping { get; }
    INotificationRepository Notifications { get; }
    IAuditLogRepository AuditLogs { get; }
    IPropertySettingsRepository PropertySettings { get; }
    IEmailTemplateRepository EmailTemplates { get; }
    IMerchantConfigurationRepository MerchantConfigurations { get; }
    IRepository<T> Repository<T>() where T : Entity;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}

// ═══════════════════════════════════════════════════════════════════════
//  TYPED REPOSITORY INTERFACES — Domain-specific queries
// ═══════════════════════════════════════════════════════════════════════
public interface IBookingRepository : IRepository<Entities.Booking>
{
    Task<Entities.Booking?> GetByReferenceAsync(string reference, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Booking>> GetByPropertyAsync(Guid propertyId, DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Booking>> GetArrivalsAsync(Guid propertyId, DateTime date, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Booking>> GetDeparturesAsync(Guid propertyId, DateTime date, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Booking>> GetInHouseAsync(Guid propertyId, CancellationToken ct = default);
    Task<int> GetOccupancyCountAsync(Guid propertyId, DateTime date, CancellationToken ct = default);
}

public interface IGuestRepository : IRepository<Entities.Guest>
{
    Task<Entities.Guest?> GetByEmailAsync(Guid propertyId, string email, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Guest>> SearchAsync(Guid propertyId, string searchTerm, CancellationToken ct = default);
    Task<Entities.Guest?> GetWithPreferencesAsync(Guid guestId, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Guest>> GetReturningGuestsAsync(Guid propertyId, int minimumStays = 2, CancellationToken ct = default);
}

public interface IRoomRepository : IRepository<Entities.Room>
{
    Task<IReadOnlyList<Entities.Room>> GetAvailableRoomsAsync(Guid propertyId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Room>> GetByStatusAsync(Guid propertyId, Entities.RoomStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Room>> GetByFloorAsync(Guid propertyId, int floor, CancellationToken ct = default);
    Task<bool> IsRoomAvailableAsync(Guid roomId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default);
}

public interface IRoomTypeRepository : IRepository<Entities.RoomType>
{
    Task<IReadOnlyList<Entities.RoomType>> GetByPropertyWithRatesAsync(Guid propertyId, CancellationToken ct = default);
    Task<int> GetAvailableCountAsync(Guid roomTypeId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default);
}

public interface IFolioRepository : IRepository<Entities.Folio>
{
    Task<Entities.Folio?> GetByBookingAsync(Guid bookingId, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Folio>> GetOpenFoliosAsync(Guid propertyId, CancellationToken ct = default);
    Task<Entities.Folio?> GetWithLineItemsAsync(Guid folioId, CancellationToken ct = default);
}

public interface IPaymentRepository : IRepository<Entities.Payment>
{
    Task<IReadOnlyList<Entities.Payment>> GetByFolioAsync(Guid folioId, CancellationToken ct = default);
    Task<decimal> GetTotalRevenueAsync(Guid propertyId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Payment>> GetByMethodAsync(Guid propertyId, Entities.PaymentMethod method, DateTime from, DateTime to, CancellationToken ct = default);
}

public interface IInvoiceRepository : IRepository<Entities.Invoice>
{
    Task<Entities.Invoice?> GetByNumberAsync(string invoiceNumber, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Invoice>> GetOverdueAsync(Guid propertyId, CancellationToken ct = default);
    Task<string> GenerateNextNumberAsync(Guid propertyId, CancellationToken ct = default);
}

public interface IRateRepository : IRepository<Entities.Rate>
{
    Task<Entities.Rate?> GetEffectiveRateAsync(Guid roomTypeId, Guid ratePlanId, DateTime date, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Rate>> GetRatesByRoomTypeAsync(Guid roomTypeId, DateTime from, DateTime to, CancellationToken ct = default);
}

public interface IHousekeepingRepository : IRepository<Entities.HousekeepingTask>
{
    Task<IReadOnlyList<Entities.HousekeepingTask>> GetPendingTasksAsync(Guid propertyId, DateTime date, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.HousekeepingTask>> GetByStaffAsync(Guid staffId, DateTime date, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.HousekeepingTask>> GetByRoomAsync(Guid roomId, CancellationToken ct = default);
}

public interface INotificationRepository : IRepository<Entities.Notification>
{
    Task<IReadOnlyList<Entities.Notification>> GetQueuedAsync(int batchSize, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Notification>> GetByRecipientAsync(Guid? guestId, Guid? staffId, CancellationToken ct = default);
}

public interface IAuditLogRepository : IRepository<Entities.AuditLog>
{
    Task<IReadOnlyList<Entities.AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.AuditLog>> GetByUserAsync(Guid userId, DateTime from, DateTime to, CancellationToken ct = default);
}
