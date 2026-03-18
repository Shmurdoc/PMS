using Microsoft.EntityFrameworkCore;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Modules.Staff.Domain.Entities;
using SAFARIstack.Shared.Domain;
using SAFARIstack.Tests.Integration.Infrastructure;
using System.Diagnostics;

namespace SAFARIstack.Tests.Integration.Stress;

/// <summary>
/// Comprehensive bulk-insert tests verifying 1000+ records in ALL 31 database tables.
/// Covers the 11 tables not previously tested: CancellationPolicies, Amenities,
/// RoomTypeAmenities, Rates, BookingRooms, RoomBlocks, GuestPreferences, GuestLoyalties,
/// Invoices, UserRoles, and StaffAttendances. Also verifies previously-seeded-but-untested
/// tables (Seasons, RatePlans, Roles, Permissions, RolePermissions).
///
/// Tests include: record counts, FK integrity, unique constraint validation,
/// bulk read performance, and cross-table join correctness.
/// </summary>
[Collection("Database")]
public class CompleteBulkInsertTests
{
    private readonly DatabaseFixture _fixture;
    private readonly Guid _propAId;
    private readonly Guid _propBId;

    public CompleteBulkInsertTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _propAId = fixture.PropertyA_Id;
        _propBId = fixture.PropertyB_Id;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  RECORD COUNTS — 11 Newly Seeded Tables
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CancellationPolicies_Has1000Records()
    {
        var count = await _fixture.Context.CancellationPolicies.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000, "we seeded 500 per property");
    }

    [Fact]
    public async Task Amenities_Has1000Records()
    {
        var count = await _fixture.Context.Amenities.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000, "we seeded 500 per property");
    }

    [Fact]
    public async Task RoomTypeAmenities_Has1000Records()
    {
        var count = await _fixture.Context.RoomTypeAmenities.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000, "we seeded 25 per room type × 40 room types");
    }

    [Fact]
    public async Task Rates_Has1000Records()
    {
        var count = await _fixture.Context.Rates.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000, "we seeded 500 per property");
    }

    [Fact]
    public async Task BookingRooms_Has1000Records()
    {
        var count = await _fixture.Context.BookingRooms.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000, "we seeded 500 per property");
    }

    [Fact]
    public async Task RoomBlocks_Has1000Records()
    {
        var count = await _fixture.Context.RoomBlocks.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000, "we seeded 500 per property");
    }

    [Fact]
    public async Task GuestPreferences_Has1000Records()
    {
        var count = await _fixture.Context.GuestPreferences.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000, "we seeded 1000 guest preferences");
    }

    [Fact]
    public async Task GuestLoyalties_Has1000Records()
    {
        var count = await _fixture.Context.GuestLoyalties.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000, "we seeded 1000 guest loyalty records (1:1)");
    }

    [Fact]
    public async Task Invoices_Has1000Records()
    {
        var count = await _fixture.Context.Invoices.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000, "we seeded one invoice per folio");
    }

    [Fact]
    public async Task UserRoles_Has1000Records()
    {
        var count = await _fixture.Context.UserRoles.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000, "we seeded one role assignment per user");
    }

    [Fact]
    public async Task StaffAttendances_Has1000Records()
    {
        var count = await _fixture.Context.StaffAttendances.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000, "we seeded 500 per property");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  RECORD COUNTS — Previously Seeded but Not Count-Tested Tables
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Seasons_Has100Records()
    {
        var count = await _fixture.Context.Seasons.CountAsync();
        count.Should().BeGreaterOrEqualTo(100, "we seeded 50 per property");
    }

    [Fact]
    public async Task RatePlans_Has200Records()
    {
        var count = await _fixture.Context.RatePlans.CountAsync();
        count.Should().BeGreaterOrEqualTo(200, "we seeded 100 per property");
    }

    [Fact]
    public async Task Roles_HasSeededSystemRoles()
    {
        var count = await _fixture.Context.Roles.CountAsync();
        count.Should().BeGreaterOrEqualTo(7, "7 system roles seeded via HasData");
    }

    [Fact]
    public async Task Permissions_HasSeededPermissions()
    {
        var count = await _fixture.Context.Permissions.CountAsync();
        count.Should().BeGreaterOrEqualTo(20, "all permission constants seeded via HasData");
    }

    [Fact]
    public async Task RolePermissions_HasSeededMappings()
    {
        var count = await _fixture.Context.RolePermissions.CountAsync();
        count.Should().BeGreaterThan(0, "role-permission mappings seeded via HasData");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  COMPREHENSIVE — All 31 Tables Summary
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AllTables_HaveExpectedRecordCounts()
    {
        var ctx = _fixture.Context;
        var counts = new Dictionary<string, int>
        {
            ["Properties"] = await ctx.Properties.CountAsync(),
            ["RoomTypes"] = await ctx.RoomTypes.CountAsync(),
            ["Rooms"] = await ctx.Rooms.CountAsync(),
            ["Guests"] = await ctx.Guests.CountAsync(),
            ["Seasons"] = await ctx.Seasons.CountAsync(),
            ["RatePlans"] = await ctx.RatePlans.CountAsync(),
            ["Bookings"] = await ctx.Bookings.IgnoreQueryFilters().CountAsync(),
            ["Folios"] = await ctx.Folios.CountAsync(),
            ["FolioLineItems"] = await ctx.FolioLineItems.CountAsync(),
            ["Payments"] = await ctx.Payments.CountAsync(),
            ["HousekeepingTasks"] = await ctx.HousekeepingTasks.CountAsync(),
            ["ApplicationUsers"] = await ctx.ApplicationUsers.CountAsync(),
            ["StaffMembers"] = await ctx.StaffMembers.CountAsync(),
            ["RfidCards"] = await ctx.RfidCards.CountAsync(),
            ["RfidReaders"] = await ctx.RfidReaders.CountAsync(),
            ["Notifications"] = await ctx.Notifications.CountAsync(),
            ["AuditLogs"] = await ctx.AuditLogs.CountAsync(),
            ["CancellationPolicies"] = await ctx.CancellationPolicies.CountAsync(),
            ["Amenities"] = await ctx.Amenities.CountAsync(),
            ["RoomTypeAmenities"] = await ctx.RoomTypeAmenities.CountAsync(),
            ["Rates"] = await ctx.Rates.CountAsync(),
            ["BookingRooms"] = await ctx.BookingRooms.CountAsync(),
            ["RoomBlocks"] = await ctx.RoomBlocks.CountAsync(),
            ["GuestPreferences"] = await ctx.GuestPreferences.CountAsync(),
            ["GuestLoyalties"] = await ctx.GuestLoyalties.CountAsync(),
            ["Invoices"] = await ctx.Invoices.CountAsync(),
            ["UserRoles"] = await ctx.UserRoles.CountAsync(),
            ["StaffAttendances"] = await ctx.StaffAttendances.CountAsync(),
            ["Roles"] = await ctx.Roles.CountAsync(),
            ["Permissions"] = await ctx.Permissions.CountAsync(),
            ["RolePermissions"] = await ctx.RolePermissions.CountAsync(),
        };

        // Verify ALL 31 tables have data
        counts.Should().HaveCount(31, "there should be 31 tables in the system");
        foreach (var kv in counts)
            kv.Value.Should().BeGreaterThan(0, $"table '{kv.Key}' should not be empty");

        // Verify key tables have 1000+ records
        var thousandPlusTables = new[]
        {
            "Rooms", "Guests", "Bookings", "Folios", "FolioLineItems", "Payments",
            "HousekeepingTasks", "ApplicationUsers", "StaffMembers", "RfidCards",
            "Notifications", "AuditLogs",
            "CancellationPolicies", "Amenities", "RoomTypeAmenities", "Rates",
            "BookingRooms", "RoomBlocks", "GuestPreferences", "GuestLoyalties",
            "Invoices", "UserRoles", "StaffAttendances"
        };

        foreach (var table in thousandPlusTables)
            counts[table].Should().BeGreaterOrEqualTo(1000, $"'{table}' should have ≥1000 records for stress testing");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  FOREIGN KEY INTEGRITY
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CancellationPolicies_AllHaveValidProperty()
    {
        var propertyIds = await _fixture.Context.Properties.Select(p => p.Id).ToListAsync();
        var orphans = await _fixture.Context.CancellationPolicies
            .Where(cp => !propertyIds.Contains(cp.PropertyId))
            .CountAsync();
        orphans.Should().Be(0, "all cancellation policies must reference a valid property");
    }

    [Fact]
    public async Task Amenities_AllHaveValidProperty()
    {
        var propertyIds = await _fixture.Context.Properties.Select(p => p.Id).ToListAsync();
        var orphans = await _fixture.Context.Amenities
            .Where(a => !propertyIds.Contains(a.PropertyId))
            .CountAsync();
        orphans.Should().Be(0, "all amenities must reference a valid property");
    }

    [Fact]
    public async Task RoomTypeAmenities_AllHaveValidForeignKeys()
    {
        // Check RoomType FK
        var roomTypeIds = await _fixture.Context.RoomTypes.Select(rt => rt.Id).ToListAsync();
        var rtOrphans = await _fixture.Context.RoomTypeAmenities
            .Where(rta => !roomTypeIds.Contains(rta.RoomTypeId))
            .CountAsync();
        rtOrphans.Should().Be(0, "all room-type amenities must reference a valid room type");

        // Check Amenity FK
        var amenityIds = await _fixture.Context.Amenities.Select(a => a.Id).ToListAsync();
        var amenityOrphans = await _fixture.Context.RoomTypeAmenities
            .Where(rta => !amenityIds.Contains(rta.AmenityId))
            .CountAsync();
        amenityOrphans.Should().Be(0, "all room-type amenities must reference a valid amenity");
    }

    [Fact]
    public async Task Rates_AllHaveValidForeignKeys()
    {
        var roomTypeIds = await _fixture.Context.RoomTypes.Select(rt => rt.Id).ToListAsync();
        var ratePlanIds = await _fixture.Context.RatePlans.Select(rp => rp.Id).ToListAsync();

        var rtOrphans = await _fixture.Context.Rates
            .Where(r => !roomTypeIds.Contains(r.RoomTypeId))
            .CountAsync();
        rtOrphans.Should().Be(0, "all rates must reference a valid room type");

        var rpOrphans = await _fixture.Context.Rates
            .Where(r => !ratePlanIds.Contains(r.RatePlanId))
            .CountAsync();
        rpOrphans.Should().Be(0, "all rates must reference a valid rate plan");
    }

    [Fact]
    public async Task BookingRooms_AllHaveValidForeignKeys()
    {
        var bookingIds = await _fixture.Context.Bookings.IgnoreQueryFilters().Select(b => b.Id).ToListAsync();
        var roomIds = await _fixture.Context.Rooms.Select(r => r.Id).ToListAsync();

        var bkOrphans = await _fixture.Context.BookingRooms
            .Where(br => !bookingIds.Contains(br.BookingId))
            .CountAsync();
        bkOrphans.Should().Be(0, "all booking rooms must reference a valid booking");

        var rmOrphans = await _fixture.Context.BookingRooms
            .Where(br => !roomIds.Contains(br.RoomId))
            .CountAsync();
        rmOrphans.Should().Be(0, "all booking rooms must reference a valid room");
    }

    [Fact]
    public async Task RoomBlocks_AllHaveValidRoomAndProperty()
    {
        var roomIds = await _fixture.Context.Rooms.Select(r => r.Id).ToListAsync();
        var orphans = await _fixture.Context.RoomBlocks
            .Where(rb => !roomIds.Contains(rb.RoomId))
            .CountAsync();
        orphans.Should().Be(0, "all room blocks must reference a valid room");

        // All blocks should have valid date ranges
        var invalidRanges = await _fixture.Context.RoomBlocks
            .Where(rb => rb.EndDate <= rb.StartDate)
            .CountAsync();
        invalidRanges.Should().Be(0, "all room blocks must have EndDate > StartDate");
    }

    [Fact]
    public async Task GuestPreferences_AllHaveValidGuest()
    {
        var guestIds = await _fixture.Context.Guests.Select(g => g.Id).ToListAsync();
        var orphans = await _fixture.Context.GuestPreferences
            .Where(gp => !guestIds.Contains(gp.GuestId))
            .CountAsync();
        orphans.Should().Be(0, "all guest preferences must reference a valid guest");
    }

    [Fact]
    public async Task GuestLoyalties_AllHaveValidGuest()
    {
        var guestIds = await _fixture.Context.Guests.Select(g => g.Id).ToListAsync();
        var orphans = await _fixture.Context.GuestLoyalties
            .Where(gl => !guestIds.Contains(gl.GuestId))
            .CountAsync();
        orphans.Should().Be(0, "all guest loyalty records must reference a valid guest");
    }

    [Fact]
    public async Task Invoices_AllHaveValidForeignKeys()
    {
        var folioIds = await _fixture.Context.Folios.Select(f => f.Id).ToListAsync();
        var guestIds = await _fixture.Context.Guests.Select(g => g.Id).ToListAsync();

        var folioOrphans = await _fixture.Context.Invoices
            .Where(inv => !folioIds.Contains(inv.FolioId))
            .CountAsync();
        folioOrphans.Should().Be(0, "all invoices must reference a valid folio");

        var guestOrphans = await _fixture.Context.Invoices
            .Where(inv => !guestIds.Contains(inv.GuestId))
            .CountAsync();
        guestOrphans.Should().Be(0, "all invoices must reference a valid guest");
    }

    [Fact]
    public async Task UserRoles_AllHaveValidUserAndRole()
    {
        var userIds = await _fixture.Context.ApplicationUsers.Select(u => u.Id).ToListAsync();
        var roleIds = await _fixture.Context.Roles.Select(r => r.Id).ToListAsync();

        var userOrphans = await _fixture.Context.UserRoles
            .Where(ur => !userIds.Contains(ur.UserId))
            .CountAsync();
        userOrphans.Should().Be(0, "all user roles must reference a valid user");

        var roleOrphans = await _fixture.Context.UserRoles
            .Where(ur => !roleIds.Contains(ur.RoleId))
            .CountAsync();
        roleOrphans.Should().Be(0, "all user roles must reference a valid role");
    }

    [Fact]
    public async Task StaffAttendances_AllHaveValidForeignKeys()
    {
        var staffIds = await _fixture.Context.StaffMembers.Select(sm => sm.Id).ToListAsync();
        var readerIds = await _fixture.Context.RfidReaders.Select(r => r.Id).ToListAsync();

        var staffOrphans = await _fixture.Context.StaffAttendances
            .Where(sa => !staffIds.Contains(sa.StaffId))
            .CountAsync();
        staffOrphans.Should().Be(0, "all attendance records must reference a valid staff member");

        // ReaderId is nullable, so only check non-null entries
        var readerOrphans = await _fixture.Context.StaffAttendances
            .Where(sa => sa.ReaderId.HasValue && !readerIds.Contains(sa.ReaderId.Value))
            .CountAsync();
        readerOrphans.Should().Be(0, "all attendance reader references must be valid");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  UNIQUE CONSTRAINT VERIFICATION
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CancellationPolicies_PropertyNamePairsAreUnique()
    {
        var totalCount = await _fixture.Context.CancellationPolicies.CountAsync();
        var distinctCount = await _fixture.Context.CancellationPolicies
            .Select(cp => new { cp.PropertyId, cp.Name })
            .Distinct()
            .CountAsync();
        distinctCount.Should().Be(totalCount, "each (PropertyId, Name) pair must be unique");
    }

    [Fact]
    public async Task Amenities_PropertyNamePairsAreUnique()
    {
        var totalCount = await _fixture.Context.Amenities.CountAsync();
        var distinctCount = await _fixture.Context.Amenities
            .Select(a => new { a.PropertyId, a.Name })
            .Distinct()
            .CountAsync();
        distinctCount.Should().Be(totalCount, "each (PropertyId, Name) pair must be unique");
    }

    [Fact]
    public async Task RoomTypeAmenities_PairsAreUnique()
    {
        var totalCount = await _fixture.Context.RoomTypeAmenities.CountAsync();
        var distinctCount = await _fixture.Context.RoomTypeAmenities
            .Select(rta => new { rta.RoomTypeId, rta.AmenityId })
            .Distinct()
            .CountAsync();
        distinctCount.Should().Be(totalCount, "each (RoomTypeId, AmenityId) pair must be unique");
    }

    [Fact]
    public async Task GuestPreferences_TripleIsUnique()
    {
        var totalCount = await _fixture.Context.GuestPreferences.CountAsync();
        var distinctCount = await _fixture.Context.GuestPreferences
            .Select(gp => new { gp.GuestId, gp.Category, gp.Key })
            .Distinct()
            .CountAsync();
        distinctCount.Should().Be(totalCount, "each (GuestId, Category, Key) triple must be unique");
    }

    [Fact]
    public async Task GuestLoyalties_OnePerGuest()
    {
        var totalCount = await _fixture.Context.GuestLoyalties.CountAsync();
        var distinctGuestCount = await _fixture.Context.GuestLoyalties
            .Select(gl => gl.GuestId)
            .Distinct()
            .CountAsync();
        distinctGuestCount.Should().Be(totalCount, "each guest can have at most one loyalty record (1:1)");
    }

    [Fact]
    public async Task Invoices_NumbersAreUnique()
    {
        var totalCount = await _fixture.Context.Invoices.CountAsync();
        var distinctCount = await _fixture.Context.Invoices
            .Select(inv => inv.InvoiceNumber)
            .Distinct()
            .CountAsync();
        distinctCount.Should().Be(totalCount, "each InvoiceNumber must be globally unique");
    }

    [Fact]
    public async Task UserRoles_PairsAreUnique()
    {
        var totalCount = await _fixture.Context.UserRoles.CountAsync();
        var distinctCount = await _fixture.Context.UserRoles
            .Select(ur => new { ur.UserId, ur.RoleId })
            .Distinct()
            .CountAsync();
        distinctCount.Should().Be(totalCount, "each (UserId, RoleId) pair must be unique");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  BULK READ PERFORMANCE
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AllNewTables_BulkReadUnder3Seconds()
    {
        var sw = Stopwatch.StartNew();

        // Read all 11 new tables in a single method
        var cancellationPolicies = await _fixture.Context.CancellationPolicies.ToListAsync();
        var amenities = await _fixture.Context.Amenities.ToListAsync();
        var roomTypeAmenities = await _fixture.Context.RoomTypeAmenities.ToListAsync();
        var rates = await _fixture.Context.Rates.ToListAsync();
        var bookingRooms = await _fixture.Context.BookingRooms.ToListAsync();
        var roomBlocks = await _fixture.Context.RoomBlocks.ToListAsync();
        var guestPreferences = await _fixture.Context.GuestPreferences.ToListAsync();
        var guestLoyalties = await _fixture.Context.GuestLoyalties.ToListAsync();
        var invoices = await _fixture.Context.Invoices.ToListAsync();
        var userRoles = await _fixture.Context.UserRoles.ToListAsync();
        var staffAttendances = await _fixture.Context.StaffAttendances.ToListAsync();

        sw.Stop();

        sw.ElapsedMilliseconds.Should().BeLessThan(3000,
            "bulk reading 11,000+ records from 11 tables should complete in under 3 seconds");

        // Verify all reads returned data
        cancellationPolicies.Should().HaveCountGreaterOrEqualTo(1000);
        amenities.Should().HaveCountGreaterOrEqualTo(1000);
        roomTypeAmenities.Should().HaveCountGreaterOrEqualTo(1000);
        rates.Should().HaveCountGreaterOrEqualTo(1000);
        bookingRooms.Should().HaveCountGreaterOrEqualTo(1000);
        roomBlocks.Should().HaveCountGreaterOrEqualTo(1000);
        guestPreferences.Should().HaveCountGreaterOrEqualTo(1000);
        guestLoyalties.Should().HaveCountGreaterOrEqualTo(1000);
        invoices.Should().HaveCountGreaterOrEqualTo(1000);
        userRoles.Should().HaveCountGreaterOrEqualTo(1000);
        staffAttendances.Should().HaveCountGreaterOrEqualTo(1000);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CROSS-TABLE JOINS
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BookingRooms_JoinWithBookingAndRoom()
    {
        var results = await _fixture.Context.BookingRooms
            .Include(br => br.Booking)
            .Include(br => br.Room)
            .Include(br => br.RoomType)
            .Take(100)
            .ToListAsync();

        results.Should().NotBeEmpty();
        foreach (var br in results)
        {
            br.Booking.Should().NotBeNull("BookingRoom must have a Booking navigation");
            br.Room.Should().NotBeNull("BookingRoom must have a Room navigation");
            br.RoomType.Should().NotBeNull("BookingRoom must have a RoomType navigation");
            br.RateApplied.Should().BeGreaterThan(0, "rate applied must be positive");
        }
    }

    [Fact]
    public async Task Invoices_JoinWithFolioAndGuest()
    {
        var results = await _fixture.Context.Invoices
            .Include(inv => inv.Folio)
            .Include(inv => inv.Guest)
            .Take(100)
            .ToListAsync();

        results.Should().NotBeEmpty();
        foreach (var inv in results)
        {
            inv.Folio.Should().NotBeNull("Invoice must have a Folio navigation");
            inv.Guest.Should().NotBeNull("Invoice must have a Guest navigation");
            inv.InvoiceNumber.Should().NotBeNullOrWhiteSpace("Invoice must have a number");
            inv.TotalAmount.Should().BeGreaterThan(0, "total must be positive");
        }
    }

    [Fact]
    public async Task Rates_JoinWithRoomTypeAndRatePlan()
    {
        var results = await _fixture.Context.Rates
            .Include(r => r.RoomType)
            .Include(r => r.RatePlan)
            .Include(r => r.Season)
            .Take(100)
            .ToListAsync();

        results.Should().NotBeEmpty();
        foreach (var rate in results)
        {
            rate.RoomType.Should().NotBeNull("Rate must have a RoomType navigation");
            rate.RatePlan.Should().NotBeNull("Rate must have a RatePlan navigation");
            rate.AmountPerNight.Should().BeGreaterThan(0, "rate must be positive");
            rate.EffectiveTo.Should().BeAfter(rate.EffectiveFrom, "effective date range must be valid");
        }
    }

    [Fact]
    public async Task StaffAttendances_JoinWithStaffAndReader()
    {
        var results = await _fixture.Context.StaffAttendances
            .Include(sa => sa.StaffMember)
            .Include(sa => sa.Reader)
            .Take(100)
            .ToListAsync();

        results.Should().NotBeEmpty();
        foreach (var att in results)
        {
            att.StaffMember.Should().NotBeNull("Attendance must have a StaffMember navigation");
            att.CardUid.Should().NotBeNullOrWhiteSpace("Attendance must have a CardUid");
            att.HourlyRate.Should().BeGreaterThan(0, "hourly rate must be positive");
            att.Status.Should().Be(AttendanceStatus.CheckedIn, "newly created attendance should be CheckedIn");
        }
    }

    [Fact]
    public async Task GuestWithPreferencesAndLoyalty_JoinWorks()
    {
        var guests = await _fixture.Context.Guests
            .Include(g => g.Preferences)
            .Include(g => g.Loyalty)
            .Where(g => g.Loyalty != null)
            .Take(50)
            .ToListAsync();

        guests.Should().NotBeEmpty();
        foreach (var guest in guests)
        {
            guest.Loyalty.Should().NotBeNull("guest with loyalty join should have loyalty");
            guest.Loyalty!.GuestId.Should().Be(guest.Id, "loyalty GuestId must match guest Id");
        }
    }

    [Fact]
    public async Task UserRoles_JoinWithUserAndRole()
    {
        var results = await _fixture.Context.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .Take(100)
            .ToListAsync();

        results.Should().NotBeEmpty();
        foreach (var ur in results)
        {
            ur.User.Should().NotBeNull("UserRole must have a User navigation");
            ur.Role.Should().NotBeNull("UserRole must have a Role navigation");
            ur.Role.Name.Should().NotBeNullOrWhiteSpace("Role must have a name");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DATA INTEGRITY & COMPLETENESS
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AllNewEntities_HaveNonEmptyIds()
    {
        // Spot-check that IDs are valid (not Guid.Empty)
        var firstPolicy = await _fixture.Context.CancellationPolicies.FirstAsync();
        firstPolicy.Id.Should().NotBe(Guid.Empty);

        var firstAmenity = await _fixture.Context.Amenities.FirstAsync();
        firstAmenity.Id.Should().NotBe(Guid.Empty);

        var firstRate = await _fixture.Context.Rates.FirstAsync();
        firstRate.Id.Should().NotBe(Guid.Empty);

        var firstInvoice = await _fixture.Context.Invoices.FirstAsync();
        firstInvoice.Id.Should().NotBe(Guid.Empty);

        var firstAttendance = await _fixture.Context.StaffAttendances.FirstAsync();
        firstAttendance.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CancellationPolicies_HaveCorrectPropertyDistribution()
    {
        var propACnt = await _fixture.Context.CancellationPolicies
            .Where(cp => cp.PropertyId == _propAId).CountAsync();
        var propBCnt = await _fixture.Context.CancellationPolicies
            .Where(cp => cp.PropertyId == _propBId).CountAsync();

        propACnt.Should().Be(500, "property A should have exactly 500 cancellation policies");
        propBCnt.Should().Be(500, "property B should have exactly 500 cancellation policies");
    }

    [Fact]
    public async Task Rates_HaveValidAmounts()
    {
        var invalidRates = await _fixture.Context.Rates
            .Where(r => r.AmountPerNight < 0)
            .CountAsync();
        invalidRates.Should().Be(0, "no rate should have a negative amount");
    }

    [Fact]
    public async Task Invoices_HaveValidFinancialData()
    {
        var invoices = await _fixture.Context.Invoices.Take(100).ToListAsync();
        foreach (var inv in invoices)
        {
            inv.SubtotalAmount.Should().BeGreaterThan(0);
            inv.VATAmount.Should().BeGreaterOrEqualTo(0);
            inv.TotalAmount.Should().BeGreaterThan(0);
            inv.TotalAmount.Should().Be(inv.SubtotalAmount + inv.VATAmount + inv.TourismLevyAmount,
                "total = subtotal + VAT + tourism levy");
        }
    }

    [Fact]
    public async Task StaffAttendances_HaveCorrectShiftData()
    {
        var attendances = await _fixture.Context.StaffAttendances.Take(100).ToListAsync();
        foreach (var att in attendances)
        {
            att.ScheduledHours.Should().BeGreaterThan(0, "scheduled hours must be positive");
            att.HourlyRate.Should().BeGreaterThan(0, "hourly rate must be positive");
            att.OvertimeRate.Should().Be(att.HourlyRate * 1.5m, "SA law: overtime = 1.5× hourly");
        }
    }

    [Fact]
    public async Task RoomBlocks_HaveValidDateRanges()
    {
        var blocks = await _fixture.Context.RoomBlocks.Take(100).ToListAsync();
        foreach (var block in blocks)
        {
            block.EndDate.Should().BeAfter(block.StartDate, "block end must be after start");
        }
    }

    [Fact]
    public async Task BookingRooms_AllHavePositiveRates()
    {
        var invalidRates = await _fixture.Context.BookingRooms
            .Where(br => br.RateApplied <= 0)
            .CountAsync();
        invalidRates.Should().Be(0, "all booking room rates must be positive");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  MULTI-TENANCY — Property Isolation
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task NewTables_HaveBalancedPropertyDistribution()
    {
        // Verify 50/50 split for all property-scoped new tables
        var amenityA = await _fixture.Context.Amenities.Where(a => a.PropertyId == _propAId).CountAsync();
        var amenityB = await _fixture.Context.Amenities.Where(a => a.PropertyId == _propBId).CountAsync();
        amenityA.Should().Be(500);
        amenityB.Should().Be(500);

        var rateA = await _fixture.Context.Rates.Where(r => r.PropertyId == _propAId).CountAsync();
        var rateB = await _fixture.Context.Rates.Where(r => r.PropertyId == _propBId).CountAsync();
        rateA.Should().Be(500);
        rateB.Should().Be(500);

        var blockA = await _fixture.Context.RoomBlocks.Where(rb => rb.PropertyId == _propAId).CountAsync();
        var blockB = await _fixture.Context.RoomBlocks.Where(rb => rb.PropertyId == _propBId).CountAsync();
        blockA.Should().Be(500);
        blockB.Should().Be(500);

        var attA = await _fixture.Context.StaffAttendances.Where(sa => sa.PropertyId == _propAId).CountAsync();
        var attB = await _fixture.Context.StaffAttendances.Where(sa => sa.PropertyId == _propBId).CountAsync();
        attA.Should().Be(500);
        attB.Should().Be(500);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  AGGREGATE QUERIES — Prove Complex SQL Works at Scale
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Rates_AveragePerPropertyIsReasonable()
    {
        var avgA = await _fixture.Context.Rates
            .Where(r => r.PropertyId == _propAId)
            .AverageAsync(r => r.AmountPerNight);

        var avgB = await _fixture.Context.Rates
            .Where(r => r.PropertyId == _propBId)
            .AverageAsync(r => r.AmountPerNight);

        avgA.Should().BeGreaterThan(0, "average rate for property A must be positive");
        avgB.Should().BeGreaterThan(0, "average rate for property B must be positive");
    }

    [Fact]
    public async Task Invoices_TotalAmountAggregation()
    {
        var totalInvoiced = await _fixture.Context.Invoices.SumAsync(inv => inv.TotalAmount);
        totalInvoiced.Should().BeGreaterThan(0, "total invoiced amount must be positive");

        var count = await _fixture.Context.Invoices.CountAsync();
        var avg = totalInvoiced / count;
        avg.Should().BeGreaterThan(1000, "average invoice should be > R1000 given our seed data");
    }

    [Fact]
    public async Task GuestLoyalties_TierDistributionQuery()
    {
        // All newly-created loyalties should be at None tier (0 stays)
        var tierCounts = await _fixture.Context.GuestLoyalties
            .GroupBy(gl => gl.Tier)
            .Select(g => new { Tier = g.Key, Count = g.Count() })
            .ToListAsync();

        tierCounts.Should().NotBeEmpty();
        var noneTier = tierCounts.FirstOrDefault(t => t.Tier == LoyaltyTier.None);
        noneTier.Should().NotBeNull("newly created loyalties should be at None tier");
        noneTier!.Count.Should().Be(1000, "all 1000 loyalty records should be None tier");
    }
}
