using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;
using System.Diagnostics;

namespace SAFARIstack.Infrastructure.Services;

/// <summary>
/// AI Concierge — Rule-based NLP engine for guest inquiries.
/// Uses pattern matching + property knowledge base. No OpenAI dependency.
/// Future: Swap engine for Azure OpenAI when available.
/// </summary>
public class AiConciergeService : IAiConciergeService
{
    private readonly ApplicationDbContext _db;

    // Intent patterns: keyword groups → intent category → response template
    private static readonly Dictionary<string, (string Intent, string[] Keywords)> IntentPatterns = new()
    {
        ["CheckIn"] = ("CheckIn", new[] { "check in", "check-in", "checkin", "arrival", "arrive", "early check" }),
        ["CheckOut"] = ("CheckOut", new[] { "check out", "check-out", "checkout", "departure", "depart", "late check" }),
        ["WiFi"] = ("WiFi", new[] { "wifi", "wi-fi", "internet", "password", "network", "connect" }),
        ["RoomService"] = ("RoomService", new[] { "room service", "food", "menu", "breakfast", "lunch", "dinner", "meal", "order", "hungry" }),
        ["Housekeeping"] = ("Housekeeping", new[] { "clean", "towel", "linen", "housekeeping", "tidy", "amenities", "toiletries", "soap", "shampoo" }),
        ["Spa"] = ("Spa", new[] { "spa", "massage", "wellness", "treatment", "relax", "sauna", "jacuzzi", "pool" }),
        ["Safari"] = ("Safari", new[] { "safari", "game drive", "bush walk", "wildlife", "animal", "bird", "big five", "big 5" }),
        ["Restaurant"] = ("Restaurant", new[] { "restaurant", "dining", "reserve", "table", "eat", "bar", "drink", "cocktail", "wine" }),
        ["Transport"] = ("Transport", new[] { "transfer", "transport", "airport", "shuttle", "taxi", "car", "parking", "uber" }),
        ["Activities"] = ("Activities", new[] { "activities", "things to do", "experience", "tour", "excursion", "adventure", "quad", "zip", "kayak" }),
        ["Billing"] = ("Billing", new[] { "bill", "invoice", "charge", "payment", "folio", "receipt", "account", "cost", "price" }),
        ["Complaint"] = ("Complaint", new[] { "complaint", "problem", "issue", "broken", "not working", "unhappy", "disappointed", "noise", "cold", "hot" }),
        ["LocalInfo"] = ("LocalInfo", new[] { "nearby", "local", "attraction", "shop", "mall", "pharmacy", "hospital", "atm", "bank", "petrol" }),
        ["Loyalty"] = ("Loyalty", new[] { "loyalty", "points", "rewards", "member", "tier", "upgrade", "status" }),
        ["Cancellation"] = ("Cancellation", new[] { "cancel", "cancellation", "refund", "change date", "modify booking", "reschedule" }),
    };

