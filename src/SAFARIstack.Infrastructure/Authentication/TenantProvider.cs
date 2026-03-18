using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;

namespace SAFARIstack.Infrastructure.Authentication;

/// <summary>
/// Extracts tenant context (PropertyId) from the current HTTP request's JWT claims.
/// Used by ApplicationDbContext to automatically scope all queries to the current tenant.
/// </summary>
public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _cachedPropertyId;
    private bool? _cachedIsSuperAdmin;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid CurrentPropertyId
    {
        get
        {
            if (_cachedPropertyId.HasValue)
                return _cachedPropertyId.Value;

            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                _cachedPropertyId = Guid.Empty;
                return Guid.Empty;
            }

            var propertyIdClaim = user.FindFirst("propertyId")?.Value;
            _cachedPropertyId = Guid.TryParse(propertyIdClaim, out var pid) ? pid : Guid.Empty;
            return _cachedPropertyId.Value;
        }
    }

    public bool IsSuperAdmin
    {
        get
        {
            if (_cachedIsSuperAdmin.HasValue)
                return _cachedIsSuperAdmin.Value;

            var user = _httpContextAccessor.HttpContext?.User;
            _cachedIsSuperAdmin = user?.IsInRole(SystemRoles.SuperAdmin) == true;
            return _cachedIsSuperAdmin.Value;
        }
    }

    public bool HasTenantContext =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true
        && CurrentPropertyId != Guid.Empty;

    public void ValidatePropertyAccess(Guid requestedPropertyId)
    {
        if (IsSuperAdmin)
            return; // SuperAdmin can access all properties

        if (!HasTenantContext)
            throw new UnauthorizedAccessException("No tenant context available. User must be authenticated.");

        if (CurrentPropertyId != requestedPropertyId)
            throw new UnauthorizedAccessException(
                $"Access denied. You do not have permission to access data for the requested property.");
    }
}
