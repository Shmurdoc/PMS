using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Infrastructure.Data.Configurations;

// ═══════════════════════════════════════════════════════════════════════════
//  APPLICATION USER CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════════

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("application_users");

        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.NormalizedEmail).IsUnique();
        builder.HasIndex(u => u.PropertyId);
        builder.HasIndex(u => u.RefreshToken);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.RefreshToken).HasMaxLength(512);

        builder.HasOne(u => u.Property)
            .WithMany()
            .HasForeignKey(u => u.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft-delete support
        builder.Property<bool>("IsDeleted").HasDefaultValue(false);
        builder.Property<DateTime?>("DeletedAt");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  ROLE CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════════

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.NormalizedName).IsUnique();

        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.NormalizedName).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);

        builder.HasMany(r => r.RolePermissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // ─── Seed default system roles ──────────────────────────────
        builder.HasData(DefaultRoles.GetAll());
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  PERMISSION CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════════

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.Name).IsUnique();

        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Module).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);

        // ─── Seed all permissions ────────────────────────────────────
        builder.HasData(DefaultPermissions.GetAll());
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  USER ROLE CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════════

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(ur => ur.Id);
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  ROLE PERMISSION CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════════

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(rp => rp.Id);
        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ─── Seed role-permission mappings ───────────────────────────
        builder.HasData(DefaultRolePermissions.GetAll());
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  SEED DATA — Deterministic GUIDs for reproducible migrations
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Helper to generate deterministic GUIDs from names (for seeding consistency).
/// </summary>
internal static class SeedGuid
{
    private static readonly Guid Namespace = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8"); // RFC 4122 DNS namespace

    public static Guid From(string name)
    {
        // Simple deterministic GUID from name using hash
        var bytes = System.Text.Encoding.UTF8.GetBytes($"SAFARIstack-{name}");
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x40); // Version 4
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80); // Variant 1
        return new Guid(guidBytes);
    }
}

internal static class DefaultRoles
{
    public static Role[] GetAll() => new[]
    {
        CreateRole(SystemRoles.SuperAdmin, "Full system access — platform owner/operator", 0),
        CreateRole(SystemRoles.PropertyAdmin, "Full property access — establishment owner/GM", 1),
        CreateRole(SystemRoles.Manager, "Operations management — duty manager", 2),
        CreateRole(SystemRoles.FrontDesk, "Front desk operations — bookings, check-in/out, guests", 3),
        CreateRole(SystemRoles.Housekeeping, "Housekeeping operations — task management, inspections", 4),
        CreateRole(SystemRoles.Finance, "Financial operations — folios, invoices, payments, reports", 5),
        CreateRole(SystemRoles.Maintenance, "Maintenance tasks — room issues, repairs", 6),
    };

    private static Role CreateRole(string name, string desc, int sortOrder)
    {
        // Use reflection to set properties for seeding (EF requires this for HasData)
        var role = (Role)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Role));
        typeof(Role).BaseType!.BaseType!.GetProperty("Id")!.SetValue(role, SeedGuid.From($"Role-{name}"));
        typeof(Role).BaseType!.BaseType!.GetProperty("CreatedAt")!.SetValue(role, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        typeof(Role).BaseType!.BaseType!.GetProperty("UpdatedAt")!.SetValue(role, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        typeof(Role).GetProperty("Name")!.SetValue(role, name);
        typeof(Role).GetProperty("NormalizedName")!.SetValue(role, name.ToUpperInvariant());
        typeof(Role).GetProperty("Description")!.SetValue(role, desc);
        typeof(Role).GetProperty("IsSystemRole")!.SetValue(role, true);
        typeof(Role).GetProperty("SortOrder")!.SetValue(role, sortOrder);
        return role;
    }
}

