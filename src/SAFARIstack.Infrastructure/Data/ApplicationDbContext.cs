using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Core.Domain.Security;
using SAFARIstack.Infrastructure.Data.Models;
using SAFARIstack.Modules.Staff.Domain.Entities;
using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Infrastructure.Data;

/// <summary>
/// Main application DbContext with modular separation, soft-delete, multi-tenancy, and audit support.
/// Global query filters automatically scope ALL multi-tenant entities to the current user's PropertyId.
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly ITenantProvider? _tenantProvider;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    // ─── Core Domain ────────────────────────────────────────────────
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<BookingRoom> BookingRooms => Set<BookingRoom>();
    public DbSet<RoomBlock> RoomBlocks => Set<RoomBlock>();

    // ─── Rates & Pricing ────────────────────────────────────────────
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<RatePlan> RatePlans => Set<RatePlan>();
    public DbSet<Rate> Rates => Set<Rate>();
    public DbSet<CancellationPolicy> CancellationPolicies => Set<CancellationPolicy>();

    // ─── Financial ──────────────────────────────────────────────────
    public DbSet<Folio> Folios => Set<Folio>();
    public DbSet<FolioLineItem> FolioLineItems => Set<FolioLineItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    // ─── Housekeeping ───────────────────────────────────────────────
    public DbSet<HousekeepingTask> HousekeepingTasks => Set<HousekeepingTask>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<RoomTypeAmenity> RoomTypeAmenities => Set<RoomTypeAmenity>();

    // ─── Guest Extensions ───────────────────────────────────────────
    public DbSet<GuestPreference> GuestPreferences => Set<GuestPreference>();
    public DbSet<GuestLoyalty> GuestLoyalties => Set<GuestLoyalty>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // ─── Settings & Configuration ───────────────────────────────────
    public DbSet<PropertySettings> PropertySettings => Set<PropertySettings>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<MerchantConfiguration> MerchantConfigurations => Set<MerchantConfiguration>();
    public DbSet<WebhookLog> WebhookLogs => Set<WebhookLog>();

    // ─── Identity & RBAC ────────────────────────────────────────────
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // ─── Staff Module ───────────────────────────────────────────────
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<RfidCard> RfidCards => Set<RfidCard>();
    public DbSet<RfidReader> RfidReaders => Set<RfidReader>();
    public DbSet<StaffAttendance> StaffAttendances => Set<StaffAttendance>();

    // ─── Event Outbox (Transactional Outbox Pattern) ─────────────────
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    // ─── POS & Inventory (Point of Sale, Daily Reconciliation) ───────
    public DbSet<CasualSale> CasualSales => Set<CasualSale>();
    public DbSet<DayEndClose> DayEndCloses => Set<DayEndClose>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    // ─── Enterprise: Upsell Engine ──────────────────────────────────
    public DbSet<UpsellOffer> UpsellOffers => Set<UpsellOffer>();
    public DbSet<UpsellTransaction> UpsellTransactions => Set<UpsellTransaction>();

    // ─── Enterprise: Gift Cards ─────────────────────────────────────
    public DbSet<GiftCard> GiftCards => Set<GiftCard>();
    public DbSet<GiftCardRedemption> GiftCardRedemptions => Set<GiftCardRedemption>();

    // ─── Enterprise: Experiences ────────────────────────────────────
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<ExperienceSchedule> ExperienceSchedules => Set<ExperienceSchedule>();
    public DbSet<ExperienceBooking> ExperienceBookings => Set<ExperienceBooking>();

    // ─── Enterprise: Multi-Property ─────────────────────────────────
    public DbSet<PropertyGroup> PropertyGroups => Set<PropertyGroup>();
    public DbSet<PropertyGroupMembership> PropertyGroupMemberships => Set<PropertyGroupMembership>();
    public DbSet<RateCopyJob> RateCopyJobs => Set<RateCopyJob>();
    public DbSet<GroupInventoryAllocation> GroupInventoryAllocations => Set<GroupInventoryAllocation>();

    // ─── Enterprise: Guest Messaging ────────────────────────────────
    public DbSet<GuestMessage> GuestMessages => Set<GuestMessage>();
    public DbSet<GuestConversation> GuestConversations => Set<GuestConversation>();

    // ─── Enterprise: AI Concierge ───────────────────────────────────
    public DbSet<AiInteraction> AiInteractions => Set<AiInteraction>();

    // ─── Enterprise: Digital Check-In ───────────────────────────────
    public DbSet<DigitalCheckIn> DigitalCheckIns => Set<DigitalCheckIn>();

    // ─── Operational: Service Requests, Maintenance, Feedback ────────
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<MaintenanceTask> MaintenanceTasks => Set<MaintenanceTask>();
    public DbSet<GuestFeedback> GuestFeedbacks => Set<GuestFeedback>();
    public DbSet<OvertimeRequest> OvertimeRequests => Set<OvertimeRequest>();
    public DbSet<DiningVenue> DiningVenues => Set<DiningVenue>();

    // ─── Security & Compliance ──────────────────────────────────────
    public DbSet<SecurityAlert> SecurityAlerts => Set<SecurityAlert>();

    // ─── Add-ons Management ──────────────────────────────────────────
    public DbSet<InstalledAddOn> InstalledAddOns => Set<InstalledAddOn>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration from this assembly (auto-discovers all configs)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ═══ OPTIMISTIC CONCURRENCY — xmin for PostgreSQL ═══
        // The [Timestamp] attribute on Entity.RowVersion (uint) is auto-mapped
        // to PostgreSQL xmin system column by the Npgsql convention
        // (NpgsqlPostgresModelFinalizingConvention). No explicit configuration needed.

        // ═══════════════════════════════════════════════════════════════
        //  MULTI-TENANCY GLOBAL QUERY FILTERS
        //  These ensure EVERY query is automatically scoped to the
        //  authenticated user's PropertyId. No data leaks between lodges.
        //  SuperAdmin bypass is handled at the ITenantProvider level.
        // ═══════════════════════════════════════════════════════════════

        // Property itself — filter by IsActive (tenant root entity)
        modelBuilder.Entity<Property>().HasQueryFilter(p => p.IsActive);

        // ─── Core Domain (IMultiTenant entities) ─────────────────────
        modelBuilder.Entity<Booking>().HasQueryFilter(b =>
            !b.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || b.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<Guest>().HasQueryFilter(g =>
            !g.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || g.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<Room>().HasQueryFilter(r =>
            !r.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || r.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<RoomType>().HasQueryFilter(rt =>
            !rt.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || rt.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<RoomBlock>().HasQueryFilter(rb =>
            !rb.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || rb.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<BookingRoom>().HasQueryFilter(br => !br.IsDeleted);

        // ─── Rates & Pricing ────────────────────────────────────────
        modelBuilder.Entity<Season>().HasQueryFilter(s =>
            !s.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || s.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<RatePlan>().HasQueryFilter(rp =>
            !rp.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || rp.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<Rate>().HasQueryFilter(r =>
            !r.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || r.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<CancellationPolicy>().HasQueryFilter(cp =>
            !cp.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || cp.PropertyId == _tenantProvider.CurrentPropertyId));

        // ─── Financial ──────────────────────────────────────────────
        modelBuilder.Entity<Folio>().HasQueryFilter(f =>
            !f.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || f.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<FolioLineItem>().HasQueryFilter(fli => !fli.IsDeleted);

        modelBuilder.Entity<Payment>().HasQueryFilter(p =>
            !p.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || p.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<Invoice>().HasQueryFilter(i =>
            !i.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || i.PropertyId == _tenantProvider.CurrentPropertyId));

        // ─── Housekeeping ───────────────────────────────────────────
        modelBuilder.Entity<HousekeepingTask>().HasQueryFilter(ht =>
            !ht.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || ht.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<Amenity>().HasQueryFilter(a =>
            _tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || a.PropertyId == _tenantProvider.CurrentPropertyId);

        // ─── Guest Extensions ───────────────────────────────────────
        modelBuilder.Entity<Notification>().HasQueryFilter(n =>
            _tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || n.PropertyId == _tenantProvider.CurrentPropertyId);

        modelBuilder.Entity<AuditLog>().HasQueryFilter(a =>
            _tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || a.PropertyId == _tenantProvider.CurrentPropertyId);

        // ─── Settings & Configuration ───────────────────────────────
        modelBuilder.Entity<PropertySettings>().HasQueryFilter(ps =>
            !ps.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || ps.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<EmailTemplate>().HasQueryFilter(et =>
            !et.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || et.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<MerchantConfiguration>().HasQueryFilter(mc =>
            !mc.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || mc.PropertyId == _tenantProvider.CurrentPropertyId));

        // ─── Identity & RBAC ────────────────────────────────────────
        modelBuilder.Entity<ApplicationUser>().HasQueryFilter(u =>
            !u.IsDeleted && u.IsActive &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || u.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<Role>().HasQueryFilter(r => !r.IsDeleted);

        // ─── Staff Module ───────────────────────────────────────────
        modelBuilder.Entity<StaffMember>().HasQueryFilter(s =>
            !s.IsDeleted && s.IsActive &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || s.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<StaffAttendance>().HasQueryFilter(sa =>
            !sa.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || sa.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<RfidReader>().HasQueryFilter(rr =>
            !rr.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || rr.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<RfidCard>().HasQueryFilter(rc =>
            !rc.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || rc.PropertyId == _tenantProvider.CurrentPropertyId));

        // ─── Enterprise: Upsell Engine ──────────────────────────────
        modelBuilder.Entity<UpsellOffer>().HasQueryFilter(uo =>
            !uo.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || uo.PropertyId == _tenantProvider.CurrentPropertyId));

        // ─── Enterprise: Gift Cards ─────────────────────────────────
        modelBuilder.Entity<GiftCard>().HasQueryFilter(gc =>
            !gc.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || gc.PropertyId == _tenantProvider.CurrentPropertyId));

        // ─── Enterprise: Experiences ────────────────────────────────
        modelBuilder.Entity<Experience>().HasQueryFilter(e =>
            !e.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || e.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<ExperienceBooking>().HasQueryFilter(eb =>
            !eb.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || eb.PropertyId == _tenantProvider.CurrentPropertyId));

        // ─── Enterprise: Guest Messaging ────────────────────────────
        modelBuilder.Entity<GuestMessage>().HasQueryFilter(gm =>
            !gm.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || gm.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<GuestConversation>().HasQueryFilter(gc =>
            !gc.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || gc.PropertyId == _tenantProvider.CurrentPropertyId));

        // ─── Enterprise: AI Concierge ───────────────────────────────
        modelBuilder.Entity<AiInteraction>().HasQueryFilter(ai =>
            !ai.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || ai.PropertyId == _tenantProvider.CurrentPropertyId));

        // ─── Enterprise: Digital Check-In ───────────────────────────
        modelBuilder.Entity<DigitalCheckIn>().HasQueryFilter(dc =>
            !dc.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || dc.PropertyId == _tenantProvider.CurrentPropertyId));

        // ─── Operational ────────────────────────────────────────────
        modelBuilder.Entity<ServiceRequest>().HasQueryFilter(sr =>
            !sr.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || sr.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<MaintenanceTask>().HasQueryFilter(mt =>
            !mt.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || mt.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<GuestFeedback>().HasQueryFilter(gf =>
            !gf.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || gf.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<OvertimeRequest>().HasQueryFilter(or =>
            !or.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || or.PropertyId == _tenantProvider.CurrentPropertyId));

        modelBuilder.Entity<DiningVenue>().HasQueryFilter(dv =>
            !dv.IsDeleted &&
            (_tenantProvider == null || !_tenantProvider.HasTenantContext || _tenantProvider.IsSuperAdmin
            || dv.PropertyId == _tenantProvider.CurrentPropertyId));
    }

    /// <summary>
    /// Override SaveChanges to handle timestamps, auditing, and soft-delete
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // ═══ FIX: Correct entity state for new entities added via navigation collections ═══
        // When a new child entity (e.g., FolioLineItem) is added to a parent's navigation
        // backing field (e.g., Folio._lineItems), EF Core's DetectChanges may track it as
        // Modified instead of Added — because the entity has a client-generated Guid key
        // (not a temporary value). The PostgreSQL xmin system column is always > 0 for
        // persisted rows, so RowVersion == 0 reliably identifies entities that have never
        // been saved. We correct their state to Added before the save executes.
        // NOTE: This fix is PostgreSQL-specific (xmin). Skip for InMemory provider where
        // RowVersion stays at 0 forever, which would incorrectly flip Modified → Added.
        if (Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
        {
            foreach (var entry in ChangeTracker.Entries<Entity>())
            {
                if (entry.State == EntityState.Modified && entry.Entity.RowVersion == 0)
                {
                    entry.State = EntityState.Added;
                }
            }
        }

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        // Handle soft-delete: intercept deletes and convert to updates
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.SoftDelete(null);
            }
        }

        // Collect domain events before saving
        var domainEvents = ChangeTracker.Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .SelectMany(e =>
            {
                var events = e.DomainEvents.ToList();
                e.ClearDomainEvents();
                return events;
            })
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Domain events will be dispatched by DomainEventDispatcher middleware/decorator
        // stored temporarily for the dispatcher to pick up
        if (domainEvents.Count > 0)
        {
            PendingDomainEvents.AddRange(domainEvents);
        }

        return result;
    }

    /// <summary>
    /// Collected domain events from the last SaveChangesAsync call, ready for dispatching
    /// </summary>
    internal List<IDomainEvent> PendingDomainEvents { get; } = new();
}
