using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.Infrastructure.Services;

/// <summary>
/// Multi-Property Enterprise Service — Group dashboards, rate copying, inventory allocation
/// </summary>
public class MultiPropertyService : IMultiPropertyService
{
    private readonly ApplicationDbContext _db;
    private readonly IUnitOfWork _uow;

    public MultiPropertyService(ApplicationDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<GroupDashboardDto> GetGroupDashboardAsync(
        Guid groupId, DateTime startDate, DateTime endDate)
    {
        var group = await _db.Set<PropertyGroup>()
            .Include(g => g.Memberships)
            .FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new InvalidOperationException($"Property group {groupId} not found.");

        var propertyIds = group.Memberships.Select(m => m.PropertyId).ToList();
        var properties = await _db.Properties
            .Where(p => propertyIds.Contains(p.Id))
            .AsNoTracking()
            .ToListAsync();

        var bookings = await _db.Bookings
            .IgnoreQueryFilters()
            .Where(b => propertyIds.Contains(b.PropertyId)
                && !b.IsDeleted
                && b.CheckInDate <= endDate
                && b.CheckOutDate >= startDate)
            .AsNoTracking()
            .ToListAsync();

        var payments = await _db.Payments
            .IgnoreQueryFilters()
            .Where(p => propertyIds.Contains(p.PropertyId)
                && !p.IsDeleted
                && p.CreatedAt >= startDate
                && p.CreatedAt <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var rooms = await _db.Rooms
            .IgnoreQueryFilters()
            .Where(r => propertyIds.Contains(r.PropertyId) && !r.IsDeleted)
            .AsNoTracking()
            .ToListAsync();

        var propertySummaries = new List<PropertySummaryDto>();
        decimal totalRevenue = 0;
        decimal totalOccupancy = 0;
        decimal totalADR = 0;

        foreach (var prop in properties)
        {
            var propBookings = bookings.Where(b => b.PropertyId == prop.Id).ToList();
            var propPayments = payments.Where(p => p.PropertyId == prop.Id).ToList();
            var propRooms = rooms.Where(r => r.PropertyId == prop.Id).ToList();

            var revenue = propPayments.Sum(p => p.Amount);
            var totalRoomNights = propRooms.Count * (endDate - startDate).Days;
            var occupiedNights = propBookings.Sum(b => b.Nights);
            var occupancy = totalRoomNights > 0 ? (decimal)occupiedNights / totalRoomNights * 100 : 0;
            var adr = propBookings.Count > 0 ? propBookings.Average(b => b.TotalAmount / Math.Max(b.Nights, 1)) : 0;
            var revPar = totalRoomNights > 0 ? revenue / totalRoomNights : 0;

            totalRevenue += revenue;
            totalOccupancy += occupancy;
            totalADR += adr;

            propertySummaries.Add(new PropertySummaryDto(
                prop.Id, prop.Name, revenue, Math.Round(occupancy, 1),
                Math.Round(adr, 2), Math.Round(revPar, 2),
                propBookings.Count, propBookings.Select(b => b.GuestId).Distinct().Count()));
        }

        var propCount = properties.Count;
        var avgOccupancy = propCount > 0 ? totalOccupancy / propCount : 0;
        var avgADR = propCount > 0 ? totalADR / propCount : 0;
        var totalRoomNightsAll = rooms.Count * (endDate - startDate).Days;
        var totalRevPAR = totalRoomNightsAll > 0 ? totalRevenue / totalRoomNightsAll : 0;

        return new GroupDashboardDto(
            group.Name, propCount, totalRevenue,
            Math.Round(avgOccupancy, 1), Math.Round(avgADR, 2),
            Math.Round(totalRevPAR, 2), propertySummaries);
    }

    public async Task<RateCopyResultDto> CopyRatesAcrossPropertiesAsync(
        Guid sourcePropertyId, IEnumerable<Guid> targetPropertyIds, RateCopyOptionsDto options)
    {
        try
        {
            var sourceRates = await _db.Rates
                .Where(r => r.PropertyId == sourcePropertyId)
                .AsNoTracking()
                .ToListAsync();

            if (!sourceRates.Any())
                return new RateCopyResultDto(false, 0, "No rates found for source property.");

            int copied = 0;
            foreach (var targetId in targetPropertyIds)
            {
                foreach (var sourceRate in sourceRates)
                {
                    if (options.EffectiveFrom.HasValue && sourceRate.EffectiveFrom < options.EffectiveFrom.Value)
                        continue;
                    if (options.EffectiveTo.HasValue && sourceRate.EffectiveFrom > options.EffectiveTo.Value)
                        continue;

                    var adjustedAmount = sourceRate.AmountPerNight *
                        (1 + options.RateAdjustmentPercentage / 100m);

                    var newRate = Rate.Create(
                        targetId,
                        sourceRate.RoomTypeId,
                        sourceRate.RatePlanId,
                        Math.Round(adjustedAmount, 2),
                        sourceRate.EffectiveFrom,
                        sourceRate.EffectiveTo);

                    await _db.Rates.AddAsync(newRate);
                    copied++;
                }
            }

            await _db.SaveChangesAsync();
            return new RateCopyResultDto(true, copied, null);
        }
        catch (Exception ex)
        {
            return new RateCopyResultDto(false, 0, ex.Message);
        }
    }

    public async Task<GroupAllocationResultDto> AllocateGroupInventoryAsync(
        Guid groupId, GroupInventoryRequestDto request)
    {
        try
        {
            var allocation = GroupInventoryAllocation.Create(
                groupId, request.RoomTypeId,
                request.StartDate, request.EndDate,
                request.AllocatedRooms, request.SellLimitPerProperty);

            await _db.Set<GroupInventoryAllocation>().AddAsync(allocation);
            await _db.SaveChangesAsync();

            return new GroupAllocationResultDto(true, allocation.Id, null);
        }
        catch (Exception ex)
        {
            return new GroupAllocationResultDto(false, Guid.Empty, ex.Message);
        }
    }

    public async Task<IEnumerable<PropertyComparisonDto>> GetPropertyComparisonAsync(
        Guid groupId, DateTime startDate, DateTime endDate)
    {
        var group = await _db.Set<PropertyGroup>()
            .Include(g => g.Memberships)
            .FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new InvalidOperationException($"Property group {groupId} not found.");

        var propertyIds = group.Memberships.Select(m => m.PropertyId).ToList();
        var results = new List<PropertyComparisonDto>();

        foreach (var propId in propertyIds)
        {
            var property = await _db.Properties.FindAsync(propId);
            if (property == null) continue;

            var bookings = await _db.Bookings
                .IgnoreQueryFilters()
                .Where(b => b.PropertyId == propId && !b.IsDeleted
                    && b.CheckInDate <= endDate && b.CheckOutDate >= startDate)
                .AsNoTracking()
                .ToListAsync();

            var payments = await _db.Payments
                .IgnoreQueryFilters()
                .Where(p => p.PropertyId == propId && !p.IsDeleted
                    && p.CreatedAt >= startDate && p.CreatedAt <= endDate)
                .AsNoTracking()
                .ToListAsync();

            var rooms = await _db.Rooms
                .IgnoreQueryFilters()
                .Where(r => r.PropertyId == propId && !r.IsDeleted)
                .CountAsync();

            var revenue = payments.Sum(p => p.Amount);
            var totalRoomNights = rooms * (endDate - startDate).Days;
            var occupiedNights = bookings.Sum(b => b.Nights);
            var occupancy = totalRoomNights > 0 ? (decimal)occupiedNights / totalRoomNights * 100 : 0;
            var adr = bookings.Count > 0 ? bookings.Average(b => b.TotalAmount / Math.Max(b.Nights, 1)) : 0;
            var revPar = totalRoomNights > 0 ? revenue / totalRoomNights : 0;

            // Upsell revenue for this property
            var upsellRevenue = await _db.Set<UpsellTransaction>()
                .Where(t => t.Offer.PropertyId == propId
                    && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .SumAsync(t => t.UnitPrice * t.Quantity);

            results.Add(new PropertyComparisonDto(
                propId, property.Name, revenue,
                Math.Round(occupancy, 1), Math.Round(adr, 2),
                Math.Round(revPar, 2), 0, upsellRevenue));
        }

        return results;
    }
}
