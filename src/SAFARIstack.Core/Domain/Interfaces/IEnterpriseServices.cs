namespace SAFARIstack.Core.Domain.Interfaces;

// ═══════════════════════════════════════════════════════════════════════
// ENTERPRISE UPGRADE SERVICE INTERFACES
// ═══════════════════════════════════════════════════════════════════════

// ─── Multi-Property Management ───────────────────────────────────────
public interface IMultiPropertyService
{
    Task<GroupDashboardDto> GetGroupDashboardAsync(Guid groupId, DateTime startDate, DateTime endDate);
    Task<RateCopyResultDto> CopyRatesAcrossPropertiesAsync(Guid sourcePropertyId, IEnumerable<Guid> targetPropertyIds, RateCopyOptionsDto options);
    Task<GroupAllocationResultDto> AllocateGroupInventoryAsync(Guid groupId, GroupInventoryRequestDto request);
    Task<IEnumerable<PropertyComparisonDto>> GetPropertyComparisonAsync(Guid groupId, DateTime startDate, DateTime endDate);
}

// ─── Upsell Engine ───────────────────────────────────────────────────
public interface IUpsellEngine
{
    Task<IEnumerable<UpsellOfferDto>> GeneratePersonalizedOffersAsync(Guid bookingId, Guid guestId, Guid propertyId);
    Task<UpsellPurchaseResultDto> PurchaseUpsellAsync(Guid offerId, Guid bookingId, Guid guestId, int quantity = 1);
    Task<UpsellAnalyticsDto> GetUpsellAnalyticsAsync(Guid propertyId, DateTime startDate, DateTime endDate);
}

// ─── Gift Card Commerce ──────────────────────────────────────────────
public interface IGiftCardService
{
    Task<GiftCardDto> CreateGiftCardAsync(CreateGiftCardRequestDto request);
    Task<GiftCardBalanceDto> CheckBalanceAsync(string cardNumber, string pin);
    Task<GiftCardRedemptionResultDto> RedeemAsync(string cardNumber, string pin, decimal amount, Guid propertyId, Guid? bookingId = null, Guid? folioId = null);
    Task<IEnumerable<GiftCardDto>> GetGiftCardsByPropertyAsync(Guid propertyId);
    Task VoidGiftCardAsync(Guid giftCardId, string reason);
}

// ─── Experience Booking ──────────────────────────────────────────────
public interface IExperienceBookingService
{
    Task<IEnumerable<ExperienceDto>> GetAvailableExperiencesAsync(Guid propertyId, DateTime date, int participants);
    Task<ExperienceBookingResultDto> BookExperienceAsync(BookExperienceRequestDto request);
    Task<ExperienceBookingResultDto> CancelExperienceBookingAsync(Guid experienceBookingId, string reason);
    Task RecordFeedbackAsync(Guid experienceBookingId, int score, string? notes);
    Task<ExperienceAnalyticsDto> GetExperienceAnalyticsAsync(Guid propertyId, DateTime startDate, DateTime endDate);
}

// ─── Unified Guest Inbox ─────────────────────────────────────────────
public interface IGuestInboxService
{
    Task<GuestMessageDto> ReceiveMessageAsync(InboundMessageDto message);
    Task<GuestMessageDto> SendReplyAsync(Guid messageId, string reply, Guid staffId);
    Task<GuestMessageDto> ApproveAiReplyAsync(Guid messageId, Guid staffId);
    Task<GuestMessageDto> EditAndApproveAiReplyAsync(Guid messageId, string editedReply, Guid staffId);
    Task<IEnumerable<ConversationDto>> GetConversationsAsync(Guid propertyId, bool unreadOnly = false);
    Task<ConversationDto> GetConversationAsync(Guid conversationId);
    Task AssignConversationAsync(Guid conversationId, Guid staffId);
    Task ResolveConversationAsync(Guid conversationId);
}

