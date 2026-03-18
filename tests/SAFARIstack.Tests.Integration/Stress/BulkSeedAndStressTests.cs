using Microsoft.EntityFrameworkCore;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Modules.Staff.Domain.Entities;
using SAFARIstack.Shared.Domain;
using SAFARIstack.Tests.Integration.Infrastructure;
using System.Diagnostics;

namespace SAFARIstack.Tests.Integration.Stress;

/// <summary>
/// Stress tests seeding 1000+ records per entity table, then performing
/// bulk reads, concurrent writes, pagination, aggregation, and data integrity
/// validations. Designed to flush out N+1 queries, concurrency conflicts,
/// index bottlenecks, and data corruption under load.
/// </summary>
[Collection("Database")]
public class BulkSeedAndStressTests
{
    private readonly DatabaseFixture _fixture;
    private readonly Guid _propAId;
    private readonly Guid _propBId;

    public BulkSeedAndStressTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _propAId = fixture.PropertyA_Id;
        _propBId = fixture.PropertyB_Id;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  SEEDING VALIDATION
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void SeedingCompleted_UnderThreshold()
    {
        _fixture.SeedDuration.TotalSeconds.Should().BeLessThan(300, "bulk seeding should not take more than 5 minutes");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  STRESS TESTS — Record Counts
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Properties_Has2Records()
    {
        var count = await _fixture.Context.Properties.CountAsync();
        count.Should().Be(2);
    }

    [Fact]
    public async Task RoomTypes_Has40Records()
    {
        var count = await _fixture.Context.RoomTypes.CountAsync();
        count.Should().BeGreaterOrEqualTo(40);
    }

    [Fact]
    public async Task Rooms_Has1000PlusRecords()
    {
        var count = await _fixture.Context.Rooms.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000);
    }

    [Fact]
    public async Task Guests_Has1200Records()
    {
        var count = await _fixture.Context.Guests.CountAsync();
        count.Should().BeGreaterOrEqualTo(1200);
    }

    [Fact]
    public async Task Bookings_Has1000Records()
    {
        // Count ALL records (including soft-deleted) to verify seeding completeness
        var count = await _fixture.Context.Bookings.IgnoreQueryFilters().CountAsync();
        count.Should().BeGreaterOrEqualTo(1000);
    }

