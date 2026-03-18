using FluentAssertions;
using SAFARIstack.Core.Domain.Entities;

namespace SAFARIstack.Tests.Unit.Domain;

public class ApplicationUserTests
{
    private static readonly Guid PropertyId = Guid.NewGuid();

    private static ApplicationUser CreateUser() =>
        ApplicationUser.Create(PropertyId, "test@example.com", "hashed_password", "John", "Doe", "+27123456789");

    [Fact]
    public void Create_ValidInput_SetsAllProperties()
    {
        var user = CreateUser();
        user.PropertyId.Should().Be(PropertyId);
        user.Email.Should().Be("test@example.com");
        user.NormalizedEmail.Should().Be("TEST@EXAMPLE.COM");
        user.PasswordHash.Should().Be("hashed_password");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Phone.Should().Be("+27123456789");
        user.FullName.Should().Be("John Doe");
        user.IsActive.Should().BeTrue();
        user.EmailConfirmed.Should().BeFalse();
        user.IsLocked.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        var user = ApplicationUser.Create(PropertyId, "  TEST@email.COM  ", "hash", "  Jane  ", "  Doe  ");
        user.Email.Should().Be("test@email.com");
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Doe");
    }

    [Fact]
    public void Create_EmptyEmail_Throws()
    {
        var act = () => ApplicationUser.Create(PropertyId, "", "hash", "First", "Last");
        act.Should().Throw<ArgumentException>().WithMessage("*Email*");
    }

    [Fact]
    public void Create_EmptyPasswordHash_Throws()
    {
        var act = () => ApplicationUser.Create(PropertyId, "email@test.com", "", "First", "Last");
        act.Should().Throw<ArgumentException>().WithMessage("*Password hash*");
    }

    [Fact]
    public void Create_RaisesUserCreatedEvent()
    {
        var user = CreateUser();
        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UserCreatedEvent>();
    }

