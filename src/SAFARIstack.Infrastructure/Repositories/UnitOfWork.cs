using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly MediatRDomainEventDispatcher _domainEventDispatcher;
    private bool _disposed;

    // Lazy-initialised repositories — created on first access
    private IBookingRepository? _bookings;
    private IGuestRepository? _guests;
    private IRoomRepository? _rooms;
    private IRoomTypeRepository? _roomTypes;
    private IFolioRepository? _folios;
    private IPaymentRepository? _payments;
    private IInvoiceRepository? _invoices;
    private IRateRepository? _rates;
    private IHousekeepingRepository? _housekeeping;
    private INotificationRepository? _notifications;
    private IAuditLogRepository? _auditLogs;
    private IPropertySettingsRepository? _propertySettings;
    private IEmailTemplateRepository? _emailTemplates;
    private IMerchantConfigurationRepository? _merchantConfigurations;

    public UnitOfWork(ApplicationDbContext context, MediatRDomainEventDispatcher domainEventDispatcher)
    {
        _context = context;
        _domainEventDispatcher = domainEventDispatcher;
    }

    // ─── Repository Properties ──────────────────────────────────────
    public IBookingRepository Bookings => _bookings ??= new BookingRepository(_context);
    public IGuestRepository Guests => _guests ??= new GuestRepository(_context);
    public IRoomRepository Rooms => _rooms ??= new RoomRepository(_context);
    public IRoomTypeRepository RoomTypes => _roomTypes ??= new RoomTypeRepository(_context);
    public IFolioRepository Folios => _folios ??= new FolioRepository(_context);
    public IPaymentRepository Payments => _payments ??= new PaymentRepository(_context);
    public IInvoiceRepository Invoices => _invoices ??= new InvoiceRepository(_context);
    public IRateRepository Rates => _rates ??= new RateRepository(_context);
    public IHousekeepingRepository Housekeeping => _housekeeping ??= new HousekeepingRepository(_context);
    public INotificationRepository Notifications => _notifications ??= new NotificationRepository(_context);
    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context);
    public IPropertySettingsRepository PropertySettings => _propertySettings ??= new PropertySettingsRepository(_context);
    public IEmailTemplateRepository EmailTemplates => _emailTemplates ??= new EmailTemplateRepository(_context);
    public IMerchantConfigurationRepository MerchantConfigurations => _merchantConfigurations ??= new MerchantConfigurationRepository(_context);

    // ─── Generic Repository Access ──────────────────────────────────
    public IRepository<T> Repository<T>() where T : Entity
    {
        return new Repository<T>(_context);
    }

    // ─── Persistence ────────────────────────────────────────────────
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var result = await _context.SaveChangesAsync(ct);

        // Dispatch all domain events collected during SaveChangesAsync
        await _domainEventDispatcher.DispatchPendingEventsAsync(_context, ct);

        return result;
    }

    // ─── Transaction Management ─────────────────────────────────────
    public async Task BeginTransactionAsync(CancellationToken ct = default) =>
        await _context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_context.Database.CurrentTransaction is not null)
            await _context.Database.CommitTransactionAsync(ct);
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_context.Database.CurrentTransaction is not null)
            await _context.Database.RollbackTransactionAsync(ct);
    }

    // ─── Disposal ───────────────────────────────────────────────────
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