// ─── AI Concierge ────────────────────────────────────────────────────
public interface IAiConciergeService
{
    Task<AiConciergeResponseDto> HandleInquiryAsync(string question, AiContextDto context);
    Task<AiAnalyticsDto> GetAiAnalyticsAsync(Guid propertyId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<AiPromptImprovementDto>> GetImprovementSuggestionsAsync(Guid propertyId);
}

// ─── Report Generation ───────────────────────────────────────────────
public interface IReportService
{
    Task<byte[]> GenerateDailyOperationsReportAsync(Guid propertyId, DateTime date, ReportFormat format);
    Task<byte[]> GenerateMonthlyFinancialReportAsync(Guid propertyId, int year, int month, ReportFormat format);
    Task<byte[]> GenerateOccupancyReportAsync(Guid propertyId, DateTime startDate, DateTime endDate, ReportFormat format);
    Task<byte[]> GenerateRevenueReportAsync(Guid propertyId, DateTime startDate, DateTime endDate, ReportFormat format);
    Task<byte[]> GenerateGuestAnalyticsReportAsync(Guid propertyId, DateTime startDate, DateTime endDate, ReportFormat format);
    Task<byte[]> GenerateHousekeepingReportAsync(Guid propertyId, DateTime date, ReportFormat format);
    Task<byte[]> GenerateStaffPerformanceReportAsync(Guid propertyId, DateTime startDate, DateTime endDate, ReportFormat format);
    Task<byte[]> GenerateUpsellPerformanceReportAsync(Guid propertyId, DateTime startDate, DateTime endDate, ReportFormat format);
    Task<byte[]> GenerateGroupConsolidatedReportAsync(Guid groupId, DateTime startDate, DateTime endDate, ReportFormat format);
    Task<byte[]> GenerateAiConciergeReportAsync(Guid propertyId, DateTime startDate, DateTime endDate, ReportFormat format);
}

// ─── Contactless Check-in ────────────────────────────────────────────
public interface IDigitalCheckInService
{
    Task<DigitalCheckInDto> InitiateCheckInAsync(Guid bookingId);
    Task<DigitalCheckInDto> VerifyIdentityAsync(Guid checkInId, Stream idDocumentImage);
    Task<IEnumerable<AvailableRoomDto>> GetEligibleRoomsAsync(Guid checkInId);
    Task<DigitalCheckInDto> SelectRoomAsync(Guid checkInId, Guid roomId, bool isUpgrade, decimal upgradeAmount);
    Task<DigitalCheckInDto> SignRegistrationCardAsync(Guid checkInId, string signatureData, string ipAddress, bool popiaConsent, bool marketingConsent);
    Task<DigitalCheckInDto> CompleteCheckInAsync(Guid checkInId);
}

// ═══════════════════════════════════════════════════════════════════════
// DTO RECORDS
// ═══════════════════════════════════════════════════════════════════════

public enum ReportFormat { Pdf, Excel, Csv }

// Multi-Property DTOs
public record GroupDashboardDto(
    string GroupName,
    int PropertyCount,
    decimal TotalRevenue,
    decimal AverageOccupancy,
    decimal AverageADR,
    decimal TotalRevPAR,
    IEnumerable<PropertySummaryDto> Properties);

public record PropertySummaryDto(
    Guid PropertyId,
    string PropertyName,
    decimal Revenue,
    decimal Occupancy,
    decimal ADR,
    decimal RevPAR,
    int TotalBookings,
    int TotalGuests);

public record PropertyComparisonDto(
    Guid PropertyId,
    string PropertyName,
    decimal Revenue,
    decimal Occupancy,
    decimal ADR,
    decimal RevPAR,
    decimal GuestSatisfaction,
    decimal UpsellRevenue);

public record RateCopyOptionsDto(
    bool CopyRatePlans = true,
    bool CopySeasons = true,
    bool CopyRestrictions = true,
    decimal RateAdjustmentPercentage = 0,
    bool OverrideExisting = false,
    DateTime? EffectiveFrom = null,
    DateTime? EffectiveTo = null);

public record RateCopyResultDto(bool Success, int RatesCopied, string? ErrorMessage);

public record GroupInventoryRequestDto(
    Guid RoomTypeId,
    DateTime StartDate,
    DateTime EndDate,
    int AllocatedRooms,
    int SellLimitPerProperty);

public record GroupAllocationResultDto(bool Success, Guid AllocationId, string? ErrorMessage);

// Upsell DTOs
public record UpsellOfferDto(
    Guid Id, string Title, string? Description,
    string OfferType, decimal OriginalPrice, decimal OfferPrice,
    decimal Savings, string? ImageUrl, DateTime? ValidTo);

public record UpsellPurchaseResultDto(bool Success, Guid? TransactionId, decimal TotalAmount, string? ErrorMessage);

public record UpsellAnalyticsDto(
    decimal TotalRevenue,
    int TotalTransactions,
    decimal ConversionRate,
    decimal AverageOrderValue,
    IEnumerable<UpsellOfferPerformanceDto> OfferPerformance);

public record UpsellOfferPerformanceDto(
    Guid OfferId, string Title, string OfferType,
    int Views, int Purchases, decimal Revenue, decimal ConversionRate);

// Gift Card DTOs
public record GiftCardDto(
    Guid Id, string CardNumber, decimal InitialBalance,
    decimal CurrentBalance, string Status,
    string? RecipientName, string? RecipientEmail,
    DateTime? ExpiryDate, DateTime CreatedAt);

public record CreateGiftCardRequestDto(
    Guid PropertyId, decimal Amount,
    string? RecipientName, string? RecipientEmail,
    string? SenderName, string? SenderEmail,
    DateTime? ScheduledDeliveryDate, DateTime? ExpiryDate,
    string? DesignTemplate, string? PersonalMessage,
    bool IsMultiPropertyRedeemable = true);

public record GiftCardBalanceDto(string CardNumber, decimal CurrentBalance, string Status, DateTime? ExpiryDate);

public record GiftCardRedemptionResultDto(bool Success, decimal AmountRedeemed, decimal RemainingBalance, string? ErrorMessage);

// Experience DTOs
public record ExperienceDto(
    Guid Id, string Name, string? Description,
    string Category, int DurationMinutes,
    decimal BasePrice, bool PricePerPerson,
    int AvailableSlots, string? ImageUrl,
    string DifficultyLevel, string? Location);

public record BookExperienceRequestDto(
    Guid PropertyId, Guid GuestId, Guid ExperienceId,
    DateTime ScheduledDate, TimeOnly ScheduledTime,
    int ParticipantCount, Guid? BookingId = null,
    Guid? ScheduleId = null, string? SpecialRequests = null);

public record ExperienceBookingResultDto(
    bool Success, Guid? ExperienceBookingId,
    decimal TotalPrice, string? ErrorMessage);

public record ExperienceAnalyticsDto(
    int TotalBookings, decimal TotalRevenue,
    decimal AverageRating, decimal CommissionPaid,
    IEnumerable<ExperiencePerformanceDto> ByExperience);

public record ExperiencePerformanceDto(
    Guid ExperienceId, string Name, string Category,
    int Bookings, decimal Revenue, decimal AverageRating);

// Messaging DTOs
public record GuestMessageDto(
    Guid Id, string Channel, string Direction,
    string SenderAddress, string? SenderName,
    string Subject, string Body, string Status,
    string? AiSuggestedReply, decimal? AiConfidenceScore,
    DateTime CreatedAt, DateTime? RepliedAt);

public record InboundMessageDto(
    Guid PropertyId, string Channel,
    string SenderAddress, string Subject, string Body,
    string? SenderName = null, Guid? GuestId = null,
    Guid? BookingId = null, string? ExternalReference = null);

public record ConversationDto(
    Guid Id, string Subject, string Status,
    string PrimaryChannel, int MessageCount,
    DateTime LastMessageAt, Guid? AssignedToStaffId,
    IEnumerable<GuestMessageDto> Messages);

// AI DTOs
public record AiConciergeResponseDto(
    string Response,
    decimal ConfidenceScore,
    string? IntentCategory,
    bool AutoSent,
    Guid InteractionId);

public record AiContextDto(
    Guid PropertyId,
    Guid? GuestId,
    Guid? BookingId,
    string? GuestName,
    DateTime? CheckInDate,
    DateTime? CheckOutDate,
    string? RoomType,
    string? LoyaltyTier,
    string Source); // WebChat, SMS, etc.

public record AiAnalyticsDto(
    int TotalInteractions,
    decimal AverageConfidence,
    decimal AutoSendRate,
    decimal ApprovalRate,
    decimal EditRate,
    decimal AverageGuestSatisfaction,
    decimal TotalCost,
    IEnumerable<AiIntentBreakdownDto> ByIntent);

public record AiIntentBreakdownDto(
    string Intent, int Count,
    decimal AverageConfidence,
    decimal ApprovalRate);

public record AiPromptImprovementDto(
    string IntentCategory,
    string CurrentIssue,
    string SuggestedImprovement,
    int OccurrenceCount);

// Digital Check-in DTOs
public record DigitalCheckInDto(
    Guid Id, Guid BookingId, string Status,
    bool IdVerified, bool RegistrationSigned,
    Guid? SelectedRoomId, string? MobileKeyStatus,
    DateTime? CompletedAt);

public record AvailableRoomDto(
    Guid RoomId, string RoomNumber,
    string? Floor, string? Wing,
    string RoomTypeName, decimal SizeInSqm,
    string? ViewType, bool IsUpgrade,
    decimal UpgradePrice, string? Description);