    // Response templates per intent
    private static readonly Dictionary<string, string> ResponseTemplates = new()
    {
        ["CheckIn"] = "Check-in is from 14:00 (2 PM). Early check-in may be available subject to room availability — please let us know your preferred time and we'll do our best. You can also check in digitally via the link in your confirmation email.",
        ["CheckOut"] = "Check-out is at 10:00 (10 AM). Late check-out until 14:00 is available at R350 subject to availability. Your final folio will be emailed to you upon departure.",
        ["WiFi"] = "Complimentary WiFi is available throughout the property. Network: 'SAFARIstack-Guest'. Password is provided on your room key card sleeve. If you need help connecting, our front desk is happy to assist.",
        ["RoomService"] = "Room service is available daily from 06:00 to 22:00. You can view our menu in the guest app or call extension 100 from your room phone. Breakfast can be delivered from 06:30.",
        ["Housekeeping"] = "Housekeeping services are provided daily between 09:00 and 14:00. For extra towels, toiletries, or a special cleaning request, please call extension 120 or message us here.",
        ["Spa"] = "Our wellness centre offers a range of African-inspired treatments. Bookings are recommended — you can book through the guest app or call extension 150. Popular treatments include the African Wellness Massage (60 min) and Marula Oil Body Wrap.",
        ["Safari"] = "Game drives depart at 05:30 (sunrise) and 16:00 (sunset). Bush walks are available at 06:00 for small groups. Bookings are essential — please book via the guest app or at the activities desk by 18:00 the evening before.",
        ["Restaurant"] = "Our restaurant serves breakfast (06:30–10:00), lunch (12:00–14:30), and dinner (18:30–21:30). Reservations for dinner are recommended, especially on weekends. You can book through the guest app or call extension 200.",
        ["Transport"] = "We offer airport transfers and local shuttles. Please contact our front desk at least 24 hours in advance. Self-parking is complimentary. We can also arrange private vehicle hire.",
        ["Activities"] = "We offer a range of experiences including game drives, bush walks, cultural tours, quad biking, and more. Check the guest app for a full list with availability and pricing, or visit our activities desk in the main lodge.",
        ["Billing"] = "You can view your current folio balance through the guest app. For billing inquiries, our front desk team is available 24/7. A detailed invoice will be emailed upon checkout.",
        ["Complaint"] = "We sincerely apologize for any inconvenience. Your comfort is our priority. I've flagged this to our duty manager who will follow up with you shortly. Is there anything immediate we can do to help?",
        ["LocalInfo"] = "Our front desk has information on local attractions, shops, and essential services. We're happy to help with directions or recommendations. A local area guide is available in your room and on the guest app.",
        ["Loyalty"] = "Thank you for being a valued loyalty member! You can check your points balance and tier status in the guest app. Earning is automatic on all direct bookings. Speak with our front desk about exclusive member benefits.",
        ["Cancellation"] = "For cancellation or date changes, please refer to the cancellation policy in your booking confirmation. Our reservations team can assist — call us or reply to your booking confirmation email. Changes are subject to availability.",
    };

    public AiConciergeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AiConciergeResponseDto> HandleInquiryAsync(string question, AiContextDto context)
    {
        var sw = Stopwatch.StartNew();

        var (intent, confidence) = ClassifyIntent(question);
        var response = GenerateResponse(intent, confidence, context);

        sw.Stop();

        // Record the interaction for analytics
        var interaction = AiInteraction.Create(
            context.PropertyId,
            question,
            response,
            confidence,
            intent,
            (int)sw.ElapsedMilliseconds,
            0, // No token usage for rule-based
            0, // No cost for rule-based
            "SAFARIstack-RuleEngine-v1",
            Enum.TryParse<AiInteractionSource>(context.Source, true, out var src)
                ? src : AiInteractionSource.WebChat,
            context.GuestId,
            context.BookingId);

        // Auto-send high-confidence responses
        if (confidence >= 0.85m)
            interaction.SetAutoSent();

        await _db.Set<AiInteraction>().AddAsync(interaction);
        await _db.SaveChangesAsync();

        return new AiConciergeResponseDto(
            response, confidence, intent,
            confidence >= 0.85m,
            interaction.Id);
    }

    public async Task<AiAnalyticsDto> GetAiAnalyticsAsync(
        Guid propertyId, DateTime startDate, DateTime endDate)
    {
        var interactions = await _db.Set<AiInteraction>()
            .Where(i => i.PropertyId == propertyId
                && i.CreatedAt >= startDate && i.CreatedAt <= endDate)
            .AsNoTracking()
            .ToListAsync();

        var total = interactions.Count;
        if (total == 0)
            return new AiAnalyticsDto(0, 0, 0, 0, 0, 0, 0, Enumerable.Empty<AiIntentBreakdownDto>());

        var avgConfidence = interactions.Average(i => i.ConfidenceScore);
        var autoSent = interactions.Count(i => i.Outcome == AiInteractionOutcome.AutoSent);
        var approved = interactions.Count(i => i.Outcome == AiInteractionOutcome.Approved);
        var edited = interactions.Count(i => i.Outcome == AiInteractionOutcome.Edited);
        var withSatisfaction = interactions.Where(i => i.GuestSatisfaction.HasValue).ToList();
        var avgSatisfaction = withSatisfaction.Any()
            ? withSatisfaction.Average(i => i.GuestSatisfaction!.Value)
            : 0.0;
        var totalCost = interactions.Sum(i => i.Cost);

        var byIntent = interactions
            .GroupBy(i => i.IntentCategory ?? "Unknown")
            .Select(g => new AiIntentBreakdownDto(
                g.Key,
                g.Count(),
                g.Average(i => i.ConfidenceScore),
                g.Count(i => i.Outcome == AiInteractionOutcome.Approved || i.Outcome == AiInteractionOutcome.AutoSent) / (decimal)g.Count()))
            .ToList();

        return new AiAnalyticsDto(
            total, avgConfidence,
            total > 0 ? (decimal)autoSent / total : 0,
            total > 0 ? (decimal)approved / total : 0,
            total > 0 ? (decimal)edited / total : 0,
            (decimal)avgSatisfaction,
            totalCost,
            byIntent);
    }