internal static class DefaultPermissions
{
    public static Permission[] GetAll()
    {
        return Permissions.GetAll()
            .Select(p =>
            {
                var perm = (Permission)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Permission));
                typeof(Permission).BaseType!.GetProperty("Id")!.SetValue(perm, SeedGuid.From($"Permission-{p.Name}"));
                typeof(Permission).BaseType!.GetProperty("CreatedAt")!.SetValue(perm, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                typeof(Permission).BaseType!.GetProperty("UpdatedAt")!.SetValue(perm, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                typeof(Permission).GetProperty("Name")!.SetValue(perm, p.Name);
                typeof(Permission).GetProperty("Module")!.SetValue(perm, p.Module);
                typeof(Permission).GetProperty("Description")!.SetValue(perm, $"Permission to {p.Name.Replace(".", " ")}");
                return perm;
            })
            .ToArray();
    }
}

internal static class DefaultRolePermissions
{
    public static RolePermission[] GetAll()
    {
        var mappings = new List<RolePermission>();

        // SuperAdmin → ALL permissions
        foreach (var p in Permissions.GetAll())
            mappings.Add(CreateMapping(SystemRoles.SuperAdmin, p.Name));

        // PropertyAdmin → ALL permissions
        foreach (var p in Permissions.GetAll())
            mappings.Add(CreateMapping(SystemRoles.PropertyAdmin, p.Name));

        // Manager → Most permissions except admin
        var managerPerms = Permissions.GetAll()
            .Where(p => p.Name != Permissions.RolesManage && p.Name != Permissions.PropertySettings)
            .ToList();
        foreach (var p in managerPerms)
            mappings.Add(CreateMapping(SystemRoles.Manager, p.Name));

        // FrontDesk → Bookings, Guests, Rooms (view), Financial (view)
        var frontDeskPerms = new[]
        {
            Permissions.BookingsView, Permissions.BookingsCreate, Permissions.BookingsEdit,
            Permissions.BookingsCheckIn, Permissions.BookingsCheckOut,
            Permissions.GuestsView, Permissions.GuestsCreate, Permissions.GuestsEdit,
            Permissions.RoomsView, Permissions.FinancialView, Permissions.RatesView
        };
        foreach (var p in frontDeskPerms)
            mappings.Add(CreateMapping(SystemRoles.FrontDesk, p));

        // Housekeeping → Housekeeping + Rooms (view)
        var hkPerms = new[]
        {
            Permissions.HousekeepingView, Permissions.HousekeepingManage, Permissions.HousekeepingInspect,
            Permissions.RoomsView
        };
        foreach (var p in hkPerms)
            mappings.Add(CreateMapping(SystemRoles.Housekeeping, p));

        // Finance → Financial, Invoices, Rates, Reports
        var financePerms = new[]
        {
            Permissions.FinancialView, Permissions.FinancialManage, Permissions.InvoicesManage,
            Permissions.RatesView, Permissions.RatesManage, Permissions.ReportsView, Permissions.AnalyticsView
        };
        foreach (var p in financePerms)
            mappings.Add(CreateMapping(SystemRoles.Finance, p));

        // Maintenance → Housekeeping (view), Rooms (view)
        var maintenancePerms = new[]
        {
            Permissions.HousekeepingView, Permissions.RoomsView
        };
        foreach (var p in maintenancePerms)
            mappings.Add(CreateMapping(SystemRoles.Maintenance, p));

        return mappings.ToArray();
    }

    private static RolePermission CreateMapping(string roleName, string permissionName)
    {
        var rp = (RolePermission)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(RolePermission));
        typeof(RolePermission).BaseType!.GetProperty("Id")!.SetValue(rp, SeedGuid.From($"RP-{roleName}-{permissionName}"));
        typeof(RolePermission).BaseType!.GetProperty("CreatedAt")!.SetValue(rp, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        typeof(RolePermission).BaseType!.GetProperty("UpdatedAt")!.SetValue(rp, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        typeof(RolePermission).GetProperty("RoleId")!.SetValue(rp, SeedGuid.From($"Role-{roleName}"));
        typeof(RolePermission).GetProperty("PermissionId")!.SetValue(rp, SeedGuid.From($"Permission-{permissionName}"));
        return rp;
    }
}
