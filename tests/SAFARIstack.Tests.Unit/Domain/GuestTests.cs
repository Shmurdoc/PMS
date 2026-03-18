using FluentAssertions;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Shared.ValueObjects;

namespace SAFARIstack.Tests.Unit.Domain;

public class GuestTests
{
    private static readonly Guid PropertyId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_SetsAllProperties()
    {
        var guest = Guest.Create(PropertyId, "John", "Doe", "john@example.com", "+27123456789");

        guest.PropertyId.Should().Be(PropertyId);
        guest.FirstName.Should().Be("John");
        guest.LastName.Should().Be("Doe");
        guest.Email.Should().Be("john@example.com");
        guest.Phone.Should().Be("+27123456789");
        guest.FullName.Should().Be("John Doe");
        guest.IsBlacklisted.Should().BeFalse();
        guest.GuestType.Should().Be(GuestType.Individual);
        guest.IdType.Should().Be(IdType.SAId);
    }

    [Fact]
    public void Create_NullEmailAndPhone_Allowed()
    {
        var guest = Guest.Create(PropertyId, "Jane", "Smith", null, null);
        guest.Email.Should().BeNull();
        guest.Phone.Should().BeNull();
    }

    [Fact]
    public void Create_RaisesGuestCreatedEvent()
    {
        var guest = Guest.Create(PropertyId, "John", "Doe", null, null);
        guest.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<GuestCreatedEvent>()
            .Which.FullName.Should().Be("John Doe");
    }

    [Fact]
    public void UpdateContactInfo_UpdatesEmailAndPhone()
    {
        var guest = Guest.Create(PropertyId, "John", "Doe", null, null);
        guest.UpdateContactInfo("new@email.com", "+27999999999");

        guest.Email.Should().Be("new@email.com");
        guest.Phone.Should().Be("+27999999999");
    }

    [Fact]
    public void UpdateIdInfo_SetsIdFields()
    {
        var guest = Guest.Create(PropertyId, "John", "Doe", null, null);
        var dob = new DateTime(1990, 5, 15);
        guest.UpdateIdInfo("9005155028083", IdType.SAId, dob);

        guest.IdNumber.Should().Be("9005155028083");
        guest.IdType.Should().Be(IdType.SAId);
        guest.DateOfBirth.Should().Be(dob);
    }

    [Fact]
    public void SetCompanyInfo_ChangesTypeToCorporate()
    {
        var guest = Guest.Create(PropertyId, "Corp", "User", null, null);
        guest.SetCompanyInfo("ACME Ltd", "VAT-12345");

        guest.CompanyName.Should().Be("ACME Ltd");
        guest.CompanyVATNumber.Should().Be("VAT-12345");
        guest.GuestType.Should().Be(GuestType.Corporate);
    }

    [Fact]
    public void Blacklist_SetsBlacklistFlagsAndRaisesEvent()
    {
        var guest = Guest.Create(PropertyId, "Bad", "Guest", null, null);
        guest.ClearDomainEvents();
        guest.Blacklist("Property damage");

        guest.IsBlacklisted.Should().BeTrue();
        guest.BlacklistReason.Should().Be("Property damage");
        guest.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<GuestBlacklistedEvent>();
    }

    [Fact]
    public void RemoveFromBlacklist_ClearsFlags()
    {
        var guest = Guest.Create(PropertyId, "Redeemed", "Guest", null, null);
        guest.Blacklist("Reason");
        guest.RemoveFromBlacklist();

        guest.IsBlacklisted.Should().BeFalse();
        guest.BlacklistReason.Should().BeNull();
    }

    [Fact]
    public void AddPreference_IncreasesPreferences()
    {
        var guest = Guest.Create(PropertyId, "Pref", "Guest", null, null);
        var pref = GuestPreference.Create(guest.Id, PreferenceCategory.Room, "floor", "high");
        guest.AddPreference(pref);

        guest.Preferences.Should().HaveCount(1);
    }
}

public class GuestPreferenceTests
{
    [Fact]
    public void Create_SetsAllFields()
    {
        var guestId = Guid.NewGuid();
        var pref = GuestPreference.Create(guestId, PreferenceCategory.Dietary, "allergy", "nuts");

        pref.GuestId.Should().Be(guestId);
        pref.Category.Should().Be(PreferenceCategory.Dietary);
        pref.Key.Should().Be("allergy");
        pref.Value.Should().Be("nuts");
    }

    [Fact]
    public void Update_ChangesValue()
    {
        var pref = GuestPreference.Create(Guid.NewGuid(), PreferenceCategory.Pillow, "type", "firm");
        pref.Update("soft");
        pref.Value.Should().Be("soft");
    }
}

public class GuestLoyaltyTests
{
    [Fact]
    public void Create_StartsWithNoTier()
    {
        var loyalty = GuestLoyalty.Create(Guid.NewGuid());
        loyalty.Tier.Should().Be(LoyaltyTier.None);
        loyalty.TotalPoints.Should().Be(0);
        loyalty.AvailablePoints.Should().Be(0);
        loyalty.TotalStays.Should().Be(0);
    }

