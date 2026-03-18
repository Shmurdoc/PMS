using SAFARIstack.Shared.Exceptions;

namespace SAFARIstack.Core.Domain.Exceptions.OTA;

/// <summary>Base exception for all OTA-related errors</summary>
public class OTAException : DomainException
{
    public override string ErrorCode => "OTA_ERROR";
    public override int StatusCode => 400;

    public OTAException(string message) : base(message) { }
}

/// <summary>OTA channel connection error (auth failure, invalid credentials)</summary>
public class ChannelConnectionException : OTAException
{
    public override string ErrorCode => "CHANNEL_CONNECTION_FAILED";
    public override int StatusCode => 503;

    public ChannelConnectionException(string channelName) 
        : base($"Failed to connect to {channelName}. Check credentials and API endpoint.") { }
}

/// <summary>OTA API unreachable or rate-limited</summary>
public class ChannelUnavailableException : OTAException
{
    public override string ErrorCode => "CHANNEL_UNAVAILABLE";
    public override int StatusCode => 503;

    public ChannelUnavailableException(string channelName) 
        : base($"{channelName} is temporarily unavailable. Retry after 5 minutes.") { }
}

/// <summary>Double-booking detected (same room, overlapping dates)</summary>
public class DoubleBookingException : OTAException
{
    public override string ErrorCode => "DOUBLE_BOOKING_DETECTED";
    public override int StatusCode => 409;

    public DoubleBookingException(string roomType, DateTime checkIn, DateTime checkOut) 
        : base($"Double-booking detected for {roomType} from {checkIn:yyyy-MM-dd} to {checkOut:yyyy-MM-dd}") { }
}

/// <summary>Conflicting availability states across channels</summary>
public class AvailabilityConflictException : OTAException
{
    public override string ErrorCode => "AVAILABILITY_CONFLICT";
    public override int StatusCode => 409;

    public AvailabilityConflictException(string roomType, int channelCount) 
        : base($"Availability mismatch for {roomType} across {channelCount} channels. Manual sync required.") { }
}

/// <summary>Unable to sync availability to one or more channels</summary>
public class SyncFailedException : OTAException
{
    public override string ErrorCode => "SYNC_FAILED";
    public override int StatusCode => 500;

    public SyncFailedException(string details) 
        : base($"OTA synchronization failed: {details}") { }
}

/// <summary>Invalid OTA booking reference or external ID</summary>
public class InvalidBookingReferenceException : OTAException
{
    public override string ErrorCode => "INVALID_BOOKING_REFERENCE";
    public override int StatusCode => 400;

    public InvalidBookingReferenceException(string reference) 
        : base($"Invalid OTA booking reference: {reference}") { }
}

/// <summary>Channel not connected or credentials missing</summary>
public class ChannelNotConnectedException : OTAException
{
    public override string ErrorCode => "CHANNEL_NOT_CONNECTED";
    public override int StatusCode => 400;

    public ChannelNotConnectedException(string channelName) 
        : base($"Channel '{channelName}' is not connected. Add credentials first.") { }
}

/// <summary>OTA-specific business rule violation (rate, dates, etc)</summary>
public class OTABusinessRuleException : OTAException
{
    public override string ErrorCode => "OTA_BUSINESS_RULE_VIOLATION";
    public override int StatusCode => 409;

    public OTABusinessRuleException(string channelName, string rule) 
        : base($"{channelName} business rule violation: {rule}") { }
}

/// <summary>Insufficient inventory or configuration for OTA operation</summary>
public class InsufficientOTAConfigException : OTAException
{
    public override string ErrorCode => "INSUFFICIENT_OTA_CONFIG";
    public override int StatusCode => 400;

    public InsufficientOTAConfigException(string details) 
        : base($"Insufficient OTA configuration: {details}") { }
}

/// <summary>OTA sync timeout (operations taking too long)</summary>
public class OTASyncTimeoutException : OTAException
{
    public override string ErrorCode => "OTA_SYNC_TIMEOUT";
    public override int StatusCode => 504;

    public OTASyncTimeoutException(string channelName, int timeoutSeconds) 
        : base($"OTA sync timeout for {channelName} after {timeoutSeconds}s") { }
}
