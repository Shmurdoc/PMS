using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Core.Domain.Entities;

// ═══════════════════════════════════════════════════════════════════════════
//  APPLICATION USER — System login user (staff member with auth credentials)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Represents a user who can log in to SAFARIstack PMS.
/// Linked to a StaffMember but separated for security (auth concerns ≠ HR concerns).
/// </summary>
public class ApplicationUser : AuditableAggregateRoot, IMultiTenant
{
    public Guid PropertyId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? AvatarUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool EmailConfirmed { get; private set; }
    public bool IsLocked { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }

    // Optional link to StaffMember (for HR/RFID features)
    public Guid? StaffMemberId { get; private set; }

    // Navigation
    public Property Property { get; private set; } = null!;
    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public string FullName => $"{FirstName} {LastName}";

    private ApplicationUser() { } // EF Core

    public static ApplicationUser Create(
        Guid propertyId,
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        var user = new ApplicationUser
        {
            PropertyId = propertyId,
            Email = email.Trim().ToLowerInvariant(),
            NormalizedEmail = email.Trim().ToUpperInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Phone = phone,
            IsActive = true,
            EmailConfirmed = false
        };

        user.AddDomainEvent(new UserCreatedEvent(user.Id, user.Email, user.FullName, propertyId));
        return user;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        IsLocked = false;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
        {
            IsLocked = true;
            LockoutEnd = DateTime.UtcNow.AddMinutes(30);
            AddDomainEvent(new UserLockedOutEvent(Id, Email));
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRefreshToken(string token, DateTime expiresAt)
    {
        RefreshToken = token;
        RefreshTokenExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsRefreshTokenValid(string token)
    {
        return RefreshToken == token
            && RefreshTokenExpiresAt.HasValue
            && RefreshTokenExpiresAt.Value > DateTime.UtcNow;
    }

    public void UpdateProfile(string firstName, string lastName, string? phone, string? avatarUrl)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone;
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new UserPasswordChangedEvent(Id, Email));
    }

    public void Deactivate()
    {
        IsActive = false;
        RevokeRefreshToken();
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new UserDeactivatedEvent(Id, Email));
    }

