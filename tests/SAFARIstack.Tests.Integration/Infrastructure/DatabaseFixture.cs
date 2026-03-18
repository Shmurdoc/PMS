using Microsoft.EntityFrameworkCore;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Modules.Staff.Domain.Entities;
using SAFARIstack.Shared.Domain;
using SAFARIstack.Shared.ValueObjects;
using System.Diagnostics;

namespace SAFARIstack.Tests.Integration.Infrastructure;

/// <summary>
/// Fake tenant provider for integration tests — SuperAdmin bypasses all filters.
/// </summary>
internal sealed class SuperAdminTenantProvider : ITenantProvider
{
    public Guid CurrentPropertyId => Guid.Empty;
    public bool IsSuperAdmin => true;
    public bool HasTenantContext => true;
    public void ValidatePropertyAccess(Guid propertyId) { }
}

/// <summary>
/// Integration test fixture using real PostgreSQL database with a unique test schema
/// per test run. The fixture seeds ALL data once for the entire collection.
/// Uses a SuperAdmin tenant provider to bypass tenant query filters.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private string _databaseName = null!;
    private static readonly ITenantProvider _tenantProvider = new SuperAdminTenantProvider();
    public ApplicationDbContext Context { get; private set; } = null!;
    public DbContextOptions<ApplicationDbContext> Options { get; private set; } = null!;

    // Test property IDs — set during seeding
    public Guid PropertyA_Id { get; private set; }
    public Guid PropertyB_Id { get; private set; }
    public TimeSpan SeedDuration { get; private set; }

    public async Task InitializeAsync()
    {
        // Create a unique test database for this fixture run
        _databaseName = $"safaristack_test_{Guid.NewGuid():N}";

        Options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql($"Host=localhost;Port=5432;Database={_databaseName};Username=postgres;Password=Morven-05;Include Error Detail=true;Trust Server Certificate=true")
            .Options;

        Context = new ApplicationDbContext(Options, _tenantProvider);
        await Context.Database.EnsureCreatedAsync();

        // Seed all data once for the collection
        await SeedAllData();
    }

    private async Task SeedAllData()
    {
        var sw = Stopwatch.StartNew();
        var ctx = Context;

        // ─── Properties ─────────────────────────────────────────────
        ctx.Properties.Add(Property.Create("Safari Lodge Alpha", "safari-alpha", "123 Main Road", "Cape Town", "Western Cape"));
        ctx.Properties.Add(Property.Create("Safari Lodge Beta", "safari-beta", "456 Game Drive", "Johannesburg", "Gauteng"));
        await ctx.SaveChangesAsync();

        var props = await ctx.Properties.ToListAsync();
        PropertyA_Id = props[0].Id;
        PropertyB_Id = props[1].Id;

        // ─── Room Types (20 per property = 40 total) ────────────────
        var roomTypes = new List<RoomType>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            for (int i = 0; i < 20; i++)
                roomTypes.Add(RoomType.Create(propId, $"Type-{p}-{i}", $"T{p}{i:D2}", 500 + (i * 100), 2 + (i % 3), 2, i % 2));
        }
        ctx.RoomTypes.AddRange(roomTypes);
        await ctx.SaveChangesAsync();

        // ─── Rooms (1000+ — 25 per room type) ───────────────────────
        var rooms = new List<Room>();
        foreach (var rt in roomTypes)
        {
            for (int i = 0; i < 25; i++)
                rooms.Add(Room.Create(rt.PropertyId, rt.Id, $"{rt.PropertyId.ToString()[..4]}-{rooms.Count + 1:D4}", (rooms.Count % 10) + 1, "Wing-" + (rooms.Count % 4)));
        }
        ctx.Rooms.AddRange(rooms);
        await ctx.SaveChangesAsync();

        // ─── Guests (1200 — 600 per property) ───────────────────────
        var guests = new List<Guest>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            for (int i = 0; i < 600; i++)
                guests.Add(Guest.Create(propId, $"First{i}", $"Last{i}", $"guest{i}@property{p}.com", $"+2711000{i:D4}"));
        }
        ctx.Guests.AddRange(guests);
        await ctx.SaveChangesAsync();

        // ─── Seasons (50 per property = 100 total) ──────────────────
        var seasons = new List<Season>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            for (int i = 0; i < 50; i++)
            {
                var start = DateTime.UtcNow.AddDays(i * 7);
                seasons.Add(Season.Create(propId, $"Season-{p}-{i}", $"S{p}{i:D3}", (SeasonType)(i % 4), start, start.AddDays(7), 1.0m + (i % 5) * 0.1m, i));
            }
        }
        ctx.Seasons.AddRange(seasons);
        await ctx.SaveChangesAsync();

        // ─── Rate Plans (100 per property = 200 total) ──────────────
        var ratePlans = new List<RatePlan>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            for (int i = 0; i < 100; i++)
                ratePlans.Add(RatePlan.Create(propId, $"Plan-{p}-{i}", $"RP{p}{i:D4}", RatePlanType.Standard, i % 2 == 0, true));
        }
        ctx.RatePlans.AddRange(ratePlans);
        await ctx.SaveChangesAsync();

        // ─── Bookings (1000 — 500 per property) ─────────────────────
        var bookings = new List<Booking>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            var propGuests = guests.Where(g => g.PropertyId == propId).ToList();
            for (int i = 0; i < 500; i++)
            {
                var guest = propGuests[i % propGuests.Count];
                var checkIn = DateTime.UtcNow.AddDays(i);
                var checkOut = checkIn.AddDays(1 + (i % 5));
                bookings.Add(Booking.Create(propId, guest.Id, $"BK-{propId.ToString()[..4]}-{i:D5}", checkIn, checkOut, 1 + (i % 4), i % 3, null));
            }
        }
        ctx.Bookings.AddRange(bookings);
        await ctx.SaveChangesAsync();

        // ─── Folios (1000 — one per booking) ────────────────────────
        var folios = new List<Folio>();
        for (int i = 0; i < bookings.Count; i++)
        {
            var b = bookings[i];
            folios.Add(Folio.Create(b.PropertyId, b.Id, b.GuestId, $"FOL-{i:D6}"));
        }
        ctx.Folios.AddRange(folios);
        await ctx.SaveChangesAsync();

        // ─── Folio Line Items (3000+ — 3 per folio) ────────────────
        var lineItems = new List<FolioLineItem>();
        foreach (var f in folios)
        {
            lineItems.Add(FolioLineItem.Create(f.Id, "Room Night", 1500, ChargeCategory.RoomCharge, 1));
            lineItems.Add(FolioLineItem.Create(f.Id, "Breakfast", 350, ChargeCategory.Breakfast, 2));
            lineItems.Add(FolioLineItem.Create(f.Id, "Spa Treatment", 800, ChargeCategory.Other, 1));
        }
        ctx.FolioLineItems.AddRange(lineItems);
        await ctx.SaveChangesAsync();

        // ─── Payments (1000 — one per folio) ────────────────────────
        var payments = new List<Payment>();
        for (int i = 0; i < folios.Count; i++)
        {
            var f = folios[i];
            payments.Add(Payment.Create(f.PropertyId, f.Id, 3000, (PaymentMethod)(i % 4), $"TXN-{i:D6}", bookings[i].Id));
        }
        ctx.Payments.AddRange(payments);
        await ctx.SaveChangesAsync();

        // ─── Housekeeping Tasks (1000+) ─────────────────────────────
        var propARooms = rooms.Where(r => r.PropertyId == PropertyA_Id).Take(500).ToList();
        var propBRooms = rooms.Where(r => r.PropertyId == PropertyB_Id).Take(500).ToList();
        var hkTasks = new List<HousekeepingTask>();
        for (int i = 0; i < 500; i++)
        {
            hkTasks.Add(HousekeepingTask.Create(PropertyA_Id, propARooms[i % propARooms.Count].Id, (HousekeepingTaskType)(i % 3), DateTime.UtcNow.AddHours(i), (HousekeepingPriority)(i % 4)));
            hkTasks.Add(HousekeepingTask.Create(PropertyB_Id, propBRooms[i % propBRooms.Count].Id, (HousekeepingTaskType)(i % 3), DateTime.UtcNow.AddHours(i), (HousekeepingPriority)(i % 4)));
        }
        ctx.HousekeepingTasks.AddRange(hkTasks);
        await ctx.SaveChangesAsync();

        // ─── Users (1000+) ──────────────────────────────────────────
        var users = new List<ApplicationUser>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            // Use a single pre-hashed password for all test users (BCrypt is slow)
            var hash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!");
            for (int i = 0; i < 500; i++)
                users.Add(ApplicationUser.Create(propId, $"user{i}@prop{p}.com", hash, $"User{i}", $"Staff{i}", $"+2712000{i:D4}"));
        }
        ctx.ApplicationUsers.AddRange(users);
        await ctx.SaveChangesAsync();

        // ─── Staff Members (1000+) ──────────────────────────────────
        var staffMembers = new List<StaffMember>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            for (int i = 0; i < 500; i++)
                staffMembers.Add(StaffMember.Create(propId, $"staff{i}@prop{p}.com", $"Staff{i}", $"Member{i}", (StaffRole)(i % 6)));
        }
        ctx.StaffMembers.AddRange(staffMembers);
        await ctx.SaveChangesAsync();

        // ─── RFID Cards (1000+) — set shadow FK via Entry API ────────
        var rfidCards = new List<RfidCard>();
        for (int i = 0; i < staffMembers.Count; i++)
        {
            var sm = staffMembers[i];
            var card = RfidCard.Create(sm.Id, $"RFID{i:D8}", RfidCardType.Card, sm.PropertyId);
            rfidCards.Add(card);
        }
        ctx.RfidCards.AddRange(rfidCards);
        // Set the shadow FK property StaffMemberId that EF expects
        for (int i = 0; i < rfidCards.Count; i++)
            ctx.Entry(rfidCards[i]).Property("StaffMemberId").CurrentValue = staffMembers[i].Id;
        await ctx.SaveChangesAsync();

        // ─── RFID Readers (100 per property = 200) ──────────────────
        var rfidReaders = new List<RfidReader>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            for (int i = 0; i < 100; i++)
                rfidReaders.Add(RfidReader.Create(propId, $"SN-{p}{i:D4}", $"Reader-{p}-{i}", RfidReaderType.Fixed, $"APIKEY-{p}-{i:D4}"));
        }
        ctx.RfidReaders.AddRange(rfidReaders);
        await ctx.SaveChangesAsync();

        // ─── Notifications (1000+) ──────────────────────────────────
        var notifications = new List<Notification>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            for (int i = 0; i < 500; i++)
                notifications.Add(Notification.Create(propId, $"recipient{i}@test.com", NotificationChannel.Email, NotificationType.BookingConfirmation, $"Subject {i}", $"Body {i}"));
        }
        ctx.Notifications.AddRange(notifications);
        await ctx.SaveChangesAsync();

        // ─── Audit Logs (1000+) ─────────────────────────────────────
        var auditLogs = new List<AuditLog>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            for (int i = 0; i < 500; i++)
                auditLogs.Add(AuditLog.Create($"Action-{i}", "Booking", Guid.NewGuid(), Guid.NewGuid(), $"system-{i}", $"Old-{i}", $"New-{i}", propId, "127.0.0.1"));
        }
        ctx.AuditLogs.AddRange(auditLogs);
        await ctx.SaveChangesAsync();

        // ─── Cancellation Policies (1000 — 500 per property) ────────
        // Unique: (PropertyId, Name)
        var cancellationPolicies = new List<CancellationPolicy>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            for (int i = 0; i < 500; i++)
                cancellationPolicies.Add(CancellationPolicy.Create(propId, $"CancelPolicy-{p}-{i}", 24 + (i % 72), i % 100, i == 0));
        }
        ctx.CancellationPolicies.AddRange(cancellationPolicies);
        await ctx.SaveChangesAsync();

        // ─── Amenities (1000 — 500 per property) ────────────────────
        // Unique: (PropertyId, Name)
        var amenities = new List<Amenity>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            for (int i = 0; i < 500; i++)
                amenities.Add(Amenity.Create(propId, $"Amenity-{p}-{i}", (AmenityCategory)(i % 8)));
        }
        ctx.Amenities.AddRange(amenities);
        await ctx.SaveChangesAsync();

        // ─── Room Type Amenities (1000 — 25 per room type × 40 types)
        // Unique: (RoomTypeId, AmenityId)
        var roomTypeAmenities = new List<RoomTypeAmenity>();
        foreach (var rt in roomTypes)
        {
            var propAmenities = amenities.Where(a => a.PropertyId == rt.PropertyId).ToList();
            for (int i = 0; i < 25 && i < propAmenities.Count; i++)
                roomTypeAmenities.Add(RoomTypeAmenity.Create(rt.Id, propAmenities[i].Id));
        }
        ctx.RoomTypeAmenities.AddRange(roomTypeAmenities);
        await ctx.SaveChangesAsync();

        // ─── Rates (1000 — 500 per property) ────────────────────────
        var rateEntities = new List<Rate>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            var propRoomTypes = roomTypes.Where(rt => rt.PropertyId == propId).ToList();
            var propRatePlans = ratePlans.Where(rp => rp.PropertyId == propId).ToList();
            var propSeasons = seasons.Where(s => s.PropertyId == propId).ToList();
            for (int i = 0; i < 500; i++)
            {
                var rt = propRoomTypes[i % propRoomTypes.Count];
                var rp = propRatePlans[i % propRatePlans.Count];
                var season = propSeasons[i % propSeasons.Count];
                var from = DateTime.UtcNow.AddDays(i);
                rateEntities.Add(Rate.Create(propId, rt.Id, rp.Id, 500 + (i * 10), from, from.AddDays(7), season.Id));
            }
        }
        ctx.Rates.AddRange(rateEntities);
        await ctx.SaveChangesAsync();

        // ─── Booking Rooms (1000 — 500 per property) ────────────────
        var bookingRoomEntities = new List<BookingRoom>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            var propBookings = bookings.Where(b => b.PropertyId == propId).ToList();
            var propRoomsForBr = rooms.Where(r => r.PropertyId == propId).ToList();
            for (int i = 0; i < 500; i++)
            {
                var bk = propBookings[i % propBookings.Count];
                var rm = propRoomsForBr[i % propRoomsForBr.Count];
                bookingRoomEntities.Add(BookingRoom.Create(bk.Id, rm.Id, rm.RoomTypeId, 1500 + (i % 500)));
            }
        }
        ctx.BookingRooms.AddRange(bookingRoomEntities);
        await ctx.SaveChangesAsync();

        // ─── Room Blocks (1000 — 500 per property) ──────────────────
        var roomBlocks = new List<RoomBlock>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            var propRoomsForBlk = rooms.Where(r => r.PropertyId == propId).Take(500).ToList();
            for (int i = 0; i < 500; i++)
            {
                var rm = propRoomsForBlk[i % propRoomsForBlk.Count];
                var start = DateTime.UtcNow.AddDays(i * 2);
                roomBlocks.Add(RoomBlock.Create(propId, rm.Id, start, start.AddDays(1), (RoomBlockReason)(i % 6)));
            }
        }
        ctx.RoomBlocks.AddRange(roomBlocks);
        await ctx.SaveChangesAsync();

        // ─── Guest Preferences (1000) ───────────────────────────────
        // Unique: (GuestId, Category, Key) — each guest used once, so triple is unique
        var guestPreferences = new List<GuestPreference>();
        for (int i = 0; i < 1000; i++)
            guestPreferences.Add(GuestPreference.Create(guests[i].Id, (PreferenceCategory)(i % 8), $"pref-key-{i}", $"pref-value-{i}"));
        ctx.GuestPreferences.AddRange(guestPreferences);
        await ctx.SaveChangesAsync();

        // ─── Guest Loyalties (1000 — one per guest, 1:1) ───────────
        // Unique: GuestId (one-to-one relationship)
        var guestLoyalties = new List<GuestLoyalty>();
        for (int i = 0; i < 1000; i++)
            guestLoyalties.Add(GuestLoyalty.Create(guests[i].Id));
        ctx.GuestLoyalties.AddRange(guestLoyalties);
        await ctx.SaveChangesAsync();

        // ─── Invoices (1000 — one per folio) ────────────────────────
        // Unique: InvoiceNumber
        var invoiceEntities = new List<Invoice>();
        for (int i = 0; i < folios.Count; i++)
        {
            var f = folios[i];
            invoiceEntities.Add(Invoice.Create(f.PropertyId, f.Id, bookings[i].GuestId,
                $"INV-{i:D6}", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1500m, 225m, 15m));
        }
        ctx.Invoices.AddRange(invoiceEntities);
        await ctx.SaveChangesAsync();

        // ─── User Roles (1000 — one role per user) ─────────────────
        // Unique: (UserId, RoleId) — each user appears once, so pair is unique
        var seededRoles = await ctx.Roles.ToListAsync();
        var userRoleEntities = new List<UserRole>();
        for (int i = 0; i < users.Count; i++)
            userRoleEntities.Add(UserRole.Create(users[i].Id, seededRoles[i % seededRoles.Count].Id));
        ctx.UserRoles.AddRange(userRoleEntities);
        await ctx.SaveChangesAsync();

        // ─── Staff Attendance (1000 — 500 per property) ─────────────
        var staffAttendances = new List<StaffAttendance>();
        for (int p = 0; p < 2; p++)
        {
            var propId = p == 0 ? PropertyA_Id : PropertyB_Id;
            var propStaff = staffMembers.Where(s => s.PropertyId == propId).ToList();
            var propReadersForAtt = rfidReaders.Where(r => r.PropertyId == propId).ToList();
            var propCards = rfidCards.Where(c => c.PropertyId == propId).ToList();
            for (int i = 0; i < 500; i++)
            {
                var sm = propStaff[i % propStaff.Count];
                var reader = propReadersForAtt[i % propReadersForAtt.Count];
                var card = propCards[i % propCards.Count];
                staffAttendances.Add(StaffAttendance.CheckIn(sm.Id, propId, card.CardUid, reader.Id,
                    (ShiftType)(i % 4), 8 + (i % 4), 150 + (i % 50)));
            }
        }
        ctx.StaffAttendances.AddRange(staffAttendances);
        await ctx.SaveChangesAsync();

        sw.Stop();
        SeedDuration = sw.Elapsed;
    }

    /// <summary>Creates a fresh DbContext sharing the same database (for multi-context concurrency tests)</summary>
    public ApplicationDbContext CreateFreshContext() => new ApplicationDbContext(Options, _tenantProvider);

    public async Task DisposeAsync()
    {
        if (Context != null)
            await Context.DisposeAsync();
        
        if (string.IsNullOrEmpty(_databaseName))
            return;

        try
        {
            // Drop the test database
            using var adminContext = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseNpgsql("Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=Morven-05;Trust Server Certificate=true")
                    .Options);
            // Drop the test database — database name is generated internally, not from user input
            #pragma warning disable EF1002
            await adminContext.Database.ExecuteSqlRawAsync($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE);");
            #pragma warning restore EF1002
            await adminContext.DisposeAsync();
        }
        catch
        {
            // Best-effort cleanup — don't fail tests on teardown
        }
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }
