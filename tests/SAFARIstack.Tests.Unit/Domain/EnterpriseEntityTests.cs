using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Tests.Unit.Domain;

/// <summary>
/// Enterprise upgrade entity tests — Upsell, GiftCard, Experience, MultiProperty
/// </summary>
public class EnterpriseEntityTests
{
    // ═══════════════════════════════════════════════════════════════
    // UPSELL OFFER
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void UpsellOffer_Create_SetsPropertiesCorrectly()
    {
        var propertyId = Guid.NewGuid();
        var offer = UpsellOffer.Create(
            propertyId, UpsellOfferType.RoomUpgrade,
            "Deluxe Upgrade", 2500m, 1800m,
            "Upgrade to deluxe suite", 500m, "/img/deluxe.jpg",
            10, DateTime.UtcNow, DateTime.UtcNow.AddDays(30));

        offer.PropertyId.Should().Be(propertyId);
        offer.OfferType.Should().Be(UpsellOfferType.RoomUpgrade);
        offer.Title.Should().Be("Deluxe Upgrade");
        offer.OriginalPrice.Should().Be(2500m);
        offer.OfferPrice.Should().Be(1800m);
        offer.Savings.Should().Be(700m);
        offer.CostPrice.Should().Be(500m);
        offer.InventoryTotal.Should().Be(10);
        offer.InventoryRemaining.Should().Be(10);
        offer.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpsellOffer_Create_ThrowsWhenOfferPriceExceedsOriginal()
    {
        var act = () => UpsellOffer.Create(
            Guid.NewGuid(), UpsellOfferType.Spa,
            "Spa", 500m, 600m); // offerPrice > originalPrice

        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot exceed*");
    }

    [Fact]
    public void UpsellOffer_Purchase_CreatesTransactionAndDecrementsInventory()
    {
        var offer = UpsellOffer.Create(
            Guid.NewGuid(), UpsellOfferType.Dining,
            "Dinner", 350m, 250m, inventoryTotal: 5);

        var tx = offer.Purchase(Guid.NewGuid(), Guid.NewGuid(), 2);

        tx.Should().NotBeNull();
        tx.Quantity.Should().Be(2);
        tx.UnitPrice.Should().Be(250m);
        tx.TotalAmount.Should().Be(500m);
        offer.InventoryRemaining.Should().Be(3);
    }

    [Fact]
    public void UpsellOffer_Purchase_ThrowsWhenInsufficientInventory()
    {
        var offer = UpsellOffer.Create(
            Guid.NewGuid(), UpsellOfferType.Parking,
            "Parking", 100m, 80m, inventoryTotal: 1);

        var act = () => offer.Purchase(Guid.NewGuid(), Guid.NewGuid(), 5);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient*");
    }

    [Fact]
    public void UpsellOffer_Deactivate_SetsInactive()
    {
        var offer = UpsellOffer.Create(
            Guid.NewGuid(), UpsellOfferType.Spa, "Spa", 500m, 400m);

        offer.Deactivate();

        offer.IsActive.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════
    // GIFT CARD
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GiftCard_Create_SetsPropertiesCorrectly()
    {
        var card = GiftCard.Create(
            Guid.NewGuid(), "SFRI-1234-5678-9012", "hash",
            1000m, "John", "john@test.com",
            "Jane", "jane@test.com");

        card.InitialBalance.Should().Be(1000m);
        card.CurrentBalance.Should().Be(1000m);
        card.Status.Should().Be(GiftCardStatus.Active);
        card.Currency.Should().Be("ZAR");
        card.RecipientName.Should().Be("John");
        card.SenderName.Should().Be("Jane");
    }

    [Fact]
    public void GiftCard_Create_ThrowsWhenAmountTooLow()
    {
        var act = () => GiftCard.Create(
            Guid.NewGuid(), "SFRI-0000-0000-0000", "hash", 10m);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*minimum*R50*");
    }

    [Fact]
    public void GiftCard_Create_ThrowsWhenAmountTooHigh()
    {
        var act = () => GiftCard.Create(
            Guid.NewGuid(), "SFRI-0000-0000-0000", "hash", 60000m);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*maximum*R50,000*");
    }

    [Fact]
    public void GiftCard_Redeem_ReducesBalanceAndCreatesRedemption()
    {
        var card = GiftCard.Create(
            Guid.NewGuid(), "SFRI-1234-5678-9012", "hash", 500m);

        var redemption = card.Redeem(Guid.NewGuid(), 200m);

        card.CurrentBalance.Should().Be(300m);
        card.Status.Should().Be(GiftCardStatus.Active);
        redemption.Amount.Should().Be(200m);
        redemption.RemainingBalance.Should().Be(300m);
    }

    [Fact]
    public void GiftCard_Redeem_FullyRedeemsAndChangesStatus()
    {
        var card = GiftCard.Create(
            Guid.NewGuid(), "SFRI-1234-5678-9012", "hash", 500m);

        card.Redeem(Guid.NewGuid(), 500m);

        card.CurrentBalance.Should().Be(0);
        card.Status.Should().Be(GiftCardStatus.FullyRedeemed);
    }

    [Fact]
    public void GiftCard_Redeem_ThrowsWhenInsufficientBalance()
    {
        var card = GiftCard.Create(
            Guid.NewGuid(), "SFRI-1234-5678-9012", "hash", 100m);

        var act = () => card.Redeem(Guid.NewGuid(), 150m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient*");
    }

    [Fact]
    public void GiftCard_Void_ChangesStatusToVoided()
    {
        var card = GiftCard.Create(
            Guid.NewGuid(), "SFRI-1234-5678-9012", "hash", 500m);

        card.Void("Fraud detected");

        card.Status.Should().Be(GiftCardStatus.Voided);
    }

    [Fact]
    public void GiftCard_Redeem_ThrowsWhenVoided()
    {
        var card = GiftCard.Create(
            Guid.NewGuid(), "SFRI-1234-5678-9012", "hash", 500m);
        card.Void("Test");

        var act = () => card.Redeem(Guid.NewGuid(), 100m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Voided*");
    }

    [Fact]
    public void GiftCard_GenerateCardNumber_ReturnsCorrectFormat()
    {
        var number = GiftCard.GenerateCardNumber();

        number.Should().StartWith("SFRI-");
        number.Should().HaveLength(19); // SFRI-XXXX-XXXX-XXXX
    }

    // ═══════════════════════════════════════════════════════════════
    // EXPERIENCE
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Experience_Create_SetsPropertiesCorrectly()
    {
        var exp = Experience.Create(
            Guid.NewGuid(), "Sunrise Safari", ExperienceCategory.Safari,
            180, 8, 1500m, "Early morning game drive",
            true, "Kruger Gate", DifficultyLevel.Moderate);

        exp.Name.Should().Be("Sunrise Safari");
        exp.Category.Should().Be(ExperienceCategory.Safari);
        exp.DurationMinutes.Should().Be(180);
        exp.MaxGuests.Should().Be(8);
        exp.BasePrice.Should().Be(1500m);
        exp.PricePerPerson.Should().BeTrue();
        exp.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Experience_Create_ThrowsWhenDurationInvalid()
    {
        var act = () => Experience.Create(
            Guid.NewGuid(), "Bad", ExperienceCategory.Safari,
            0, 5, 100m);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Duration*positive*");
    }

    [Fact]
    public void Experience_SetThirdParty_ConfiguresCommission()
    {
        var exp = Experience.Create(
            Guid.NewGuid(), "River Rafting", ExperienceCategory.Adventure,
            120, 12, 800m);

        exp.SetThirdParty("Safari Adventures Ltd", 0.15m);

        exp.IsThirdParty.Should().BeTrue();
        exp.ThirdPartyOperator.Should().Be("Safari Adventures Ltd");
        exp.CommissionRate.Should().Be(0.15m);
    }

    [Fact]
    public void ExperienceBooking_Create_CalculatesCorrectly()
    {
        var booking = ExperienceBooking.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow.AddDays(3), new TimeOnly(6, 0),
            4, 6000m);

        booking.ParticipantCount.Should().Be(4);
        booking.TotalPrice.Should().Be(6000m);
        booking.Status.Should().Be(ExperienceBookingStatus.Confirmed);
    }

    [Fact]
    public void ExperienceBooking_Lifecycle_WorksCorrectly()
    {
        var booking = ExperienceBooking.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow, new TimeOnly(6, 0), 2, 3000m);

        booking.CheckIn();
        booking.Status.Should().Be(ExperienceBookingStatus.InProgress);
        booking.CheckInTime.Should().NotBeNull();

        booking.Complete();
        booking.Status.Should().Be(ExperienceBookingStatus.Completed);
        booking.CompletedAt.Should().NotBeNull();

        booking.AddFeedback(5, "Amazing experience!");
        booking.FeedbackScore.Should().Be(5);
    }

    [Fact]
    public void ExperienceBooking_AddFeedback_ThrowsOnInvalidScore()
    {
        var booking = ExperienceBooking.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow, new TimeOnly(6, 0), 2, 3000m);

        var act = () => booking.AddFeedback(6, "Too high");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*1-5*");
    }

    // ═══════════════════════════════════════════════════════════════
    // MULTI-PROPERTY
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void PropertyGroup_Create_SetsPropertiesCorrectly()
    {
        var group = PropertyGroup.Create("Safari Collection", "Premium lodges", "Cape Town");

        group.Name.Should().Be("Safari Collection");
        group.Description.Should().Be("Premium lodges");
        group.HeadquartersAddress.Should().Be("Cape Town");
        group.IsActive.Should().BeTrue();
    }

    [Fact]
    public void PropertyGroup_Create_ThrowsWhenNameEmpty()
    {
        var act = () => PropertyGroup.Create("");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*required*");
    }

    [Fact]
    public void PropertyGroup_AddProperty_AddsAndPreventsDuplicates()
    {
        var group = PropertyGroup.Create("Test Group");
        var propId = Guid.NewGuid();

        group.AddProperty(propId, true);

        group.Memberships.Should().HaveCount(1);
        group.Memberships.First().IsFlagship.Should().BeTrue();

        var act = () => group.AddProperty(propId);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already a member*");
    }

    [Fact]
    public void PropertyGroup_RemoveProperty_RemovesSuccessfully()
    {
        var group = PropertyGroup.Create("Test Group");
        var propId = Guid.NewGuid();
        group.AddProperty(propId);

        group.RemoveProperty(propId);

        group.Memberships.Should().BeEmpty();
    }

    [Fact]
    public void RateCopyJob_Lifecycle_TransitionsCorrectly()
    {
        var job = RateCopyJob.Create(
            Guid.NewGuid(),
            new List<Guid> { Guid.NewGuid() },
            new List<Guid> { Guid.NewGuid() },
            Guid.NewGuid(),
            10m, false);

        job.Status.Should().Be(RateCopyJobStatus.Pending);

        job.MarkInProgress();
        job.Status.Should().Be(RateCopyJobStatus.InProgress);

        job.MarkCompleted(25);
        job.Status.Should().Be(RateCopyJobStatus.Completed);
        job.TotalRatesCopied.Should().Be(25);
        job.CompletedAt.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════
    // GUEST MESSAGING
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GuestMessage_CreateInbound_SetsPropertiesCorrectly()
    {
        var msg = GuestMessage.CreateInbound(
            Guid.NewGuid(), MessageChannel.Email,
            "guest@test.com", "Room Query", "Is late checkout available?",
            Guid.NewGuid(), null, "John Doe");

        msg.Channel.Should().Be(MessageChannel.Email);
        msg.Direction.Should().Be(MessageDirection.Inbound);
        msg.Subject.Should().Be("Room Query");
        msg.Status.Should().Be(MessageStatus.Received);
        msg.SenderName.Should().Be("John Doe");
    }

    [Fact]
    public void GuestMessage_AiWorkflow_ApproveAndEdit()
    {
        var msg = GuestMessage.CreateInbound(
            Guid.NewGuid(), MessageChannel.WebChat,
            "webchat", "WiFi", "What's the WiFi password?");

        msg.SetAiSuggestion("The WiFi password is on your key card.", 0.9m);
        msg.AiSuggestedReply.Should().Be("The WiFi password is on your key card.");
        msg.AiConfidenceScore.Should().Be(0.9m);

        var staffId = Guid.NewGuid();
        msg.ApproveAiReply(staffId);
        msg.AiReplyApproved.Should().BeTrue();
        msg.FinalReply.Should().Be("The WiFi password is on your key card.");
        msg.Status.Should().Be(MessageStatus.Replied);
    }

    [Fact]
    public void GuestMessage_EditAndApproveAi_SetsEditedFlag()
    {
        var msg = GuestMessage.CreateInbound(
            Guid.NewGuid(), MessageChannel.SMS,
            "+27123456789", "Test", "Hello");

        msg.SetAiSuggestion("Original AI reply", 0.7m);

        msg.EditAndApproveAiReply("Edited reply by staff", Guid.NewGuid());

        msg.AiReplyEdited.Should().BeTrue();
        msg.AiReplyApproved.Should().BeTrue();
        msg.FinalReply.Should().Be("Edited reply by staff");
    }

    // ═══════════════════════════════════════════════════════════════
    // AI INTERACTION
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void AiInteraction_LearningLoop_TracksOutcomes()
    {
        var interaction = AiInteraction.Create(
            Guid.NewGuid(), "What time is checkout?",
            "Checkout is at 10 AM", 0.92m, "CheckOut",
            45, 0, 0, "RuleEngine-v1", AiInteractionSource.WebChat);

        interaction.Outcome.Should().Be(AiInteractionOutcome.Pending);

        interaction.SetAutoSent();
        interaction.Outcome.Should().Be(AiInteractionOutcome.AutoSent);

        interaction.RecordGuestSatisfaction(4);
        interaction.GuestSatisfaction.Should().Be(4);
    }

    [Fact]
    public void AiInteraction_EditAndApprove_TracksCorrectly()
    {
        var interaction = AiInteraction.Create(
            Guid.NewGuid(), "Where is the spa?",
            "The spa is...", 0.65m, "Spa",
            30, 0, 0, "RuleEngine-v1", AiInteractionSource.GuestApp);

        interaction.EditAndApprove("The spa is located in the wellness wing.", Guid.NewGuid());

        interaction.WasEdited.Should().BeTrue();
        interaction.WasApproved.Should().BeTrue();
        interaction.EditedResponse.Should().Be("The spa is located in the wellness wing.");
        interaction.Outcome.Should().Be(AiInteractionOutcome.Edited);
    }

    // ═══════════════════════════════════════════════════════════════
    // DIGITAL CHECK-IN
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DigitalCheckIn_FullWorkflow()
    {
        var checkIn = DigitalCheckIn.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        checkIn.Status.Should().Be(DigitalCheckInStatus.Invited);
        checkIn.Token.Should().NotBeNullOrEmpty();

        checkIn.VerifyIdentity("SA_ID", "abc123hash", 0.95m);
        checkIn.IdVerified.Should().BeTrue();
        checkIn.Status.Should().Be(DigitalCheckInStatus.IdentityVerified);

        checkIn.SignRegistrationCard("base64sig", "192.168.1.1", true, false);
        checkIn.Status.Should().Be(DigitalCheckInStatus.RegistrationSigned);
        checkIn.PopiaConsentGiven.Should().BeTrue();

        var roomId = Guid.NewGuid();
        checkIn.SelectRoom(roomId, true, 350m);
        checkIn.SelectedRoomId.Should().Be(roomId);
        checkIn.RoomUpgradeSelected.Should().BeTrue();
        checkIn.UpgradeAmount.Should().Be(350m);

        checkIn.Complete();
        checkIn.Status.Should().Be(DigitalCheckInStatus.Completed);
        checkIn.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void DigitalCheckIn_MobileKey_Provisioning()
    {
        var checkIn = DigitalCheckIn.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        checkIn.ProvisionMobileKey("key-123", DateTime.UtcNow, DateTime.UtcNow.AddDays(3));

        checkIn.MobileKeyId.Should().Be("key-123");
        checkIn.MobileKeyStatus.Should().Be(MobileKeyStatus.Active);

        checkIn.RevokeMobileKey();
        checkIn.MobileKeyStatus.Should().Be(MobileKeyStatus.Revoked);
    }

    // ═══════════════════════════════════════════════════════════════
    // CONVERSATION
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GuestConversation_Lifecycle()
    {
        var conv = GuestConversation.Create(
            Guid.NewGuid(), "Booking inquiry", MessageChannel.Email);

        conv.Status.Should().Be(ConversationStatus.Open);

        var staffId = Guid.NewGuid();
        conv.AssignTo(staffId);
        conv.AssignedToStaffId.Should().Be(staffId);

        conv.Resolve();
        conv.Status.Should().Be(ConversationStatus.Resolved);
        conv.ResolvedAt.Should().NotBeNull();

        conv.Reopen();
        conv.Status.Should().Be(ConversationStatus.Open);

        conv.Close();
        conv.Status.Should().Be(ConversationStatus.Closed);
    }
}
