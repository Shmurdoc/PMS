using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.Infrastructure.Services;

/// <summary>
/// Unified Guest Inbox — Cross-channel messaging with AI-assisted reply suggestions.
/// Conversations group messages by guest/booking for staff workflow.
/// </summary>
public class GuestInboxService : IGuestInboxService
{
    private readonly ApplicationDbContext _db;
    private readonly IAiConciergeService _aiConcierge;

    public GuestInboxService(ApplicationDbContext db, IAiConciergeService aiConcierge)
    {
        _db = db;
        _aiConcierge = aiConcierge;
    }

    public async Task<GuestMessageDto> ReceiveMessageAsync(InboundMessageDto message)
    {
        var channel = Enum.TryParse<MessageChannel>(message.Channel, true, out var ch)
            ? ch : MessageChannel.Email;

        var msg = GuestMessage.CreateInbound(
            message.PropertyId, channel,
            message.SenderAddress, message.Subject, message.Body,
            message.GuestId, message.BookingId,
            message.SenderName, message.ExternalReference);

        // Find or create conversation
        var conversation = await FindOrCreateConversationAsync(
            message.PropertyId, message.GuestId, message.BookingId,
            message.Subject, channel);

        msg.GetType().GetProperty("ConversationId")!
            .SetValue(msg, conversation.Id);

        await _db.Set<GuestMessage>().AddAsync(msg);
        conversation.AddMessage(msg);

        // Only call Update for existing conversations loaded from DB.
        // New conversations are already tracked as Added — calling Update
        // would flip them to Modified, causing an update on a non-existent row.
        if (_db.Entry(conversation).State != EntityState.Added)
            _db.Set<GuestConversation>().Update(conversation);

        // Generate AI suggestion (non-blocking — catch errors gracefully)
        try
        {
            var context = new AiContextDto(
                message.PropertyId, message.GuestId, message.BookingId,
                message.SenderName, null, null, null, null, message.Channel);

            var aiResponse = await _aiConcierge.HandleInquiryAsync(message.Body, context);
            msg.SetAiSuggestion(aiResponse.Response, aiResponse.ConfidenceScore,
                Enum.TryParse<MessageIntent>(aiResponse.IntentCategory, true, out var intent) ? intent : null);
        }
        catch
        {
            // AI failure should not block message receipt
        }

        await _db.SaveChangesAsync();
        return MapToDto(msg);
    }

    public async Task<GuestMessageDto> SendReplyAsync(Guid messageId, string reply, Guid staffId)
    {
        var msg = await _db.Set<GuestMessage>().FindAsync(messageId)
            ?? throw new InvalidOperationException($"Message {messageId} not found.");

        msg.Reply(reply, staffId);
        _db.Set<GuestMessage>().Update(msg);
        await _db.SaveChangesAsync();

        return MapToDto(msg);
    }

    public async Task<GuestMessageDto> ApproveAiReplyAsync(Guid messageId, Guid staffId)
    {
        var msg = await _db.Set<GuestMessage>().FindAsync(messageId)
            ?? throw new InvalidOperationException($"Message {messageId} not found.");

        if (string.IsNullOrEmpty(msg.AiSuggestedReply))
            throw new InvalidOperationException("No AI suggestion available to approve.");

        msg.ApproveAiReply(staffId);
        _db.Set<GuestMessage>().Update(msg);
        await _db.SaveChangesAsync();

        return MapToDto(msg);
    }

    public async Task<GuestMessageDto> EditAndApproveAiReplyAsync(
        Guid messageId, string editedReply, Guid staffId)
    {
        var msg = await _db.Set<GuestMessage>().FindAsync(messageId)
            ?? throw new InvalidOperationException($"Message {messageId} not found.");

        msg.EditAndApproveAiReply(editedReply, staffId);
        _db.Set<GuestMessage>().Update(msg);
        await _db.SaveChangesAsync();

        return MapToDto(msg);
    }

    public async Task<IEnumerable<ConversationDto>> GetConversationsAsync(
        Guid propertyId, bool unreadOnly = false)
    {
        var query = _db.Set<GuestConversation>()
            .Where(c => c.PropertyId == propertyId)
            .AsNoTracking();

        if (unreadOnly)
            query = query.Where(c => c.Status == ConversationStatus.Open);

        var conversations = await query
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

        var result = new List<ConversationDto>();
        foreach (var conv in conversations)
        {
            var messages = await _db.Set<GuestMessage>()
                .Where(m => m.ConversationId == conv.Id)
                .OrderBy(m => m.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            result.Add(new ConversationDto(
                conv.Id, conv.Subject, conv.Status.ToString(),
                conv.PrimaryChannel.ToString(), conv.MessageCount,
                conv.LastMessageAt, conv.AssignedToStaffId,
                messages.Select(MapToDto)));
        }

        return result;
    }

    public async Task<ConversationDto> GetConversationAsync(Guid conversationId)
    {
        var conv = await _db.Set<GuestConversation>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new InvalidOperationException($"Conversation {conversationId} not found.");

        var messages = await _db.Set<GuestMessage>()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        return new ConversationDto(
            conv.Id, conv.Subject, conv.Status.ToString(),
            conv.PrimaryChannel.ToString(), conv.MessageCount,
            conv.LastMessageAt, conv.AssignedToStaffId,
            messages.Select(MapToDto));
    }

    public async Task AssignConversationAsync(Guid conversationId, Guid staffId)
    {
        var conv = await _db.Set<GuestConversation>().FindAsync(conversationId)
            ?? throw new InvalidOperationException($"Conversation {conversationId} not found.");

        conv.AssignTo(staffId);
        _db.Set<GuestConversation>().Update(conv);
        await _db.SaveChangesAsync();
    }

    public async Task ResolveConversationAsync(Guid conversationId)
    {
        var conv = await _db.Set<GuestConversation>().FindAsync(conversationId)
            ?? throw new InvalidOperationException($"Conversation {conversationId} not found.");

        conv.Resolve();
        _db.Set<GuestConversation>().Update(conv);
        await _db.SaveChangesAsync();
    }

    // ─── Private Helpers ─────────────────────────────────────────────

    private async Task<GuestConversation> FindOrCreateConversationAsync(
        Guid propertyId, Guid? guestId, Guid? bookingId,
        string subject, MessageChannel channel)
    {
        // Try to find open conversation for same guest + property
        GuestConversation? existing = null;
        if (guestId.HasValue)
        {
            existing = await _db.Set<GuestConversation>()
                .Where(c => c.PropertyId == propertyId
                    && c.GuestId == guestId
                    && c.Status == ConversationStatus.Open)
                .OrderByDescending(c => c.LastMessageAt)
                .FirstOrDefaultAsync();
        }

        if (existing != null) return existing;

        var conversation = GuestConversation.Create(
            propertyId, subject, channel, guestId, bookingId);

        await _db.Set<GuestConversation>().AddAsync(conversation);
        return conversation;
    }

    private static GuestMessageDto MapToDto(GuestMessage msg)
    {
        return new GuestMessageDto(
            msg.Id, msg.Channel.ToString(), msg.Direction.ToString(),
            msg.SenderAddress, msg.SenderName,
            msg.Subject, msg.Body, msg.Status.ToString(),
            msg.AiSuggestedReply, msg.AiConfidenceScore,
            msg.CreatedAt, msg.RepliedAt);
    }
}
