using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.Infrastructure.Services;

/// <summary>
/// Digital check-in service — contactless guest check-in flow.
/// Steps: Initiate → Verify ID → Select Room → Sign Registration → Complete.
/// </summary>
public class DigitalCheckInService : IDigitalCheckInService
{
    private readonly ApplicationDbContext _db;
    public DigitalCheckInService(ApplicationDbContext db) => _db = db;

    public async Task<DigitalCheckInDto> InitiateCheckInAsync(Guid bookingId)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new InvalidOperationException("Booking not found");

        // Check if check-in already initiated
        var existing = await _db.DigitalCheckIns.FirstOrDefaultAsync(d => d.BookingId == bookingId);
        if (existing is not null)
            return MapToDto(existing);

        var checkIn = DigitalCheckIn.Create(
            booking.PropertyId, bookingId, booking.GuestId);

        _db.DigitalCheckIns.Add(checkIn);
        await _db.SaveChangesAsync();

        return MapToDto(checkIn);
    }

    public async Task<DigitalCheckInDto> VerifyIdentityAsync(Guid checkInId, Stream idDocumentImage)
    {
        var checkIn = await _db.DigitalCheckIns.FindAsync(checkInId)
            ?? throw new InvalidOperationException("Check-in record not found");

        // Simulate document verification (in production: call ID verification API)
        var documentHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                await ReadStreamAsync(idDocumentImage)));

        checkIn.VerifyIdentity("Passport", documentHash, 0.95m);

        _db.DigitalCheckIns.Update(checkIn);
        await _db.SaveChangesAsync();

        return MapToDto(checkIn);
    }

    public async Task<IEnumerable<AvailableRoomDto>> GetEligibleRoomsAsync(Guid checkInId)
    {
        var checkIn = await _db.DigitalCheckIns.FindAsync(checkInId)
            ?? throw new InvalidOperationException("Check-in record not found");

        var booking = await _db.Bookings
            .Include(b => b.BookingRooms)
                .ThenInclude(br => br.Room)
                    .ThenInclude(r => r.RoomType)
            .FirstOrDefaultAsync(b => b.Id == checkIn.BookingId);

        var firstBookingRoom = booking?.BookingRooms.FirstOrDefault();
        if (firstBookingRoom?.Room is null)
            return Enumerable.Empty<AvailableRoomDto>();

        var assignedRoom = firstBookingRoom.Room;
        var roomType = assignedRoom.RoomType;

        // Return assigned room + any available upgrades
        var rooms = new List<AvailableRoomDto>
        {
            new(
                assignedRoom.Id, assignedRoom.RoomNumber,
                assignedRoom.Floor?.ToString(), assignedRoom.Wing,
                roomType?.Name ?? "Standard", roomType?.SizeInSquareMeters ?? 25,
                roomType?.ViewType, false, 0m,
                "Your assigned room")
        };

        // Find upgrade options: higher-tier rooms that are available
        var availableUpgrades = await _db.Rooms
            .Include(r => r.RoomType)
            .Where(r => r.PropertyId == booking.PropertyId
                     && r.Status == RoomStatus.Available
                     && r.HkStatus == HousekeepingStatus.Clean
                     && r.RoomType != null
                     && r.RoomType.BasePrice > (roomType != null ? roomType.BasePrice : 0))
            .OrderBy(r => r.RoomType!.BasePrice)
            .Take(3)
            .ToListAsync();

        foreach (var r in availableUpgrades)
        {
            rooms.Add(new AvailableRoomDto(
                r.Id, r.RoomNumber, r.Floor?.ToString(), r.Wing,
                r.RoomType?.Name ?? "Upgraded",
                r.RoomType?.SizeInSquareMeters ?? 30,
                r.RoomType?.ViewType, true,
                (r.RoomType?.BasePrice ?? 0) - (roomType?.BasePrice ?? 0),
                $"Upgrade to {r.RoomType?.Name ?? "premium room"}"));
        }

        return rooms;
    }

    public async Task<DigitalCheckInDto> SelectRoomAsync(Guid checkInId, Guid roomId, bool isUpgrade, decimal upgradeAmount)
    {
        var checkIn = await _db.DigitalCheckIns.FindAsync(checkInId)
            ?? throw new InvalidOperationException("Check-in record not found");

        checkIn.SelectRoom(roomId, isUpgrade, upgradeAmount);

        _db.DigitalCheckIns.Update(checkIn);
        await _db.SaveChangesAsync();

        return MapToDto(checkIn);
    }

    public async Task<DigitalCheckInDto> SignRegistrationCardAsync(
        Guid checkInId, string signatureData, string ipAddress,
        bool popiaConsent, bool marketingConsent)
    {
        var checkIn = await _db.DigitalCheckIns.FindAsync(checkInId)
            ?? throw new InvalidOperationException("Check-in record not found");

        checkIn.SignRegistrationCard(signatureData, ipAddress, popiaConsent, marketingConsent);

        _db.DigitalCheckIns.Update(checkIn);
        await _db.SaveChangesAsync();

        return MapToDto(checkIn);
    }

    public async Task<DigitalCheckInDto> CompleteCheckInAsync(Guid checkInId)
    {
        var checkIn = await _db.DigitalCheckIns.FindAsync(checkInId)
            ?? throw new InvalidOperationException("Check-in record not found");

        checkIn.Complete();

        _db.DigitalCheckIns.Update(checkIn);
        await _db.SaveChangesAsync();

        return MapToDto(checkIn);
    }

    private static DigitalCheckInDto MapToDto(DigitalCheckIn c) => new(
        c.Id, c.BookingId, c.Status.ToString(),
        c.IdVerified, c.SignedAt.HasValue,
        c.SelectedRoomId, c.MobileKeyStatus.ToString(),
        c.CompletedAt);

    private static async Task<byte[]> ReadStreamAsync(Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }
}
