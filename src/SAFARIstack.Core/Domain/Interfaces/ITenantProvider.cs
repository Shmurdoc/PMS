namespace SAFARIstack.Core.Domain.Interfaces;

/// <summary>
/// Provides the current tenant's PropertyId from the authenticated user's JWT claims.
/// This is the cornerstone of multi-tenancy isolation — every query is automatically
/// scoped to the current tenant via EF Core global query filters.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// The PropertyId of the current authenticated user.
    /// Returns Guid.Empty if no user is authenticated (public/anonymous endpoints).
    /// </summary>
    Guid CurrentPropertyId { get; }

    /// <summary>
    /// Whether the current user is a SuperAdmin who can access all properties.
    /// SuperAdmins bypass tenant filtering for cross-property operations.
    /// </summary>
    bool IsSuperAdmin { get; }

    /// <summary>
    /// Whether there is an authenticated tenant context available.
    /// </summary>
    bool HasTenantContext { get; }

    /// <summary>
    /// Validates that the requested propertyId matches the current user's tenant.
    /// Throws UnauthorizedAccessException if mismatched (unless SuperAdmin).
    /// </summary>
    void ValidatePropertyAccess(Guid requestedPropertyId);
}