    [Fact]
    public void RecordLogin_ResetsFailedAttempts()
    {
        var user = CreateUser();
        user.RecordFailedLogin();
        user.RecordFailedLogin();
        user.RecordLogin();

        user.FailedLoginAttempts.Should().Be(0);
        user.IsLocked.Should().BeFalse();
        user.LockoutEnd.Should().BeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RecordFailedLogin_Under5_IncreasesCounter()
    {
        var user = CreateUser();
        user.RecordFailedLogin();
        user.RecordFailedLogin();

        user.FailedLoginAttempts.Should().Be(2);
        user.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void RecordFailedLogin_5thAttempt_LocksAccount()
    {
        var user = CreateUser();
        for (int i = 0; i < 5; i++) user.RecordFailedLogin();

        user.FailedLoginAttempts.Should().Be(5);
        user.IsLocked.Should().BeTrue();
        user.LockoutEnd.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void RecordFailedLogin_5thAttempt_RaisesLockedOutEvent()
    {
        var user = CreateUser();
        user.ClearDomainEvents();
        for (int i = 0; i < 5; i++) user.RecordFailedLogin();

        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UserLockedOutEvent>();
    }

    [Fact]
    public void SetRefreshToken_SetsTokenAndExpiry()
    {
        var user = CreateUser();
        var expiry = DateTime.UtcNow.AddDays(7);
        user.SetRefreshToken("refresh-token-abc", expiry);

        user.RefreshToken.Should().Be("refresh-token-abc");
        user.RefreshTokenExpiresAt.Should().Be(expiry);
    }

    [Fact]
    public void RevokeRefreshToken_ClearsToken()
    {
        var user = CreateUser();
        user.SetRefreshToken("token", DateTime.UtcNow.AddDays(7));
        user.RevokeRefreshToken();

        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public void IsRefreshTokenValid_ValidToken_ReturnsTrue()
    {
        var user = CreateUser();
        user.SetRefreshToken("valid-token", DateTime.UtcNow.AddDays(7));
        user.IsRefreshTokenValid("valid-token").Should().BeTrue();
    }

    [Fact]
    public void IsRefreshTokenValid_WrongToken_ReturnsFalse()
    {
        var user = CreateUser();
        user.SetRefreshToken("valid-token", DateTime.UtcNow.AddDays(7));
        user.IsRefreshTokenValid("wrong-token").Should().BeFalse();
    }

    [Fact]
    public void IsRefreshTokenValid_ExpiredToken_ReturnsFalse()
    {
        var user = CreateUser();
        user.SetRefreshToken("token", DateTime.UtcNow.AddDays(-1));
        user.IsRefreshTokenValid("token").Should().BeFalse();
    }

    [Fact]
    public void UpdateProfile_ChangesNameAndPhone()
    {
        var user = CreateUser();
        user.UpdateProfile("Jane", "Smith", "+27987654321", "https://avatar.com/pic.jpg");

        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
        user.Phone.Should().Be("+27987654321");
        user.AvatarUrl.Should().Be("https://avatar.com/pic.jpg");
    }

    [Fact]
    public void ChangePassword_UpdatesHash()
    {
        var user = CreateUser();
        user.ClearDomainEvents();
        user.ChangePassword("new_hashed_password");

        user.PasswordHash.Should().Be("new_hashed_password");
        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UserPasswordChangedEvent>();
    }

    [Fact]
    public void Deactivate_SetsInactiveAndRevokesToken()
    {
        var user = CreateUser();
        user.SetRefreshToken("token", DateTime.UtcNow.AddDays(7));
        user.ClearDomainEvents();
        user.Deactivate();

        user.IsActive.Should().BeFalse();
        user.RefreshToken.Should().BeNull();
        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UserDeactivatedEvent>();
    }

    [Fact]
    public void Reactivate_SetsActiveAndResetsLockout()
    {
        var user = CreateUser();
        for (int i = 0; i < 5; i++) user.RecordFailedLogin();
        user.Deactivate();
        user.Reactivate();

        user.IsActive.Should().BeTrue();
        user.IsLocked.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public void Unlock_ResetsLockoutState()
    {
        var user = CreateUser();
        for (int i = 0; i < 5; i++) user.RecordFailedLogin();
        user.Unlock();

        user.IsLocked.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
    }

    [Fact]
    public void LinkStaffMember_SetsStaffId()
    {
        var user = CreateUser();
        var staffId = Guid.NewGuid();
        user.LinkStaffMember(staffId);
        user.StaffMemberId.Should().Be(staffId);
    }

    [Fact]
    public void ConfirmEmail_SetsFlag()
    {
        var user = CreateUser();
        user.ConfirmEmail();
        user.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public void AddRole_IncreasesUserRoles()
    {
        var user = CreateUser();
        var role = UserRole.Create(user.Id, Guid.NewGuid());
        user.AddRole(role);
        user.UserRoles.Should().HaveCount(1);
    }
}

public class RoleTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var role = Role.Create("Manager", "Property manager", true, 2);
        role.Name.Should().Be("Manager");
        role.NormalizedName.Should().Be("MANAGER");
        role.Description.Should().Be("Property manager");
        role.IsSystemRole.Should().BeTrue();
        role.SortOrder.Should().Be(2);
    }

    [Fact]
    public void Update_SystemRole_Throws()
    {
        var role = Role.Create("SuperAdmin", "System admin", true);
        var act = () => role.Update("NewName", "New desc");
        act.Should().Throw<InvalidOperationException>().WithMessage("*system roles*");
    }

    [Fact]
    public void Update_CustomRole_Succeeds()
    {
        var role = Role.Create("Custom", "Custom role", false);
        role.Update("Updated", "Updated desc");

        role.Name.Should().Be("Updated");
        role.NormalizedName.Should().Be("UPDATED");
        role.Description.Should().Be("Updated desc");
    }

    [Fact]
    public void AddPermission_IncreasesPermissions()
    {
        var role = Role.Create("Test", "Test role");
        var perm = RolePermission.Create(role.Id, Guid.NewGuid());
        role.AddPermission(perm);
        role.RolePermissions.Should().HaveCount(1);
    }
}

public class PermissionTests
{
    [Fact]
    public void Create_SetsAllFields()
    {
        var perm = Permission.Create("bookings.create", "Bookings", "Can create bookings");
        perm.Name.Should().Be("bookings.create");
        perm.Module.Should().Be("Bookings");
        perm.Description.Should().Be("Can create bookings");
    }
}

public class PermissionsConstantsTests
{
    [Fact]
    public void GetAll_ReturnsAllPermissions()
    {
        var perms = Permissions.GetAll().ToList();
        perms.Should().NotBeEmpty();
        perms.Count.Should().BeGreaterThanOrEqualTo(20);
    }

    [Fact]
    public void GetAll_AllHaveModuleSeparator()
    {
        foreach (var (name, module) in Permissions.GetAll())
        {
            name.Should().Contain(".");
            module.Should().NotBeNullOrWhiteSpace();
        }
    }
}

public class UserRoleTests
{
    [Fact]
    public void Create_SetsIds()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();

        var ur = UserRole.Create(userId, roleId, assignedBy);
        ur.UserId.Should().Be(userId);
        ur.RoleId.Should().Be(roleId);
        ur.AssignedByUserId.Should().Be(assignedBy);
    }
}

public class RolePermissionTests
{
    [Fact]
    public void Create_SetsIds()
    {
        var roleId = Guid.NewGuid();
        var permId = Guid.NewGuid();
        var rp = RolePermission.Create(roleId, permId);
        rp.RoleId.Should().Be(roleId);
        rp.PermissionId.Should().Be(permId);
    }
}
