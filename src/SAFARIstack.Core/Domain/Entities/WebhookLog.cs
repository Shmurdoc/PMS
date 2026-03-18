using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

/// <summary>
/// Webhook event log for audit and replay capability
/// Tracks all incoming webhooks from payment gateways and other integrations
/// </summary>
public class WebhookLog : AuditableEntity
{
    public string Provider { get; set; } = string.Empty;           // "Ozow", "PayFast", "Stripe", etc.
    public string TransactionId { get; set; } = string.Empty;       // External transaction ID
    public string Status { get; set; } = string.Empty;              // payment status
    public string Payload { get; set; } = string.Empty;             // Raw request body
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public bool IsProcessed { get; set; }
    public string? ProcessingError { get; set; }

    private WebhookLog() { }

    public static WebhookLog Create(
        string provider,
        string transactionId,
        string status,
        string payload)
    {
        return new WebhookLog
        {
            Provider = provider,
            TransactionId = transactionId,
            Status = status,
            Payload = payload,
            ProcessedAt = DateTime.UtcNow,
            IsProcessed = false
        };
    }

    public void MarkAsProcessed(bool success, string? error = null)
    {
        IsProcessed = true;
        ProcessingError = error;
        UpdatedAt = DateTime.UtcNow;
    }
}
