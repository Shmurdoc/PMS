namespace SAFARIstack.Shared.Exceptions;

/// <summary>
/// Base exception class for domain-level exceptions (business logic violations).
/// These are business errors, not technical errors, and should be caught and
/// handled gracefully by domain services and API controllers.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Gets the error code for API responses and logging.
    /// </summary>
    public virtual string ErrorCode => GetType().Name;

    /// <summary>
    /// Gets the HTTP status code that should be returned for this exception.
    /// Default is 400 Bad Request. Override in derived classes for custom codes.
    /// </summary>
    public virtual int StatusCode => 400; // Bad Request

    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) 
        : base(message, innerException) { }
}
