using System.Text;
using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Services;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Infrastructure.Services.Payments.Providers;

namespace SAFARIstack.API.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/payments")
            .WithName("Payments")
            .WithOpenApi()
            .WithTags("Financial");

        group.MapPost("/ozow/initiate", InitiateOzowPayment)
            .WithName("InitiateOzowPayment")
            .WithDescription("Initiate a payment via Ozow hosted checkout")
            .RequireAuthorization();

        group.MapPost("/payfast/initiate", InitiatePayFastPayment)
            .WithName("InitiatePayFastPayment")
            .WithDescription("Initiate a payment via PayFast hosted checkout")
            .RequireAuthorization();

        group.MapGet("/{transactionId}/status", GetPaymentStatus)
            .WithName("GetPaymentStatus")
            .WithDescription("Get the current status of a payment transaction")
            .RequireAuthorization();

        group.MapPost("/webhook/ozow", HandleOzowWebhook)
            .WithName("OzowWebhook")
            .WithDescription("Receive Ozow payment status callback")
            .AllowAnonymous();

        group.MapPost("/webhook/payfast", HandlePayFastWebhook)
            .WithName("PayFastWebhook")
            .WithDescription("Receive PayFast payment status callback")
            .AllowAnonymous();
    }

    private static async Task<IResult> InitiateOzowPayment(
        InitiateOzowPaymentRequest request,
        OzowPaymentProvider ozowProvider,
        ApplicationDbContext dbContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("PaymentEndpoints");
        try
        {
            logger.LogInformation("Initiating Ozow payment: PropertyId={PropertyId} Amount={Amount}", request.PropertyId, request.Amount);

            // Verify property exists
            var property = await dbContext.Properties.FirstOrDefaultAsync(p => p.Id == request.PropertyId);
            if (property == null)
                return Results.BadRequest(new { error = "Property not found" });

            // Initiate charge via Ozow
            var result = await ozowProvider.ChargeAsync(
                request.Amount,
                request.Currency ?? "ZAR",
                request.PaymentMethodId ?? "card",
                $"Folio {request.FolioId}",
                Guid.NewGuid().ToString("N")[..50]);

            // Create payment record in database
            var payment = Payment.Create(
                request.PropertyId,
                request.FolioId,
                request.Amount,
                PaymentMethod.Ozow,
                result.ExternalChargeId,
                request.BookingId);

            dbContext.Payments.Add(payment);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new { transactionId = payment.Id, checkoutUrl = result.ReceiptUrl, status = result.Status });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initiating Ozow payment");
            return Results.StatusCode(500);
        }
    }

    private static async Task<IResult> InitiatePayFastPayment(
        InitiatePayFastPaymentRequest request,
        PayFastPaymentProvider payFastProvider,
        ApplicationDbContext dbContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("PaymentEndpoints");
        try
        {
            logger.LogInformation("Initiating PayFast payment: PropertyId={PropertyId} Amount={Amount}", request.PropertyId, request.Amount);

            // Verify property exists
            var property = await dbContext.Properties.FirstOrDefaultAsync(p => p.Id == request.PropertyId);
            if (property == null)
                return Results.BadRequest(new { error = "Property not found" });

            // Initiate charge via PayFast
            var result = await payFastProvider.ChargeAsync(
                request.Amount,
                request.Currency ?? "ZAR",
                request.PaymentMethodId ?? "credit_card",
                $"Folio {request.FolioId}",
                Guid.NewGuid().ToString("N")[..50]);

            // Create payment record in database
            var payment = Payment.Create(
                request.PropertyId,
                request.FolioId,
                request.Amount,
                PaymentMethod.PayFast,
                result.ExternalChargeId,
                request.BookingId);

            dbContext.Payments.Add(payment);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new { transactionId = payment.Id, checkoutUrl = result.ReceiptUrl, status = result.Status });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initiating PayFast payment");
            return Results.StatusCode(500);
        }
    }

    private static async Task<IResult> GetPaymentStatus(
        string transactionId,
        ApplicationDbContext dbContext,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("PaymentEndpoints");
        try
        {
            var payment = await dbContext.Payments.FirstOrDefaultAsync(p =>
                p.GatewayReference == transactionId ||
                p.Id.ToString() == transactionId);

            if (payment == null)
                return Results.NotFound(new { error = "Payment not found" });

            return Results.Ok(new { transactionId = payment.Id, status = payment.Status.ToString(), amount = payment.Amount, currency = payment.Currency });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting payment status");
            return Results.StatusCode(500);
        }
    }

    private static async Task<IResult> HandleOzowWebhook(HttpRequest request, OzowPaymentProvider ozowProvider, ApplicationDbContext dbContext, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("PaymentEndpoints");
        try
        {
            // Enable buffering to read body multiple times
            request.EnableBuffering();
            var body = await new StreamReader(request.Body, Encoding.UTF8).ReadToEndAsync();
            request.Body.Position = 0;

            // Get signature from headers
            var signature = request.Headers["X-Ozow-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
            {
                logger.LogWarning("Ozow webhook: Missing signature header");
                return Results.Unauthorized();
            }

            // Verify signature (simplified - would need actual webhook secret from config)
            if (!ozowProvider.VerifyWebhookSignature(body, signature, ""))
            {
                logger.LogWarning("Ozow webhook: Invalid signature");
                return Results.Unauthorized();
            }

            // Parse webhook JSON (simplified)
            logger.LogInformation("Ozow webhook received and verified");

            return Results.Ok(new { status = "received" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Ozow webhook");
            return Results.StatusCode(500);
        }
    }

    private static async Task<IResult> HandlePayFastWebhook(HttpRequest request, PayFastPaymentProvider payFastProvider, ApplicationDbContext dbContext, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("PaymentEndpoints");
        try
        {
            // Enable buffering to read body for signature verification
            request.EnableBuffering();
            var body = await new StreamReader(request.Body, Encoding.UTF8).ReadToEndAsync();
            request.Body.Position = 0;

            // Get signature from headers
            var signature = request.Headers["signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
            {
                logger.LogWarning("PayFast webhook: Missing signature");
                return Results.Unauthorized();
            }

            // Verify signature (simplified - would need actual webhook secret from config)
            if (!payFastProvider.VerifyWebhookSignature(body, signature, ""))
            {
                logger.LogWarning("PayFast webhook: Invalid signature");
                return Results.Unauthorized();
            }

            logger.LogInformation("PayFast webhook received and verified");

            return Results.Ok(new { status = "received" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing PayFast webhook");
            return Results.StatusCode(500);
        }
    }
}

public record InitiateOzowPaymentRequest(Guid PropertyId, Guid FolioId, decimal Amount, string? Currency = "ZAR", string? PaymentMethodId = null, Guid? BookingId = null);
public record InitiatePayFastPaymentRequest(Guid PropertyId, Guid FolioId, decimal Amount, string? Currency = "ZAR", string? PaymentMethodId = null, Guid? BookingId = null);

public record PayFastPaymentInitiationResponse(
    string TransactionId,
    string Status,
    string CheckoutUrl,
    decimal Amount,
    string Currency);

public record PaymentStatusResponse(
    string TransactionId,
    string Status,
    decimal Amount,
    string Currency,
    string Method,
    DateTime CreatedAt,
    DateTime UpdatedAt);
