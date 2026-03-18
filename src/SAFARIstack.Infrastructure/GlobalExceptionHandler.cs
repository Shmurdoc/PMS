using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SAFARIstack.Infrastructure;

/// <summary>
/// Global exception handler that returns RFC 7807 ProblemDetails responses
/// with correlation ID for end-to-end tracing.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Get correlation ID from middleware
        var correlationId = httpContext.Items["CorrelationId"]?.ToString() ?? "unknown";

        var (statusCode, title, detail, errorCode, errors) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                "One or more validation errors occurred.",
                "VALIDATION_ERROR",
                validationEx.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }).ToArray() as object
            ),

            ArgumentException argEx => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                argEx.Message,
                "BAD_REQUEST",
                (object?)null
            ),

            InvalidOperationException invOpEx => (
                StatusCodes.Status409Conflict,
                "Business Rule Violation",
                invOpEx.Message,
                "BUSINESS_RULE_VIOLATION",
                (object?)null
            ),

            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Concurrency Conflict",
                "The record was modified by another user. Please refresh and try again.",
                "CONCURRENCY_CONFLICT",
                (object?)null
            ),

            KeyNotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                "Not Found",
                notFoundEx.Message,
                "NOT_FOUND",
                (object?)null
            ),

            UnauthorizedAccessException unauthEx when unauthEx.Message.Contains("property") => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                unauthEx.Message,
                "TENANT_ACCESS_DENIED",
                (object?)null
            ),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required to access this resource.",
                "UNAUTHORIZED",
                (object?)null
            ),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please try again later.",
                "INTERNAL_ERROR",
                (object?)null
            )
        };

        // Structured logging with correlation context
        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception,
                "Unhandled exception | CorrelationId={CorrelationId} | Path={Path} | Method={Method} | Message={Message}",
                correlationId, httpContext.Request.Path, httpContext.Request.Method, exception.Message);
        }
        else
        {
            _logger.LogWarning(
                "Handled exception ({StatusCode}) | CorrelationId={CorrelationId} | Path={Path} | Code={ErrorCode} | Message={Message}",
                statusCode, correlationId, httpContext.Request.Path, errorCode, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        // Add structured extensions
        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