    public async Task<IEnumerable<AiPromptImprovementDto>> GetImprovementSuggestionsAsync(Guid propertyId)
    {
        var recentInteractions = await _db.Set<AiInteraction>()
            .Where(i => i.PropertyId == propertyId
                && i.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .AsNoTracking()
            .ToListAsync();

        var suggestions = new List<AiPromptImprovementDto>();

        // Find intents with low confidence
        var byIntent = recentInteractions.GroupBy(i => i.IntentCategory ?? "Unknown");
        foreach (var group in byIntent)
        {
            var avgConf = group.Average(i => i.ConfidenceScore);
            if (avgConf < 0.6m)
            {
                suggestions.Add(new AiPromptImprovementDto(
                    group.Key,
                    $"Low confidence ({avgConf:P0}) on {group.Count()} interactions",
                    "Add more keyword patterns or review unmatched queries for this intent category",
                    group.Count()));
            }

            var editRate = group.Count(i => i.Outcome == AiInteractionOutcome.Edited) / (decimal)group.Count();
            if (editRate > 0.3m)
            {
                suggestions.Add(new AiPromptImprovementDto(
                    group.Key,
                    $"High edit rate ({editRate:P0}) — staff frequently modify responses",
                    "Review edited responses to improve the default template for this intent",
                    group.Count(i => i.Outcome == AiInteractionOutcome.Edited)));
            }
        }

        // Find unclassified queries
        var unclassified = recentInteractions.Count(i => string.IsNullOrEmpty(i.IntentCategory));
        if (unclassified > 5)
        {
            suggestions.Add(new AiPromptImprovementDto(
                "Unclassified",
                $"{unclassified} queries could not be classified in the last 30 days",
                "Review unclassified queries to identify new intent categories to add",
                unclassified));
        }

        return suggestions;
    }

    // ─── Intent Classification (rule-based NLP) ──────────────────────

    private static (string Intent, decimal Confidence) ClassifyIntent(string question)
    {
        var normalized = question.ToLowerInvariant().Trim();
        var bestIntent = "General";
        var bestScore = 0m;

        foreach (var (key, (intent, keywords)) in IntentPatterns)
        {
            var matchCount = keywords.Count(k => normalized.Contains(k));
            if (matchCount == 0) continue;

            // Score: weighted by matches and keyword specificity
            var score = (decimal)matchCount / keywords.Length;

            // Boost for exact phrase matches
            if (keywords.Any(k => k.Length > 5 && normalized.Contains(k)))
                score = Math.Min(1.0m, score + 0.2m);

            // Boost for question words aligned with intent
            if (normalized.StartsWith("how") || normalized.StartsWith("where") ||
                normalized.StartsWith("when") || normalized.StartsWith("what"))
                score = Math.Min(1.0m, score + 0.1m);

            if (score > bestScore)
            {
                bestScore = score;
                bestIntent = intent;
            }
        }

        // Map to confidence scale (0.0 to 1.0)
        var confidence = bestScore > 0 ? Math.Min(0.95m, bestScore + 0.3m) : 0.2m;
        return (bestIntent, confidence);
    }

    private static string GenerateResponse(string intent, decimal confidence, AiContextDto context)
    {
        if (ResponseTemplates.TryGetValue(intent, out var template))
        {
            // Personalize if guest context available
            var greeting = !string.IsNullOrEmpty(context.GuestName)
                ? $"Hi {context.GuestName}! " : "";
            return greeting + template;
        }

        var defaultGreeting = !string.IsNullOrEmpty(context.GuestName)
            ? $"Thank you for reaching out, {context.GuestName}. " : "Thank you for your inquiry. ";

        return defaultGreeting +
            "I'd be happy to help. Let me connect you with a team member who can assist you directly. " +
            "In the meantime, you can check our guest app for information about services, dining, and activities.";
    }
}
