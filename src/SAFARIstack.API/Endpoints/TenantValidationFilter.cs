using SAFARIstack.Core.Domain.Interfaces;
using System.Text.Json;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// Endpoint filter that validates the PropertyId parameter in the route/query/body
/// matches the authenticated user's PropertyId from JWT claims.
/// This is the second layer of multi-tenancy defense (after global query filters).
/// </summary>
public class TenantValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var tenantProvider = context.HttpContext.RequestServices
            .GetRequiredService<ITenantProvider>();

        // Skip validation for unauthenticated requests (anonymous endpoints)
        if (!tenantProvider.HasTenantContext)
            return await next(context);

        // Check route values for propertyId
        if (context.HttpContext.Request.RouteValues.TryGetValue("propertyId", out var routePropertyId))
        {
            if (Guid.TryParse(routePropertyId?.ToString(), out var propertyId))
            {
                tenantProvider.ValidatePropertyAccess(propertyId);
            }
        }

        // Check query string for propertyId
        if (context.HttpContext.Request.Query.TryGetValue("propertyId", out var queryPropertyId))
        {
            if (Guid.TryParse(queryPropertyId.FirstOrDefault(), out var propertyId))
            {
                tenantProvider.ValidatePropertyAccess(propertyId);
            }
        }

        // Check request body for PropertyId on write operations (POST, PUT, PATCH)
        var method = context.HttpContext.Request.Method;
        if (method is "POST" or "PUT" or "PATCH")
        {
            // Inspect endpoint filter arguments for objects with a PropertyId property
            foreach (var arg in context.Arguments)
            {
                if (arg is null) continue;
                var type = arg.GetType();
                var propIdProp = type.GetProperty("PropertyId");
                if (propIdProp is not null && propIdProp.PropertyType == typeof(Guid))
                {
                    var bodyPropertyId = (Guid)propIdProp.GetValue(arg)!;
                    if (bodyPropertyId != Guid.Empty)
                    {
                        tenantProvider.ValidatePropertyAccess(bodyPropertyId);
                    }
                }
            }
        }

        return await next(context);
    }
}

/// <summary>
/// Extension methods for applying tenant validation to endpoint groups and individual endpoints.
/// </summary>
public static class TenantValidationExtensions
{
    /// <summary>
    /// Adds tenant validation filter to a route group — ensures all endpoints
    /// in the group validate PropertyId against the authenticated user's JWT claim.
    /// </summary>
    public static RouteGroupBuilder RequireTenantValidation(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter<TenantValidationFilter>();
        return group;
    }

    /// <summary>
    /// Adds tenant validation filter to a single endpoint.
    /// </summary>
    public static RouteHandlerBuilder RequireTenantValidation(this RouteHandlerBuilder builder)
    {
        builder.AddEndpointFilter<TenantValidationFilter>();
        return builder;
    }
}
