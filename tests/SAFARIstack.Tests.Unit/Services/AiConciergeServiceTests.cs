using Microsoft.EntityFrameworkCore;
using Moq;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Infrastructure.Services;

namespace SAFARIstack.Tests.Unit.Services;

/// <summary>
/// Tests for AiConciergeService — rule-based NLP intent classification + response generation.
/// </summary>
public class AiConciergeServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly AiConciergeService _service;

    public AiConciergeServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var tenantMock = new Mock<ITenantProvider>();
        tenantMock.Setup(x => x.HasTenantContext).Returns(false);
        _db = new ApplicationDbContext(options, tenantMock.Object);
        _service = new AiConciergeService(_db);
    }

    public void Dispose() => _db.Dispose();

    private static AiContextDto DefaultContext(string? guestName = null) => new(
        PropertyId: Guid.NewGuid(),
        GuestId: Guid.NewGuid(),
        BookingId: Guid.NewGuid(),
        GuestName: guestName,
        CheckInDate: DateTime.UtcNow,
        CheckOutDate: DateTime.UtcNow.AddDays(3),
        RoomType: "Deluxe Suite",
        LoyaltyTier: "Gold",
        Source: "WebChat");

    // ─── Intent Classification ───────────────────────────────────

    [Theory]
    [InlineData("What time is check-in?", "CheckIn")]
    [InlineData("I want early check in", "CheckIn")]
    [InlineData("What time is checkout?", "CheckOut")]
    [InlineData("Can I get late check-out?", "CheckOut")]
    [InlineData("What is the WiFi password?", "WiFi")]
    [InlineData("How do I connect to the internet?", "WiFi")]
    [InlineData("Can I order room service for breakfast?", "RoomService")]
    [InlineData("I need extra towels please", "Housekeeping")]
    [InlineData("Can housekeeping come clean my room?", "Housekeeping")]
    [InlineData("Do you have a spa or massage?", "Spa")]
    [InlineData("When is the next game drive safari?", "Safari")]
    [InlineData("Can I see the big five here?", "Safari")]
    [InlineData("I want to reserve a table at the restaurant", "Restaurant")]
    [InlineData("I need an airport transfer please", "Transport")]
    [InlineData("Is there parking available?", "Transport")]
    [InlineData("What activities and tours do you offer?", "Activities")]
    [InlineData("Can I see my bill and invoice?", "Billing")]
    [InlineData("I have a question about a charge on my account", "Billing")]
    [InlineData("The air conditioning is broken and not working", "Complaint")]
    [InlineData("Are there any shops nearby?", "LocalInfo")]
    [InlineData("Where is the nearest ATM or bank?", "LocalInfo")]
    [InlineData("How many loyalty points and rewards do I have?", "Loyalty")]
    [InlineData("I need to cancel my booking and get a refund", "Cancellation")]
    public async Task HandleInquiryAsync_ClassifiesIntentCorrectly(string question, string expectedIntent)
    {
        var result = await _service.HandleInquiryAsync(question, DefaultContext());

        result.IntentCategory.Should().Be(expectedIntent);
        result.ConfidenceScore.Should().BeGreaterThan(0.3m);
        result.Response.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task HandleInquiryAsync_UnknownQuery_ReturnsGeneralWithLowConfidence()
    {
        var result = await _service.HandleInquiryAsync(
            "Tell me about quantum physics", DefaultContext());

        result.IntentCategory.Should().Be("General");
        result.ConfidenceScore.Should().BeLessOrEqualTo(0.5m);
    }

    // ─── Personalization ─────────────────────────────────────────

    [Fact]
    public async Task HandleInquiryAsync_WithGuestName_PersonalizesResponse()
    {
        var ctx = DefaultContext("Sarah");

        var result = await _service.HandleInquiryAsync("What time is check in?", ctx);

        result.Response.Should().StartWith("Hi Sarah!");
    }

    [Fact]
    public async Task HandleInquiryAsync_WithoutGuestName_NoPersonalGreeting()
    {
        var ctx = DefaultContext(null);

        var result = await _service.HandleInquiryAsync("What time is check in?", ctx);

        result.Response.Should().NotStartWith("Hi ");
    }

    // ─── Auto-Send Threshold ─────────────────────────────────────

    [Fact]
    public async Task HandleInquiryAsync_HighConfidence_AutoSendsTrue()
    {
        // WiFi with multiple keywords should score high
        var result = await _service.HandleInquiryAsync(
            "What is the wifi password to connect to the internet?", DefaultContext());

        result.ConfidenceScore.Should().BeGreaterOrEqualTo(0.5m);
    }

    [Fact]
    public async Task HandleInquiryAsync_RecordsInteractionInDatabase()
    {
        var ctx = DefaultContext("Test Guest");

        await _service.HandleInquiryAsync("What time is checkout?", ctx);

        var saved = await _db.Set<AiInteraction>().FirstOrDefaultAsync();
        saved.Should().NotBeNull();
        saved!.Query.Should().Contain("checkout");
        saved.IntentCategory.Should().Be("CheckOut");
        saved.PropertyId.Should().Be(ctx.PropertyId);
    }

    [Fact]
    public async Task HandleInquiryAsync_ReturnsInteractionId()
    {
        var result = await _service.HandleInquiryAsync(
            "I need towels", DefaultContext());

        result.InteractionId.Should().NotBe(Guid.Empty);
    }

    // ─── Analytics ───────────────────────────────────────────────

    [Fact]
    public async Task GetAiAnalyticsAsync_EmptyPeriod_ReturnsZeroCounts()
    {
        var propId = Guid.NewGuid();

        var analytics = await _service.GetAiAnalyticsAsync(
            propId, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        analytics.TotalInteractions.Should().Be(0);
        analytics.AverageConfidence.Should().Be(0);
    }

    [Fact]
    public async Task GetAiAnalyticsAsync_WithData_CalculatesCorrectly()
    {
        var propId = Guid.NewGuid();
        var ctx = new AiContextDto(propId, null, null, null, null, null, null, null, "WebChat");

        // Generate several interactions
        await _service.HandleInquiryAsync("What is the wifi password?", ctx);
        await _service.HandleInquiryAsync("When is checkout?", ctx);
        await _service.HandleInquiryAsync("I need extra towels", ctx);

        var analytics = await _service.GetAiAnalyticsAsync(
            propId, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));

        analytics.TotalInteractions.Should().Be(3);
        analytics.AverageConfidence.Should().BeGreaterThan(0);
        analytics.ByIntent.Should().NotBeEmpty();
    }

    // ─── Improvement Suggestions ─────────────────────────────────

    [Fact]
    public async Task GetImprovementSuggestionsAsync_NoData_ReturnsEmpty()
    {
        var suggestions = await _service.GetImprovementSuggestionsAsync(Guid.NewGuid());

        suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetImprovementSuggestionsAsync_WithLowConfidence_SuggestsImprovement()
    {
        var propId = Guid.NewGuid();

        // Create interactions with low confidence (unclassified)
        for (int i = 0; i < 10; i++)
        {
            var interaction = AiInteraction.Create(
                propId, $"Random question {i}", "Response",
                0.3m, null, // null intent = unclassified
                10, 0, 0, "RuleEngine-v1", AiInteractionSource.WebChat);
            await _db.Set<AiInteraction>().AddAsync(interaction);
        }
        await _db.SaveChangesAsync();

        var suggestions = await _service.GetImprovementSuggestionsAsync(propId);

        suggestions.Should().NotBeEmpty();
        suggestions.Should().Contain(s => s.IntentCategory == "Unclassified");
    }
}
