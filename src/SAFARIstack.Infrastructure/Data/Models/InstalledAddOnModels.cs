using System;
using SAFARIstack.Core.Domain.Addons;

namespace SAFARIstack.Infrastructure.Data.Models;

/// <summary>
/// Database model for installed addons
/// </summary>
public class InstalledAddOn
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique addon identifier
    /// </summary>
    public string AddOnId { get; set; } = string.Empty;

    /// <summary>
    /// Addon name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Currently installed version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Lifecycle status
    /// </summary>
    public AddOnLifecyclePhase Status { get; set; }

    /// <summary>
    /// Whether the addon is currently enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// When the addon was installed
    /// </summary>
    public DateTime InstalledAt { get; set; }

    /// <summary>
    /// When the addon was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Configuration data (JSON)
    /// </summary>
    public string ConfigJson { get; set; } = "{}";

    /// <summary>
    /// Last error message (if any)
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Number of times this addon has been updated
    /// </summary>
    public int UpdateCount { get; set; }
}

/// <summary>
/// Database model for addon configurations
/// </summary>
public class AddOnConfiguration
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to installed addon
    /// </summary>
    public Guid InstalledAddOnId { get; set; }

    /// <summary>
    /// Configuration key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Configuration value (JSON)
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is encrypted
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// When this was created/updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public InstalledAddOn? InstalledAddOn { get; set; }
}

/// <summary>
/// Database model for addon event subscriptions
/// </summary>
public class AddOnEventSubscription
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to installed addon
    /// </summary>
    public Guid InstalledAddOnId { get; set; }

    /// <summary>
    /// Event hook name
    /// </summary>
    public string EventHookName { get; set; } = string.Empty;

    /// <summary>
    /// When subscription was created
    /// </summary>
    public DateTime SubscribedAt { get; set; }

    // Navigation properties
    public InstalledAddOn? InstalledAddOn { get; set; }
}