    [Fact]
    public void RecordStay_IncrementsTotals()
    {
        var loyalty = GuestLoyalty.Create(Guid.NewGuid());
        loyalty.RecordStay(3, 5000);

        loyalty.TotalStays.Should().Be(1);
        loyalty.TotalNights.Should().Be(3);
        loyalty.TotalSpend.Should().Be(5000);
        loyalty.TotalPoints.Should().Be(5000); // R1 = 1 point
        loyalty.AvailablePoints.Should().Be(5000);
        loyalty.LastStayDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(4, LoyaltyTier.None)]
    [InlineData(5, LoyaltyTier.Bronze)]
    [InlineData(20, LoyaltyTier.Silver)]
    [InlineData(50, LoyaltyTier.Gold)]
    [InlineData(100, LoyaltyTier.Platinum)]
    public void RecordStay_TierUpgrade_BasedOnNights(int totalNights, LoyaltyTier expectedTier)
    {
        var loyalty = GuestLoyalty.Create(Guid.NewGuid());
        // Simulate stays to reach the totalNights
        for (int i = 0; i < totalNights; i++)
        {
            loyalty.RecordStay(1, 100);
        }
        loyalty.TotalNights.Should().Be(totalNights);
        loyalty.Tier.Should().Be(expectedTier);
    }

    [Fact]
    public void RedeemPoints_Sufficient_ReturnsTrue()
    {
        var loyalty = GuestLoyalty.Create(Guid.NewGuid());
        loyalty.RecordStay(5, 10000);
        loyalty.RedeemPoints(5000).Should().BeTrue();
        loyalty.AvailablePoints.Should().Be(5000);
    }

    [Fact]
    public void RedeemPoints_Insufficient_ReturnsFalse()
    {
        var loyalty = GuestLoyalty.Create(Guid.NewGuid());
        loyalty.RecordStay(1, 100);
        loyalty.RedeemPoints(500).Should().BeFalse();
        loyalty.AvailablePoints.Should().Be(100); // Unchanged
    }
}

public class NotificationTests
{
    [Fact]
    public void Create_SetsAllFields()
    {
        var propertyId = Guid.NewGuid();
        var notif = Notification.Create(propertyId, "test@test.com", NotificationChannel.Email,
            NotificationType.BookingConfirmation, "Booking Confirmed", "Your booking is confirmed");

        notif.PropertyId.Should().Be(propertyId);
        notif.RecipientAddress.Should().Be("test@test.com");
        notif.Channel.Should().Be(NotificationChannel.Email);
        notif.Type.Should().Be(NotificationType.BookingConfirmation);
        notif.Subject.Should().Be("Booking Confirmed");
        notif.Status.Should().Be(NotificationStatus.Queued);
    }

    [Fact]
    public void MarkSent_UpdatesStatusAndTimestamp()
    {
        var notif = Notification.Create(Guid.NewGuid(), "a@b.com", NotificationChannel.SMS,
            NotificationType.CheckInReminder, "Reminder", "Check in today");
        notif.MarkSent("REF-123");

        notif.Status.Should().Be(NotificationStatus.Sent);
        notif.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        notif.ExternalReference.Should().Be("REF-123");
    }

    [Fact]
    public void MarkFailed_IncrementsRetry_KeepsQueuedUnder3()
    {
        var notif = Notification.Create(Guid.NewGuid(), "a@b.com", NotificationChannel.Email,
            NotificationType.SystemAlert, "Alert", "Test");
        notif.MarkFailed("timeout");

        notif.RetryCount.Should().Be(1);
        notif.Status.Should().Be(NotificationStatus.Queued);
        notif.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public void MarkFailed_After3Retries_MarksFailed()
    {
        var notif = Notification.Create(Guid.NewGuid(), "a@b.com", NotificationChannel.Email,
            NotificationType.SystemAlert, "Alert", "Test");
        notif.MarkFailed("error1");
        notif.MarkFailed("error2");
        notif.MarkFailed("error3");

        notif.RetryCount.Should().Be(3);
        notif.Status.Should().Be(NotificationStatus.Failed);
    }

    [Fact]
    public void MarkRead_SetsReadStatus()
    {
        var notif = Notification.Create(Guid.NewGuid(), "a@b.com", NotificationChannel.InApp,
            NotificationType.BookingConfirmation, "Test", "Body");
        notif.MarkRead();
        notif.Status.Should().Be(NotificationStatus.Read);
        notif.ReadAt.Should().NotBeNull();
    }
}

public class AuditLogTests
{
    [Fact]
    public void Create_SetsAllFields()
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var log = AuditLog.Create("Create", "Booking", entityId, userId, "admin@test.com",
            null, "{\"status\":\"Confirmed\"}", Guid.NewGuid(), "192.168.1.1");

        log.Action.Should().Be("Create");
        log.EntityType.Should().Be("Booking");
        log.EntityId.Should().Be(entityId);
        log.UserId.Should().Be(userId);
        log.UserName.Should().Be("admin@test.com");
        log.NewValues.Should().Contain("Confirmed");
        log.IpAddress.Should().Be("192.168.1.1");
    }
}