    public void Reactivate()
    {
        IsActive = true;
        FailedLoginAttempts = 0;
        IsLocked = false;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unlock()
    {
        IsLocked = false;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void LinkStaffMember(Guid staffMemberId)
    {
        StaffMemberId = staffMemberId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddRole(UserRole userRole)
    {
        _userRoles.Add(userRole);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  ROLE — Predefined system roles for establishment management
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// System role that defines a set of permissions.
/// Roles are seeded at startup (SuperAdmin, PropertyAdmin, Manager, etc.).
/// </summary>
public class Role : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; } // Cannot be deleted
    public int SortOrder { get; private set; }

    // Navigation
    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();
    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private Role() { } // EF Core

    public static Role Create(string name, string? description, bool isSystemRole = true, int sortOrder = 0)
    {
        return new Role
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Description = description,
            IsSystemRole = isSystemRole,
            SortOrder = sortOrder
        };
    }

    public void Update(string name, string? description)
    {
        if (IsSystemRole)
            throw new InvalidOperationException("Cannot modify system roles.");

        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddPermission(RolePermission permission)
    {
        _rolePermissions.Add(permission);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  PERMISSION — Granular access rights
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Represents a granular permission (e.g., "bookings.create", "housekeeping.view").
/// Permissions are grouped by module for easy management.
/// </summary>
public class Permission : Entity
{
    public string Name { get; private set; } = string.Empty;        // "bookings.create"
    public string Module { get; private set; } = string.Empty;      // "Bookings"
    public string? Description { get; private set; }

    // Navigation
    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Permission() { } // EF Core

    public static Permission Create(string name, string module, string? description = null)
    {
        return new Permission
        {
            Name = name,
            Module = module,
            Description = description
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  USER-ROLE — Many-to-Many join entity
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Join entity: which users have which roles.
/// Includes who assigned the role and when.
/// </summary>
public class UserRole : Entity
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid? AssignedByUserId { get; private set; }

    // Navigation
    public ApplicationUser User { get; private set; } = null!;
    public Role Role { get; private set; } = null!;

    private UserRole() { } // EF Core

    public static UserRole Create(Guid userId, Guid roleId, Guid? assignedByUserId = null)
    {
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedByUserId = assignedByUserId
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  ROLE-PERMISSION — Many-to-Many join entity
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Join entity: which roles have which permissions.
/// </summary>
public class RolePermission : Entity
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    // Navigation
    public Role Role { get; private set; } = null!;
    public Permission Permission { get; private set; } = null!;

    private RolePermission() { } // EF Core

    public static RolePermission Create(Guid roleId, Guid permissionId)
    {
        return new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  DOMAIN EVENTS
// ═══════════════════════════════════════════════════════════════════════════

public record UserCreatedEvent(Guid UserId, string Email, string FullName, Guid PropertyId) : DomainEvent;
public record UserLockedOutEvent(Guid UserId, string Email) : DomainEvent;
public record UserPasswordChangedEvent(Guid UserId, string Email) : DomainEvent;
public record UserDeactivatedEvent(Guid UserId, string Email) : DomainEvent;

// ═══════════════════════════════════════════════════════════════════════════
//  PERMISSION CONSTANTS — Used for policy-based authorization
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Centralized permission name constants used in authorization policies.
/// Naming convention: "{module}.{action}" — all lowercase.
/// </summary>
public static class Permissions
{
    // ─── Bookings ────────────────────────────────────────────────
    public const string BookingsView = "bookings.view";
    public const string BookingsCreate = "bookings.create";
    public const string BookingsEdit = "bookings.edit";
    public const string BookingsCancel = "bookings.cancel";
    public const string BookingsCheckIn = "bookings.checkin";
    public const string BookingsCheckOut = "bookings.checkout";

    // ─── Guests ──────────────────────────────────────────────────
    public const string GuestsView = "guests.view";
    public const string GuestsCreate = "guests.create";
    public const string GuestsEdit = "guests.edit";
    public const string GuestsBlacklist = "guests.blacklist";

    // ─── Rooms ───────────────────────────────────────────────────
    public const string RoomsView = "rooms.view";
    public const string RoomsManage = "rooms.manage";

    // ─── Housekeeping ────────────────────────────────────────────
    public const string HousekeepingView = "housekeeping.view";
    public const string HousekeepingManage = "housekeeping.manage";
    public const string HousekeepingInspect = "housekeeping.inspect";

    // ─── Financial ───────────────────────────────────────────────
    public const string FinancialView = "financial.view";
    public const string FinancialManage = "financial.manage";
    public const string InvoicesManage = "invoices.manage";

    // ─── Rates & Pricing ─────────────────────────────────────────
    public const string RatesView = "rates.view";
    public const string RatesManage = "rates.manage";

    // ─── Staff ───────────────────────────────────────────────────
    public const string StaffView = "staff.view";
    public const string StaffManage = "staff.manage";

    // ─── RFID ────────────────────────────────────────────────────
    public const string RfidManage = "rfid.manage";

    // ─── Reports & Analytics ─────────────────────────────────────
    public const string ReportsView = "reports.view";
    public const string AnalyticsView = "analytics.view";

    // ─── Administration ──────────────────────────────────────────
    public const string UsersManage = "users.manage";
    public const string RolesManage = "roles.manage";
    public const string PropertySettings = "property.settings";
    public const string AuditLogsView = "auditlogs.view";

    /// <summary>
    /// Returns all permission constants via reflection for seeding.
    /// </summary>
    public static IEnumerable<(string Name, string Module)> GetAll()
    {
        var fields = typeof(Permissions).GetFields(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);

        foreach (var field in fields)
        {
            if (field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            {
                var value = (string)field.GetRawConstantValue()!;
                var module = value.Split('.')[0];
                var moduleName = char.ToUpper(module[0]) + module[1..];
                yield return (value, moduleName);
            }
        }
    }
}

/// <summary>
/// Predefined role names.
/// </summary>
public static class SystemRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string PropertyAdmin = "PropertyAdmin";
    public const string Manager = "Manager";
    public const string FrontDesk = "FrontDesk";
    public const string Housekeeping = "Housekeeping";
    public const string Finance = "Finance";
    public const string Maintenance = "Maintenance";
    public const string Receptionist = "Receptionist";
}
