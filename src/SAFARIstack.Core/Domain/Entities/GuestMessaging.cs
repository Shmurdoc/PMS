using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════
// UNIFIED GUEST INBOX — Cross-Channel Messaging with AI-Assisted Replies
// ═══════════════════════════════════════════════════════════════════════

public class GuestMessage : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid? GuestId { get; private set; }
    public Guid? BookingId { get; private set; }
    public Guid? ConversationId { get; private set; }
    public MessageChannel Channel { get; private set; }
    public MessageDirection Direction { get; private set; }
    public string SenderAddress { get; private set; } = string.Empty; // email, phone, etc.
    public string? SenderName { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public MessageStatus Status { get; private set; } = MessageStatus.Received;
    public MessagePriority Priority { get; private set; } = MessagePriority.Normal;
    public MessageIntent? DetectedIntent { get; private set; }
    public decimal? SentimentScore { get; private set; } // -1 to 1
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public Guid? ReadByStaffId { get; private set; }
    public Guid? AssignedToStaffId { get; private set; }
    public string? AiSuggestedReply { get; private set; }
    public decimal? AiConfidenceScore { get; private set; }
    public bool AiReplyApproved { get; private set; }
    public bool AiReplyEdited { get; private set; }
    public string? FinalReply { get; private set; }
    public DateTime? RepliedAt { get; private set; }
    public Guid? RepliedByStaffId { get; private set; }
    public string? ExternalReference { get; private set; } // OTA message ID etc.

    // Navigation
    public Guest? Guest { get; private set; }
    public Booking? Booking { get; private set; }
    public GuestConversation? Conversation { get; private set; }

    private GuestMessage() { }

    public static GuestMessage CreateInbound(
        Guid propertyId, MessageChannel channel,
        string senderAddress, string subject, string body,
        Guid? guestId = null, Guid? bookingId = null,
        string? senderName = null, string? externalReference = null)
    {
        return new GuestMessage
        {
            PropertyId = propertyId,
            Channel = channel,
            Direction = MessageDirection.Inbound,
            SenderAddress = senderAddress.Trim(),
            SenderName = senderName?.Trim(),
            Subject = subject.Trim(),
            Body = body.Trim(),
            GuestId = guestId,
            BookingId = bookingId,
            ExternalReference = externalReference
        };
    }

    public static GuestMessage CreateOutbound(
        Guid propertyId, MessageChannel channel,
        string recipientAddress, string subject, string body,
        Guid staffId, Guid? guestId = null, Guid? bookingId = null)
    {
        return new GuestMessage
        {
            PropertyId = propertyId,
            Channel = channel,
            Direction = MessageDirection.Outbound,
            SenderAddress = recipientAddress.Trim(),
            Subject = subject.Trim(),
            Body = body.Trim(),
            Status = MessageStatus.Sent,
            RepliedByStaffId = staffId,
            RepliedAt = DateTime.UtcNow,
            GuestId = guestId,
            BookingId = bookingId
        };
    }

    public void MarkRead(Guid staffId)
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
        ReadByStaffId = staffId;
    }

    public void AssignTo(Guid staffId) => AssignedToStaffId = staffId;

    public void SetAiSuggestion(string suggestedReply, decimal confidenceScore,
        MessageIntent? detectedIntent = null, decimal? sentimentScore = null)
    {
        AiSuggestedReply = suggestedReply;
        AiConfidenceScore = confidenceScore;
        DetectedIntent = detectedIntent;
        SentimentScore = sentimentScore;
    }

    public void ApproveAiReply(Guid staffId)
    {
        AiReplyApproved = true;
        FinalReply = AiSuggestedReply;
        RepliedByStaffId = staffId;
        RepliedAt = DateTime.UtcNow;
        Status = MessageStatus.Replied;
    }

    public void EditAndApproveAiReply(string editedReply, Guid staffId)
    {
        AiReplyEdited = true;
        AiReplyApproved = true;
        FinalReply = editedReply;
        RepliedByStaffId = staffId;
        RepliedAt = DateTime.UtcNow;
        Status = MessageStatus.Replied;
    }

    public void Reply(string reply, Guid staffId)
    {
        FinalReply = reply;
        RepliedByStaffId = staffId;
        RepliedAt = DateTime.UtcNow;
        Status = MessageStatus.Replied;
    }

    public void SetPriority(MessagePriority priority) => Priority = priority;
    public void Escalate() { Priority = MessagePriority.Urgent; Status = MessageStatus.Escalated; }
}

public class GuestConversation : AuditableEntity, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public Guid? GuestId { get; private set; }
    public Guid? BookingId { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public ConversationStatus Status { get; private set; } = ConversationStatus.Open;
    public MessageChannel PrimaryChannel { get; private set; }
    public Guid? AssignedToStaffId { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public int MessageCount { get; private set; }
    public DateTime LastMessageAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    private readonly List<GuestMessage> _messages = new();
    public IReadOnlyCollection<GuestMessage> Messages => _messages.AsReadOnly();

    private GuestConversation() { }

    public static GuestConversation Create(
        Guid propertyId, string subject, MessageChannel primaryChannel,
        Guid? guestId = null, Guid? bookingId = null)
    {
        return new GuestConversation
        {
            PropertyId = propertyId,
            Subject = subject.Trim(),
            PrimaryChannel = primaryChannel,
            GuestId = guestId,
            BookingId = bookingId
        };
    }

    public void AddMessage(GuestMessage message)
    {
        _messages.Add(message);
        MessageCount++;
        LastMessageAt = DateTime.UtcNow;
    }

    public void AssignTo(Guid staffId) => AssignedToStaffId = staffId;
    public void Resolve()
    {
        Status = ConversationStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
    }
    public void Reopen() { Status = ConversationStatus.Open; ResolvedAt = null; }
    public void Close() => Status = ConversationStatus.Closed;
}

public enum MessageChannel { Email, SMS, WhatsApp, WebChat, OTABookingCom, OTAExpedia, InApp, Push }
public enum MessageDirection { Inbound, Outbound }
public enum MessageStatus { Received, Read, Replied, Escalated, Archived, Sent, Failed }
public enum MessagePriority { Low, Normal, High, Urgent }
public enum MessageIntent
{
    BookingInquiry, BookingModification, BookingCancellation,
    CheckInInfo, CheckOutInfo, RoomRequest,
    RestaurantHours, PoolHours, SpaInfo, WifiInfo,
    Complaint, Compliment, LostAndFound,
    TransportRequest, ActivityBooking,
    GeneralQuestion, Other
}
public enum ConversationStatus { Open, Resolved, Closed }
