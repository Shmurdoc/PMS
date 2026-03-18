using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Core.Domain.Interfaces;

/// <summary>
/// Authentication service contract — JWT token generation, password handling, user management.
/// </summary>
public interface IAuthService
{
    // ─── Authentication ──────────────────────────────────────────
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> AdminRegisterAsync(RegisterRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(Guid userId);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

    // ─── User Management ─────────────────────────────────────────
    Task<ApplicationUser?> GetUserByIdAsync(Guid userId);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
    Task<IEnumerable<ApplicationUser>> GetUsersByPropertyAsync(Guid propertyId);
    Task<bool> DeactivateUserAsync(Guid userId);
    Task<bool> ReactivateUserAsync(Guid userId);
    Task<bool> UnlockUserAsync(Guid userId);
    Task<bool> UpdateProfileAsync(Guid userId, string firstName, string lastName, string? phone, string? avatarUrl);

    // ─── Role Management ─────────────────────────────────────────
    Task<bool> AssignRoleAsync(Guid userId, string roleName, Guid? assignedByUserId = null);
    Task<bool> RemoveRoleAsync(Guid userId, string roleName);
    Task<IEnumerable<string>> GetUserRolesAsync(Guid userId);
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId);
}

// ─── Request / Result DTOs ───────────────────────────────────────────

public record RegisterRequest(
    Guid PropertyId,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone = null,
    string? RoleName = null);

public record LoginRequest(
    string Email,
    string Password);

public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public IEnumerable<string> Roles { get; set; } = [];
    public IEnumerable<string> Permissions { get; set; } = [];
    public string? Error { get; set; }

    public static AuthResult Failure(string error) => new() { Success = false, Error = error };
}
