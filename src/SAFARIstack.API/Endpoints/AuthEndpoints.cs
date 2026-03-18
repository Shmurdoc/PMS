using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// Authentication & Authorization endpoints — Register, Login, Token refresh,
/// User management, Role assignment.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // ═══════════════════════════════════════════════════════════════
        //  PUBLIC AUTH ENDPOINTS (No authorization required)
        // ═══════════════════════════════════════════════════════════════
        var authGroup = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .AllowAnonymous()
            .WithAutoValidation();

        // ─── Register ────────────────────────────────────────────────
        authGroup.MapPost("/register", async (RegisterRequest request, IAuthService authService) =>
        {
            var result = await authService.RegisterAsync(request);
            return result.Success
                ? Results.Created($"/api/users/{result.UserId}", result)
                : Results.BadRequest(result);
        })
        .WithName("Register")
        .WithOpenApi()
        .Produces<AuthResult>(StatusCodes.Status201Created)
        .Produces<AuthResult>(StatusCodes.Status400BadRequest);

        // ─── Login ───────────────────────────────────────────────────
        authGroup.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
        {
            var result = await authService.LoginAsync(request);
            return result.Success
                ? Results.Ok(result)
                : Results.Unauthorized();
        })
        .WithName("Login")
        .WithOpenApi()
        .Produces<AuthResult>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        // ─── Refresh Token ───────────────────────────────────────────
        authGroup.MapPost("/refresh", async (RefreshTokenRequest request, IAuthService authService) =>
        {
            var result = await authService.RefreshTokenAsync(request.RefreshToken);
            return result.Success
                ? Results.Ok(result)
                : Results.Unauthorized();
        })
        .WithName("RefreshToken")
        .WithOpenApi()
        .Produces<AuthResult>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        // ═══════════════════════════════════════════════════════════════
        //  AUTHENTICATED USER ENDPOINTS
        // ═══════════════════════════════════════════════════════════════
        var meGroup = app.MapGroup("/api/auth/me")
            .WithTags("Authentication")
            .RequireAuthorization()
            .WithAutoValidation();

        // ─── Get Current User ────────────────────────────────────────
        meGroup.MapGet("/", async (HttpContext context, IAuthService authService) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var user = await authService.GetUserByIdAsync(userId.Value);
            if (user is null) return Results.NotFound();

            return Results.Ok(new
            {
                user.Id, user.Email, user.FirstName, user.LastName, user.FullName,
                user.Phone, user.AvatarUrl, user.PropertyId, user.LastLoginAt,
                Roles = user.UserRoles.Select(ur => ur.Role.Name)
            });
        })
        .WithName("GetCurrentUser")
        .WithOpenApi();

        // ─── Change Password ─────────────────────────────────────────
        meGroup.MapPost("/change-password", async (
            ChangePasswordRequest request, HttpContext context, IAuthService authService) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var success = await authService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword);
            return success ? Results.NoContent() : Results.BadRequest(new { Error = "Current password is incorrect." });
        })
        .WithName("ChangePassword")
        .WithOpenApi();

        // ─── Update Profile ──────────────────────────────────────────
        meGroup.MapPut("/profile", async (
            UpdateUserProfileRequest request, HttpContext context, IAuthService authService) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            var success = await authService.UpdateProfileAsync(
                userId.Value, request.FirstName, request.LastName, request.Phone, request.AvatarUrl);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateProfile")
        .WithOpenApi();

        // ─── Logout (Revoke Refresh Token) ───────────────────────────
        meGroup.MapPost("/logout", async (HttpContext context, IAuthService authService) =>
        {
            var userId = GetUserId(context);
            if (userId is null) return Results.Unauthorized();

            await authService.RevokeTokenAsync(userId.Value);
            return Results.NoContent();
        })
        .WithName("Logout")
        .WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  USER MANAGEMENT (Admin only)
        // ═══════════════════════════════════════════════════════════════
        var usersGroup = app.MapGroup("/api/users")
            .WithTags("User Management")
            .RequireAuthorization("UsersManage")
            .WithAutoValidation();

        // ─── Get All Users for Property ──────────────────────────────
        usersGroup.MapGet("/by-property/{propertyId:guid}", async (
            Guid propertyId, int? page, int? pageSize, SAFARIstack.Infrastructure.Data.ApplicationDbContext db) =>
        {
            var query = db.ApplicationUsers
                .Where(u => u.PropertyId == propertyId)
                .OrderBy(u => u.LastName)
                .Select(u => new
                {
                    u.Id, u.Email, u.FirstName, u.LastName, u.FullName,
                    u.Phone, u.IsActive, u.IsLocked, u.LastLoginAt,
                    u.EmailConfirmed, u.PropertyId,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name)
                });
            return Results.Ok(await PaginationHelpers.PaginateAsync(query, page ?? 1, pageSize ?? 25));
        })
        .WithName("GetUsersByProperty")
        .WithOpenApi();

        // ─── Get User by ID ─────────────────────────────────────────
        usersGroup.MapGet("/{userId:guid}", async (
            Guid userId, IAuthService authService) =>
        {
            var user = await authService.GetUserByIdAsync(userId);
            if (user is null) return Results.NotFound();

            return Results.Ok(new
            {
                user.Id, user.Email, user.FirstName, user.LastName, user.FullName,
                user.Phone, user.AvatarUrl, user.IsActive, user.IsLocked,
                user.LastLoginAt, user.EmailConfirmed, user.PropertyId,
                Roles = user.UserRoles.Select(ur => ur.Role.Name)
            });
        })
        .WithName("GetUserById")
        .WithOpenApi();

        // ─── Create User (Admin registers employee) ──────────────────
        usersGroup.MapPost("/", async (RegisterRequest request, IAuthService authService) =>
        {
            var result = await authService.AdminRegisterAsync(request);
            return result.Success
                ? Results.Created($"/api/users/{result.UserId}", new { result.UserId, result.Email, result.FullName, result.Roles })
                : Results.BadRequest(result);
        })
        .WithName("CreateUser")
        .WithOpenApi();

        // ─── Assign Role ────────────────────────────────────────────
        usersGroup.MapPost("/{userId:guid}/roles", async (
            Guid userId, AssignRoleRequest request, HttpContext context, IAuthService authService) =>
        {
            var assignedBy = GetUserId(context);
            var success = await authService.AssignRoleAsync(userId, request.RoleName, assignedBy);
            return success
                ? Results.NoContent()
                : Results.BadRequest(new { Error = "User or role not found." });
        })
        .WithName("AssignRole")
        .WithOpenApi();

        // ─── Remove Role ────────────────────────────────────────────
        usersGroup.MapDelete("/{userId:guid}/roles/{roleName}", async (
            Guid userId, string roleName, IAuthService authService) =>
        {
            var success = await authService.RemoveRoleAsync(userId, roleName);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoveRole")
        .WithOpenApi();

        // ─── Deactivate User ────────────────────────────────────────
        usersGroup.MapPost("/{userId:guid}/deactivate", async (
            Guid userId, IAuthService authService) =>
        {
            var success = await authService.DeactivateUserAsync(userId);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeactivateUser")
        .WithOpenApi();

        // ─── Reactivate User ────────────────────────────────────────
        usersGroup.MapPost("/{userId:guid}/reactivate", async (
            Guid userId, IAuthService authService) =>
        {
            var success = await authService.ReactivateUserAsync(userId);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("ReactivateUser")
        .WithOpenApi();

        // ─── Unlock User ────────────────────────────────────────────
        usersGroup.MapPost("/{userId:guid}/unlock", async (
            Guid userId, IAuthService authService) =>
        {
            var success = await authService.UnlockUserAsync(userId);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UnlockUser")
        .WithOpenApi();

        // ─── Get User Roles ─────────────────────────────────────────
        usersGroup.MapGet("/{userId:guid}/roles", async (
            Guid userId, IAuthService authService) =>
        {
            var roles = await authService.GetUserRolesAsync(userId);
            return Results.Ok(roles);
        })
        .WithName("GetUserRoles")
        .WithOpenApi();

        // ─── Get User Permissions ────────────────────────────────────
        usersGroup.MapGet("/{userId:guid}/permissions", async (
            Guid userId, IAuthService authService) =>
        {
            var permissions = await authService.GetUserPermissionsAsync(userId);
            return Results.Ok(permissions);
        })
        .WithName("GetUserPermissions")
        .WithOpenApi();

        // ═══════════════════════════════════════════════════════════════
        //  ROLE & PERMISSION LOOKUP (for admin UI)
        // ═══════════════════════════════════════════════════════════════
        var rolesGroup = app.MapGroup("/api/roles")
            .WithTags("Roles & Permissions")
            .RequireAuthorization("RolesManage");

        rolesGroup.MapGet("/", async (SAFARIstack.Infrastructure.Data.ApplicationDbContext db) =>
        {
            var roles = await db.Roles
                .OrderBy(r => r.SortOrder)
                .Select(r => new
                {
                    r.Id, r.Name, r.Description, r.IsSystemRole, r.SortOrder,
                    UserCount = r.UserRoles.Count,
                    Permissions = r.RolePermissions.Select(rp => rp.Permission.Name)
                })
                .ToListAsync();

            return Results.Ok(roles);
        })
        .WithName("GetAllRoles")
        .WithOpenApi();

        rolesGroup.MapGet("/permissions", async (SAFARIstack.Infrastructure.Data.ApplicationDbContext db) =>
        {
            var permissions = await db.Permissions
                .OrderBy(p => p.Module).ThenBy(p => p.Name)
                .Select(p => new { p.Id, p.Name, p.Module, p.Description })
                .ToListAsync();

            return Results.Ok(permissions);
        })
        .WithName("GetAllPermissions")
        .WithOpenApi();
    }

    // ─── Helper ──────────────────────────────────────────────────────
    private static Guid? GetUserId(HttpContext context)
    {
        var sub = context.User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                  ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

// ─── Request DTOs ────────────────────────────────────────────────────
public record RefreshTokenRequest(string RefreshToken);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record UpdateUserProfileRequest(string FirstName, string LastName, string? Phone, string? AvatarUrl);
public record AssignRoleRequest(string RoleName);
