using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Authentication;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.Infrastructure;

/// <summary>
/// Authentication service — handles registration, login, JWT generation with role claims,
/// refresh tokens, user management, and role assignment.
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext db,
        JwtSettings jwtSettings,
        ILogger<AuthService> logger)
    {
        _db = db;
        _jwtSettings = jwtSettings;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════
    //  REGISTRATION
    // ═══════════════════════════════════════════════════════════════

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        // Validate password policy
        var passwordError = ValidatePassword(request.Password);
        if (passwordError is not null)
            return AuthResult.Failure(passwordError);

        // Check if email already exists
        var existingUser = await _db.ApplicationUsers
            .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.Trim().ToUpperInvariant());

        if (existingUser is not null)
            return AuthResult.Failure("A user with this email already exists.");

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, BCrypt.Net.BCrypt.GenerateSalt(12));

        // Resolve PropertyId — use provided or default to first property
        var propertyId = request.PropertyId;
        if (propertyId == Guid.Empty)
        {
            var defaultProperty = await _db.Properties.FirstOrDefaultAsync();
            if (defaultProperty is null)
                return AuthResult.Failure("No property configured. Please contact an administrator.");
            propertyId = defaultProperty.Id;
        }
        else
        {
            var propertyExists = await _db.Properties.AnyAsync(p => p.Id == propertyId);
            if (!propertyExists)
                return AuthResult.Failure("Invalid property ID.");
        }

        // Create user
        var user = ApplicationUser.Create(
            propertyId,
            request.Email,
            passwordHash,
            request.FirstName,
            request.LastName,
            request.Phone);

        user.ConfirmEmail(); // Auto-confirm for admin-created users

        await _db.ApplicationUsers.AddAsync(user);

        // Restrict role assignment — never allow SuperAdmin via registration
        var requestedRole = request.RoleName ?? SystemRoles.FrontDesk;
        var allowedSelfRegisterRoles = new[] { SystemRoles.FrontDesk, SystemRoles.Receptionist };

        // If a protected role is requested, default to FrontDesk
        var roleName = allowedSelfRegisterRoles.Contains(requestedRole, StringComparer.OrdinalIgnoreCase)
            ? requestedRole
            : SystemRoles.FrontDesk;

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpperInvariant());

        if (role is not null)
        {
            var userRole = UserRole.Create(user.Id, role.Id);
            await _db.UserRoles.AddAsync(userRole);
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("User registered: {Email} with role {Role} for property {PropertyId}",
            user.Email, roleName, propertyId);

        // Generate tokens
        var roles = new List<string> { roleName };
        var permissions = role is not null
            ? await GetPermissionsForRoleAsync(role.Id)
            : Enumerable.Empty<string>();

        var token = GenerateJwtToken(user, roles, permissions);
        var refreshToken = GenerateRefreshToken();
        var hashedRefreshToken = HashRefreshToken(refreshToken);
        user.SetRefreshToken(hashedRefreshToken, DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));
        await _db.SaveChangesAsync();

        return new AuthResult
        {
            Success = true,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles,
            Permissions = permissions
        };
    }

    /// <summary>
    /// Admin-only registration — allows assigning any role including PropertyAdmin.
    /// SuperAdmin cannot be assigned via API — only via DB seeding.
    /// </summary>
    public async Task<AuthResult> AdminRegisterAsync(RegisterRequest request)
    {
        var passwordError = ValidatePassword(request.Password);
        if (passwordError is not null)
            return AuthResult.Failure(passwordError);

        var existingUser = await _db.ApplicationUsers
            .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.Trim().ToUpperInvariant());

        if (existingUser is not null)
            return AuthResult.Failure("A user with this email already exists.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, BCrypt.Net.BCrypt.GenerateSalt(12));

        var user = ApplicationUser.Create(
            request.PropertyId, request.Email, passwordHash,
            request.FirstName, request.LastName, request.Phone);
        user.ConfirmEmail();
        await _db.ApplicationUsers.AddAsync(user);

        // Admin can assign any role except SuperAdmin
        var roleName = request.RoleName ?? SystemRoles.FrontDesk;
        if (string.Equals(roleName, SystemRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            roleName = SystemRoles.PropertyAdmin; // Downgrade SuperAdmin attempts

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpperInvariant());
        if (role is not null)
        {
            var userRole = UserRole.Create(user.Id, role.Id);
            await _db.UserRoles.AddAsync(userRole);
        }

        await _db.SaveChangesAsync();

        var roles = new List<string> { roleName };
        var permissions = role is not null
            ? await GetPermissionsForRoleAsync(role.Id)
            : Enumerable.Empty<string>();

        _logger.LogInformation("Admin created user: {Email} with role {Role} for property {PropertyId}",
            user.Email, roleName, request.PropertyId);

        return new AuthResult
        {
            Success = true,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles,
            Permissions = permissions
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  LOGIN
    // ═══════════════════════════════════════════════════════════════

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _db.ApplicationUsers
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.Trim().ToUpperInvariant());

        if (user is null)
            return AuthResult.Failure("Invalid email or password.");

        if (!user.IsActive)
            return AuthResult.Failure("This account has been deactivated. Contact your administrator.");

        if (user.IsLocked && user.LockoutEnd > DateTime.UtcNow)
            return AuthResult.Failure($"Account is locked. Try again after {user.LockoutEnd:HH:mm}.");

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            await _db.SaveChangesAsync();
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return AuthResult.Failure("Invalid email or password.");
        }

        // Successful login
        user.RecordLogin();

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        var token = GenerateJwtToken(user, roles, permissions);
        var refreshToken = GenerateRefreshToken();
        var hashedRefreshToken = HashRefreshToken(refreshToken);
        user.SetRefreshToken(hashedRefreshToken, DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));

        await _db.SaveChangesAsync();

        _logger.LogInformation("User logged in: {Email} with roles [{Roles}]",
            user.Email, string.Join(", ", roles));

        return new AuthResult
        {
            Success = true,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles,
            Permissions = permissions
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  REFRESH TOKEN
    // ═══════════════════════════════════════════════════════════════

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        var hashedToken = HashRefreshToken(refreshToken);

        var user = await _db.ApplicationUsers
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.RefreshToken == hashedToken);

        if (user is null || !user.IsRefreshTokenValid(hashedToken))
            return AuthResult.Failure("Invalid or expired refresh token.");

        if (!user.IsActive)
            return AuthResult.Failure("Account is deactivated.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        var newToken = GenerateJwtToken(user, roles, permissions);
        var newRefreshToken = GenerateRefreshToken();
        var hashedNewRefreshToken = HashRefreshToken(newRefreshToken);
        user.SetRefreshToken(hashedNewRefreshToken, DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));

        await _db.SaveChangesAsync();

        return new AuthResult
        {
            Success = true,
            Token = newToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles,
            Permissions = permissions
        };
    }

    public async Task RevokeTokenAsync(Guid userId)
    {
        var user = await _db.ApplicationUsers.FindAsync(userId);
        if (user is null) return;

        user.RevokeRefreshToken();
        await _db.SaveChangesAsync();
        _logger.LogInformation("Refresh token revoked for user {UserId}", userId);
    }

    // ═══════════════════════════════════════════════════════════════
    //  PASSWORD
    // ═══════════════════════════════════════════════════════════════

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var passwordError = ValidatePassword(newPassword);
        if (passwordError is not null) return false;

        var user = await _db.ApplicationUsers.FindAsync(userId);
        if (user is null) return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return false;

        var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
        user.ChangePassword(newHash);
        user.RevokeRefreshToken();
        await _db.SaveChangesAsync();

        _logger.LogInformation("Password changed for user {Email}", user.Email);
        return true;
    }

    // ═══════════════════════════════════════════════════════════════
    //  USER MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApplicationUser?> GetUserByIdAsync(Guid userId)
    {
        return await _db.ApplicationUsers
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        return await _db.ApplicationUsers
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == email.Trim().ToUpperInvariant());
    }

    public async Task<IEnumerable<ApplicationUser>> GetUsersByPropertyAsync(Guid propertyId)
    {
        return await _db.ApplicationUsers
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.PropertyId == propertyId)
            .OrderBy(u => u.LastName)
            .ToListAsync();
    }

    public async Task<bool> DeactivateUserAsync(Guid userId)
    {
        var user = await _db.ApplicationUsers.FindAsync(userId);
        if (user is null) return false;
        user.Deactivate();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReactivateUserAsync(Guid userId)
    {
        var user = await _db.ApplicationUsers.FindAsync(userId);
        if (user is null) return false;
        user.Reactivate();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnlockUserAsync(Guid userId)
    {
        var user = await _db.ApplicationUsers.FindAsync(userId);
        if (user is null) return false;
        user.Unlock();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateProfileAsync(Guid userId, string firstName, string lastName, string? phone, string? avatarUrl)
    {
        var user = await _db.ApplicationUsers.FindAsync(userId);
        if (user is null) return false;
        user.UpdateProfile(firstName, lastName, phone, avatarUrl);
        await _db.SaveChangesAsync();
        return true;
    }

    // ═══════════════════════════════════════════════════════════════
    //  ROLE MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    public async Task<bool> AssignRoleAsync(Guid userId, string roleName, Guid? assignedByUserId = null)
    {
        var user = await _db.ApplicationUsers.FindAsync(userId);
        if (user is null) return false;

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpperInvariant());
        if (role is null) return false;

        // Check if already assigned
        var existing = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);
        if (existing) return true;

        var userRole = UserRole.Create(userId, role.Id, assignedByUserId);
        await _db.UserRoles.AddAsync(userRole);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Role {Role} assigned to user {UserId} by {AssignedBy}",
            roleName, userId, assignedByUserId);
        return true;
    }

    public async Task<bool> RemoveRoleAsync(Guid userId, string roleName)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpperInvariant());
        if (role is null) return false;

        var userRole = await _db.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);
        if (userRole is null) return false;

        _db.UserRoles.Remove(userRole);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Role {Role} removed from user {UserId}", roleName, userId);
        return true;
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(Guid userId)
    {
        return await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId)
    {
        return await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ═══════════════════════════════════════════════════════════════

    private string GenerateJwtToken(ApplicationUser user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("name", user.FullName),
            new("propertyId", user.PropertyId.ToString()),
        };

        // Add role claims
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        // Add permission claims
        foreach (var permission in permissions)
            claims.Add(new Claim("permission", permission));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private async Task<IEnumerable<string>> GetPermissionsForRoleAsync(Guid roleId)
    {
        return await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Validates password meets minimum complexity requirements.
    /// Returns null if valid, or an error message if invalid.
    /// </summary>
    private static string? ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "Password is required.";
        if (password.Length < 8)
            return "Password must be at least 8 characters long.";
        if (!password.Any(char.IsUpper))
            return "Password must contain at least one uppercase letter.";
        if (!password.Any(char.IsLower))
            return "Password must contain at least one lowercase letter.";
        if (!password.Any(char.IsDigit))
            return "Password must contain at least one digit.";
        if (password.All(char.IsLetterOrDigit))
            return "Password must contain at least one special character.";
        return null;
    }

    /// <summary>
    /// Hashes a refresh token using SHA256 before storing in the database.
    /// This prevents token theft if the DB is compromised.
    /// </summary>
    private static string HashRefreshToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
