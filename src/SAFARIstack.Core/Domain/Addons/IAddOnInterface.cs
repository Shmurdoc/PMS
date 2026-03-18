using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SAFARIstack.Core.Domain.Addons;

/// <summary>
/// Represents a lifecycle phase of an addon
/// </summary>
public enum AddOnLifecyclePhase
{
    NotInstalled,
    Installing,
    Installed,
    Updating,
    Uninstalling,
    Failed,
    Disabled
}

/// <summary>
/// Represents an event hook type that addons can subscribe to
/// </summary>
public enum AddOnEventHook
{
    // Booking lifecycle
    OnBeforeBookingCreated,
    OnAfterBookingCreated,
    OnBeforeBookingUpdated,
    OnAfterBookingUpdated,
    OnBeforeBookingCancelled,
    OnAfterBookingCancelled,

    // Guest lifecycle
    OnBeforeGuestCheckedIn,
    OnAfterGuestCheckedIn,
    OnBeforeGuestCheckedOut,
    OnAfterGuestCheckedOut,

    // Payment events
    OnBeforePaymentProcessed,
    OnAfterPaymentProcessed,
    OnPaymentFailed,

    // Security events
    OnSecurityAlertGenerated,
    OnSecurityAlertResolved,

    // System events
    OnSystemStartup,
    OnSystemShutdown,
    OnHealthCheckRequired
}

/// <summary>
/// Result of an addon operation
/// </summary>
public record AddOnOperationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Dictionary<string, object>? Data { get; init; }
    public Exception? Error { get; init; }

    public static AddOnOperationResult Ok(string message = "Success", Dictionary<string, object>? data = null)
        => new() { Success = true, Message = message, Data = data };

    public static AddOnOperationResult Fail(string message, Exception? error = null)
        => new() { Success = false, Message = message, Error = error };
}

/// <summary>
/// Represents metadata about an addon
/// </summary>
public record AddOnMetadata
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string[] Dependencies { get; init; } = Array.Empty<string>();
    public string[] ProvidedServices { get; init; } = Array.Empty<string>();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Base interface for all addons
/// Addons use this interface to participate in the SAFARIstack ecosystem
/// </summary>
public interface IAddOn
{
    /// <summary>
    /// Unique identifier for this addon
    /// Format: "addon-<namespace>-<name>"
    /// Example: "addon-channel-manager-pro"
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable name of the addon
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Current version of the addon
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Get metadata about this addon
    /// </summary>
    AddOnMetadata GetMetadata();

    /// <summary>
    /// Called when the addon is being installed
    /// Use this to initialize databases, create tables, etc.
    /// </summary>
    Task<AddOnOperationResult> OnInstallAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the addon is being updated
    /// Use this for any necessary migrations or cleanup
    /// </summary>
    Task<AddOnOperationResult> OnUpdateAsync(string oldVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the addon is being uninstalled
    /// Use this to clean up databases, files, etc.
    /// </summary>
    Task<AddOnOperationResult> OnUninstallAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called during system startup after the addon is installed
    /// Use this to initialize services, start background tasks, etc.
    /// </summary>
    Task<AddOnOperationResult> OnInitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called during system shutdown to gracefully clean up
    /// </summary>
    Task<AddOnOperationResult> OnShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the addon is healthy
    /// Return false if the addon has encountered errors during operation
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get configuration schema for this addon (JSON Schema format)
    /// </summary>
    string GetConfigurationSchema();

    /// <summary>
    /// Get current configuration
    /// </summary>
    Task<Dictionary<string, object>> GetConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Update configuration
    /// </summary>
    Task<AddOnOperationResult> SetConfigAsync(Dictionary<string, object> config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of API routes this addon provides
    /// </summary>
    IEnumerable<AddOnApiRoute> GetApiRoutes();
}

/// <summary>
/// Represents an API route provided by an addon
/// </summary>
public record AddOnApiRoute
{
    public string Method { get; init; } = "GET"; // GET, POST, PUT, DELETE, PATCH
    public string Path { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool RequiresAuthentication { get; init; } = true;
    public string[] RequiredRoles { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Interface for addons to listen to system events
/// </summary>
public interface IAddOnEventListener
{
    /// <summary>
    /// Get list of events this addon is interested in
    /// </summary>
    IEnumerable<AddOnEventHook> GetSubscribedEvents();

    /// <summary>
    /// Handle an event
    /// </summary>
    Task<AddOnOperationResult> HandleEventAsync(
        AddOnEventHook eventHook,
        Dictionary<string, object> eventData,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for addon to access and store configuration
/// </summary>
public interface IAddOnConfiguration
{
    /// <summary>
    /// Get a configuration value
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Set a configuration value
    /// </summary>
    Task<bool> SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Delete a configuration value
    /// </summary>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all configuration values
    /// </summary>
    Task<Dictionary<string, object>> GetAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for addon to access database
/// </summary>
public interface IAddOnDataAccess
{
    /// <summary>
    /// Execute a raw SQL query
    /// </summary>
    Task<List<Dictionary<string, object>>> ExecuteQueryAsync(
        string sql,
        Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a command (INSERT, UPDATE, DELETE)
    /// </summary>
    Task<int> ExecuteCommandAsync(
        string sql,
        Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default);
}