    [Fact]
    public async Task Folios_Has1000Records()
    {
        var count = await _fixture.Context.Folios.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000);
    }

    [Fact]
    public async Task FolioLineItems_Has3000Records()
    {
        var count = await _fixture.Context.FolioLineItems.CountAsync();
        count.Should().BeGreaterOrEqualTo(3000);
    }

    [Fact]
    public async Task Payments_Has1000Records()
    {
        var count = await _fixture.Context.Payments.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000);
    }

    [Fact]
    public async Task HousekeepingTasks_Has1000Records()
    {
        var count = await _fixture.Context.HousekeepingTasks.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000);
    }

    [Fact]
    public async Task Users_Has1000Records()
    {
        var count = await _fixture.Context.ApplicationUsers.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000);
    }

    [Fact]
    public async Task StaffMembers_Has1000Records()
    {
        var count = await _fixture.Context.StaffMembers.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000);
    }

    [Fact]
    public async Task RfidCards_Has1000Records()
    {
        var count = await _fixture.Context.RfidCards.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000);
    }

    [Fact]
    public async Task RfidReaders_Has200Records()
    {
        var count = await _fixture.Context.RfidReaders.CountAsync();
        count.Should().BeGreaterOrEqualTo(200);
    }

    [Fact]
    public async Task Notifications_Has1000Records()
    {
        var count = await _fixture.Context.Notifications.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000);
    }

    [Fact]
    public async Task AuditLogs_Has1000Records()
    {
        var count = await _fixture.Context.AuditLogs.CountAsync();
        count.Should().BeGreaterOrEqualTo(1000);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  PERFORMANCE — Bulk reads under threshold
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BulkRead_AllGuests_Under2Seconds()
    {
        var sw = Stopwatch.StartNew();
        var guests = await _fixture.Context.Guests.AsNoTracking().ToListAsync();
        sw.Stop();
        guests.Count.Should().BeGreaterOrEqualTo(1200);
        sw.Elapsed.TotalSeconds.Should().BeLessThan(2, "reading 1200 guests should be fast");
    }

    [Fact]
    public async Task BulkRead_AllBookings_Under2Seconds()
    {
        var sw = Stopwatch.StartNew();
        // Include all records (even soft-deleted) for accurate performance benchmark
        var bookings = await _fixture.Context.Bookings.IgnoreQueryFilters().AsNoTracking().ToListAsync();
        sw.Stop();
        bookings.Count.Should().BeGreaterOrEqualTo(1000);
        sw.Elapsed.TotalSeconds.Should().BeLessThan(2, "reading 1000 bookings should be fast");
    }

    [Fact]
    public async Task BulkRead_AllRooms_Under2Seconds()
    {
        var sw = Stopwatch.StartNew();
        var rooms = await _fixture.Context.Rooms.AsNoTracking().ToListAsync();
        sw.Stop();
        rooms.Count.Should().BeGreaterOrEqualTo(1000);
        sw.Elapsed.TotalSeconds.Should().BeLessThan(2);
    }

    [Fact]
    public async Task BulkRead_AllFolioLineItems_Under3Seconds()
    {
        var sw = Stopwatch.StartNew();
        var items = await _fixture.Context.FolioLineItems.AsNoTracking().ToListAsync();
        sw.Stop();
        items.Count.Should().BeGreaterOrEqualTo(3000);
        sw.Elapsed.TotalSeconds.Should().BeLessThan(3, "reading 3000 line items should complete quickly");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  PAGINATION — Skip/Take at scale
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(0, 50)]
    [InlineData(50, 50)]
    [InlineData(500, 50)]
    [InlineData(900, 50)]
    public async Task Pagination_Guests_ReturnsCorrectPage(int skip, int take)
    {
        var page = await _fixture.Context.Guests
            .AsNoTracking()
            .OrderBy(g => g.Id)
            .Skip(skip).Take(take)
            .ToListAsync();

        page.Count.Should().Be(take);
        page.Select(g => g.Id).Should().OnlyHaveUniqueItems("each page should have unique guests");
    }

    [Fact]
    public async Task Pagination_LastPage_ReturnsRemainder()
    {
        using var ctx = _fixture.CreateFreshContext();
        var total = await ctx.Guests.CountAsync();
        total.Should().BeGreaterThan(0, "guests must be seeded");
        var pageSize = 50;
        var remainder = total % pageSize;
        var lastPageSkip = remainder == 0 ? total - pageSize : total - remainder;
        var expectedCount = remainder == 0 ? pageSize : remainder;

        var page = await ctx.Guests
            .AsNoTracking()
            .OrderBy(g => g.Id)
            .Skip(lastPageSkip).Take(pageSize)
            .ToListAsync();

        page.Count.Should().Be(expectedCount);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DATA INTEGRITY — FK, uniqueness
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AllBookings_HaveValidGuestId()
    {
        var guestIds = (await _fixture.Context.Guests.Select(g => g.Id).ToListAsync()).ToHashSet();
        var bookingGuestIds = await _fixture.Context.Bookings.Select(b => b.GuestId).Distinct().ToListAsync();

        bookingGuestIds.Should().OnlyContain(id => guestIds.Contains(id),
            "every booking must reference a valid guest");
    }

    [Fact]
    public async Task AllBookings_HaveValidPropertyId()
    {
        var propIds = (await _fixture.Context.Properties.Select(p => p.Id).ToListAsync()).ToHashSet();
        var bookingPropIds = await _fixture.Context.Bookings.Select(b => b.PropertyId).Distinct().ToListAsync();

        bookingPropIds.Should().OnlyContain(id => propIds.Contains(id),
            "every booking must reference a valid property");
    }

    [Fact]
    public async Task AllFolios_HaveValidBookingId()
    {
        var bookingIds = (await _fixture.Context.Bookings.Select(b => b.Id).ToListAsync()).ToHashSet();
        var folioBookingIds = await _fixture.Context.Folios.Select(f => f.BookingId).Distinct().ToListAsync();

        folioBookingIds.Should().OnlyContain(id => bookingIds.Contains(id));
    }

    [Fact]
    public async Task AllRooms_HaveValidRoomTypeId()
    {
        var rtIds = (await _fixture.Context.RoomTypes.Select(rt => rt.Id).ToListAsync()).ToHashSet();
        var roomRtIds = await _fixture.Context.Rooms.Select(r => r.RoomTypeId).Distinct().ToListAsync();

        roomRtIds.Should().OnlyContain(id => rtIds.Contains(id));
    }

    [Fact]
    public async Task BookingReferences_AreUnique()
    {
        var refs = await _fixture.Context.Bookings.Select(b => b.BookingReference).ToListAsync();
        refs.Should().OnlyHaveUniqueItems("booking references must be unique");
    }

    [Fact]
    public async Task GuestEmails_AreUniquePerProperty()
    {
        var groups = await _fixture.Context.Guests
            .Where(g => g.Email != null)
            .GroupBy(g => new { g.PropertyId, g.Email })
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToListAsync();

        groups.Should().BeEmpty("each guest email should be unique within a property");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  MULTI-TENANCY — Property isolation
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PropertyA_BookingsOnly_BelongToPropertyA()
    {
        var bookings = await _fixture.Context.Bookings
            .Where(b => b.PropertyId == _propAId)
            .ToListAsync();

        bookings.Should().NotBeEmpty();
        bookings.Should().OnlyContain(b => b.PropertyId == _propAId);
    }

    [Fact]
    public async Task PropertyB_GuestsOnly_BelongToPropertyB()
    {
        var guests = await _fixture.Context.Guests
            .Where(g => g.PropertyId == _propBId)
            .ToListAsync();

        guests.Should().NotBeEmpty();
        guests.Should().OnlyContain(g => g.PropertyId == _propBId);
    }

    [Fact]
    public async Task PropertiesHaveEqualDistribution()
    {
        var aCount = await _fixture.Context.Bookings.CountAsync(b => b.PropertyId == _propAId);
        var bCount = await _fixture.Context.Bookings.CountAsync(b => b.PropertyId == _propBId);

        aCount.Should().Be(500);
        bCount.Should().Be(500);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  SOFT-DELETE — Deletion + query filter
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SoftDelete_Guest_ExcludedFromQuery()
    {
        using var ctx = _fixture.CreateFreshContext();

        // Pick a guest that has NO FK references in GuestPreferences, GuestLoyalties, or Invoices
        // (indices 0–999 are referenced; the tail-end guests 1000+ are safe to soft-delete)
        var referencedGuestIds = await ctx.GuestPreferences.Select(gp => gp.GuestId)
            .Union(ctx.GuestLoyalties.Select(gl => gl.GuestId))
            .Union(ctx.Invoices.Select(inv => inv.GuestId))
            .Distinct()
            .ToListAsync();

        var guest = await ctx.Guests
            .Where(g => !referencedGuestIds.Contains(g.Id))
            .FirstAsync();
        var guestId = guest.Id;

        ctx.Guests.Remove(guest);
        await ctx.SaveChangesAsync();

        // Query filters should exclude it
        var found = await ctx.Guests.FirstOrDefaultAsync(g => g.Id == guestId);
        found.Should().BeNull("soft-deleted guest should not appear in filtered queries");

        // But it still exists in the database (unfiltered)
        var raw = await ctx.Guests.IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Id == guestId);
        raw.Should().NotBeNull("soft-deleted guest should still exist in DB");
        raw!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDelete_Booking_SetsDeletedTimestamp()
    {
        using var ctx = _fixture.CreateFreshContext();
        var booking = await ctx.Bookings.FirstAsync();

        ctx.Bookings.Remove(booking);
        await ctx.SaveChangesAsync();

        var raw = await ctx.Bookings.IgnoreQueryFilters()
            .FirstAsync(b => b.Id == booking.Id);
        raw.IsDeleted.Should().BeTrue();
        raw.DeletedAt.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CONCURRENCY — Optimistic locking with xmin
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConcurrentUpdate_SameGuest_ThrowsConcurrencyException()
    {
        using var ctx1 = _fixture.CreateFreshContext();
        using var ctx2 = _fixture.CreateFreshContext();

        var guest1 = await ctx1.Guests.Skip(10).FirstAsync();
        var guest2 = await ctx2.Guests.FirstAsync(g => g.Id == guest1.Id);

        guest1.UpdateContactInfo("modified1@test.com", "+27111111111");
        await ctx1.SaveChangesAsync();

        guest2.UpdateContactInfo("modified2@test.com", "+27222222222");
        var act = async () => await ctx2.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "optimistic concurrency should prevent stale updates");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  AGGREGATE QUERIES — Financial integrity
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FolioLineItems_TotalPerFolio_MatchesExpectedAmount()
    {
        var folioTotals = await _fixture.Context.FolioLineItems
            .GroupBy(li => li.FolioId)
            .Select(g => new { FolioId = g.Key, Total = g.Sum(li => li.UnitPrice * li.Quantity) })
            .Take(100)
            .ToListAsync();

        folioTotals.Should().NotBeEmpty();
        folioTotals.Should().OnlyContain(ft => ft.Total == 3000,
            "each folio should total 3000 ZAR (1500 + 700 + 800)");
    }

    [Fact]
    public async Task TotalPayments_MatchesExpectedPerFolio()
    {
        var paymentTotals = await _fixture.Context.Payments
            .GroupBy(p => p.FolioId)
            .Select(g => new { FolioId = g.Key, Total = g.Sum(p => p.Amount) })
            .Take(100)
            .ToListAsync();

        paymentTotals.Should().NotBeEmpty();
        paymentTotals.Should().OnlyContain(pt => pt.Total == 3000,
            "each folio payment should be 3000");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TIMESTAMP AUDIT — CreatedAt/UpdatedAt auto-set
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AllEntities_HaveCreatedAtSet()
    {
        var guests = await _fixture.Context.Guests.AsNoTracking().Take(100).ToListAsync();
        guests.Should().OnlyContain(g => g.CreatedAt > DateTime.MinValue,
            "CreatedAt must be set by SaveChangesAsync");
    }

    [Fact]
    public async Task AllEntities_HaveUpdatedAtSet()
    {
        var bookings = await _fixture.Context.Bookings.AsNoTracking().Take(100).ToListAsync();
        bookings.Should().OnlyContain(b => b.UpdatedAt > DateTime.MinValue,
            "UpdatedAt must be set by SaveChangesAsync");
    }

    [Fact]
    public async Task UpdatedEntity_HasLaterUpdatedAt()
    {
        using var ctx = _fixture.CreateFreshContext();
        var guest = await ctx.Guests.Skip(50).FirstAsync();
        var originalUpdatedAt = guest.UpdatedAt;

        await Task.Delay(50);
        guest.UpdateContactInfo("updated@test.com", "+27199999999");
        await ctx.SaveChangesAsync();

        guest.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CONCURRENT READS — No deadlocks under parallel queries
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ParallelReads_NoDeadlocks()
    {
        var tasks = Enumerable.Range(0, 20).Select(async i =>
        {
            using var ctx = _fixture.CreateFreshContext();
            switch (i % 5)
            {
                case 0: await ctx.Guests.AsNoTracking().Take(100).ToListAsync(); break;
                case 1: await ctx.Bookings.AsNoTracking().Take(100).ToListAsync(); break;
                case 2: await ctx.Rooms.AsNoTracking().Take(100).ToListAsync(); break;
                case 3: await ctx.Folios.AsNoTracking().Take(100).ToListAsync(); break;
                case 4: await ctx.HousekeepingTasks.AsNoTracking().Take(100).ToListAsync(); break;
            }
        });

        var act = () => Task.WhenAll(tasks);
        await act.Should().NotThrowAsync("parallel reads should not cause deadlocks");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  BOOKING LIFECYCLE — Full state machine at scale
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BookingLifecycle_CheckInThenCheckOut_AtScale()
    {
        using var ctx = _fixture.CreateFreshContext();
        var userId = Guid.NewGuid();
        var bookings = await ctx.Bookings
            .Where(b => b.Status == BookingStatus.Confirmed)
            .Take(50)
            .ToListAsync();

        bookings.Should().NotBeEmpty("there must be confirmed bookings to test the lifecycle");

        foreach (var booking in bookings)
        {
            booking.CheckIn(userId);
            booking.Status.Should().Be(BookingStatus.CheckedIn);
        }
        await ctx.SaveChangesAsync();

        foreach (var booking in bookings)
        {
            booking.CheckOut(userId);
            booking.Status.Should().Be(BookingStatus.CheckedOut);
        }
        await ctx.SaveChangesAsync();

        var ids = bookings.Select(b => b.Id).ToList();
        var reloaded = await ctx.Bookings.IgnoreQueryFilters().Where(b => ids.Contains(b.Id) && !b.IsDeleted).ToListAsync();
        reloaded.Should().NotBeEmpty("reloaded bookings should exist after checkout");
        reloaded.Should().OnlyContain(b => b.Status == BookingStatus.CheckedOut);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  HOUSEKEEPING WORKFLOW — Full 6-state lifecycle at scale
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HousekeepingLifecycle_FullWorkflow_AtScale()
    {
        using var ctx = _fixture.CreateFreshContext();
        var tasks = await ctx.HousekeepingTasks
            .Where(t => t.Status == HousekeepingTaskStatus.Pending)
            .Take(20)
            .ToListAsync();

        var staffId = Guid.NewGuid();

        foreach (var task in tasks)
        {
            task.AssignTo(staffId);
            task.Start();
            task.Complete(true, true, true, true, true);
            task.Inspect(Guid.NewGuid(), true, null);
            task.Status.Should().Be(HousekeepingTaskStatus.Inspected);
        }
        await ctx.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CROSS-TABLE JOIN — Complex queries at scale
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ComplexJoin_BookingsWithGuestsAndFolios_Under3Seconds()
    {
        var sw = Stopwatch.StartNew();
        var results = await _fixture.Context.Bookings
            .AsNoTracking()
            .Join(_fixture.Context.Guests,
                b => b.GuestId,
                g => g.Id,
                (b, g) => new { b.BookingReference, g.FirstName, g.LastName, b.PropertyId, BookingId = b.Id })
            .Join(_fixture.Context.Folios,
                bg => bg.BookingId,
                f => f.BookingId,
                (bg, f) => new { bg.BookingReference, bg.FirstName, bg.LastName, bg.PropertyId })
            .Where(r => r.PropertyId == _propAId)
            .Take(200)
            .ToListAsync();
        sw.Stop();

        results.Should().NotBeEmpty();
        results.Should().OnlyContain(r => r.PropertyId == _propAId);
        sw.Elapsed.TotalSeconds.Should().BeLessThan(3, "complex join query should be efficient");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  EDGE CASES — Boundary conditions at scale
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EmptyPropertyFilter_ReturnsNothing()
    {
        var bookings = await _fixture.Context.Bookings
            .Where(b => b.PropertyId == Guid.Empty)
            .ToListAsync();

        bookings.Should().BeEmpty("no bookings should exist for an empty GUID property");
    }

    [Fact]
    public async Task NonExistentProperty_ReturnsNothing()
    {
        var bookings = await _fixture.Context.Bookings
            .Where(b => b.PropertyId == Guid.NewGuid())
            .ToListAsync();

        bookings.Should().BeEmpty("no bookings should exist for a random property ID");
    }

    [Fact]
    public async Task CountPerProperty_SumsToTotal()
    {
        var total = await _fixture.Context.Guests.CountAsync();
        var aCount = await _fixture.Context.Guests.CountAsync(g => g.PropertyId == _propAId);
        var bCount = await _fixture.Context.Guests.CountAsync(g => g.PropertyId == _propBId);

        (aCount + bCount).Should().BeLessOrEqualTo(total);
        (aCount + bCount).Should().BeGreaterOrEqualTo(total - 10, "at most 10 guests deleted in other tests");
    }
}
