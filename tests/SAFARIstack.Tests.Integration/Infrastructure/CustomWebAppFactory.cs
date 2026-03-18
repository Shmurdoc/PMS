using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Modules.Staff.Domain.Entities;

namespace SAFARIstack.Tests.Integration.Infrastructure;

/// <summary>
/// WebApplicationFactory that replaces the real PostgreSQL database with a unique per-run
/// test database and wires up a fake JWT authentication scheme for endpoint testing.
/// </summary>
public class CustomWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbName = $"safaristack_e2e_{Guid.NewGuid():N}";
    private string _connectionString = null!;

    // Seeded reference IDs for use in tests
    public Guid PropertyAId { get; private set; }
    public Guid PropertyBId { get; private set; }
    public Guid GuestAId { get; private set; }
    public Guid RoomTypeAId { get; private set; }
    public Guid RoomAId { get; private set; }
    public Guid StaffAId { get; private set; }
    public Guid UserAId { get; private set; }
    public string UserAEmail { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // ─── Override config BEFORE Program.cs fail-fast validation ──
        _connectionString = $"Host=localhost;Port=5432;Database={_dbName};Username=postgres;Password=Morven-05;Include Error Detail=true;Trust Server Certificate=true";

        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("JwtSettings:SecretKey", "integration-test-secret-key-minimum-32-characters-long");

        builder.ConfigureTestServices(services =>
        {
            // ─── Replace DbContext connection string with test DB ────
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseNpgsql(_connectionString);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
                options.ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });

            // ─── Add fake JWT auth scheme ───────────────────────────
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = "TestScheme";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });

            // ─── Disable rate limiting for tests ────────────────────────
            services.RemoveAll<IIpPolicyStore>();
            services.RemoveAll<IRateLimitCounterStore>();
            services.RemoveAll<IRateLimitConfiguration>();
            services.RemoveAll<IProcessingStrategy>();
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
            services.Configure<IpRateLimitOptions>(opt =>
            {
                opt.EnableEndpointRateLimiting = false;
                opt.GeneralRules = new List<RateLimitRule>();
            });
        });
    }

    public async Task InitializeAsync()
    {
        // Create and migrate the test database
        _connectionString = $"Host=localhost;Port=5432;Database={_dbName};Username=postgres;Password=Morven-05;Include Error Detail=true;Trust Server Certificate=true";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(_connectionString);

        var tenantProvider = new SuperAdminTenantProvider();
        await using var ctx = new ApplicationDbContext(optionsBuilder.Options, tenantProvider);
        await ctx.Database.EnsureCreatedAsync();

        // Seed minimal reference data for endpoint tests
        await SeedReferenceData(ctx);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        // Drop the test database
        try
        {
            var masterConn = $"Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=Morven-05;Trust Server Certificate=true";
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(masterConn);
            await using var masterCtx = new ApplicationDbContext(optionsBuilder.Options);
            await masterCtx.Database.ExecuteSqlRawAsync(
                $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{_dbName}'");
            await masterCtx.Database.ExecuteSqlRawAsync($"DROP DATABASE IF EXISTS \"{_dbName}\"");
        }
        catch { /* best effort */ }
    }

    private async Task SeedReferenceData(ApplicationDbContext ctx)
    {
        // Properties
        var propA = Property.Create("Safari Lodge Alpha", "safari-lodge-alpha", "123 Main St", "Cape Town", "Western Cape");
        var propB = Property.Create("Safari Lodge Beta", "safari-lodge-beta", "456 Oak Ave", "Durban", "KwaZulu-Natal");
        ctx.Properties.AddRange(propA, propB);
        await ctx.SaveChangesAsync();
        PropertyAId = propA.Id;
        PropertyBId = propB.Id;

        // Room types
        var roomType = RoomType.Create(propA.Id, "Deluxe Suite", "DLX", 2500m, 4, 2, 2);
        ctx.RoomTypes.Add(roomType);
        await ctx.SaveChangesAsync();
        RoomTypeAId = roomType.Id;

        // Rooms
        var room = Room.Create(propA.Id, roomType.Id, "101", 1, "A");
        var room2 = Room.Create(propA.Id, roomType.Id, "102", 1, "A");
        ctx.Rooms.AddRange(room, room2);
        await ctx.SaveChangesAsync();
        RoomAId = room.Id;

        // Guest
        var guest = Guest.Create(propA.Id, "John", "TestGuest", "john@test.com", "+27821234567");
        ctx.Guests.Add(guest);
        await ctx.SaveChangesAsync();
        GuestAId = guest.Id;

        // Staff
        var staff = StaffMember.Create(propA.Id, "staff@safari.com", "Jane", "Cleaner", StaffRole.Housekeeping);
        ctx.StaffMembers.Add(staff);
        await ctx.SaveChangesAsync();
        StaffAId = staff.Id;

        // Application user (for auth flow tests)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Test@12345");
        var user = ApplicationUser.Create(propA.Id, "admin@safari.com", passwordHash, "Admin", "User", "+27820000000");
        ctx.ApplicationUsers.Add(user);
        await ctx.SaveChangesAsync();
        UserAId = user.Id;
        UserAEmail = user.Email;

        // Roles & permissions are already seeded by EnsureCreatedAsync (via HasData in model snapshot).
        // Just look up the SuperAdmin role so we can assign it to our test user.
        var superAdminRole = await ctx.Roles.FirstAsync(r => r.NormalizedName == SystemRoles.SuperAdmin.ToUpperInvariant());
        var userRole = UserRole.Create(user.Id, superAdminRole.Id);
        ctx.UserRoles.Add(userRole);
        await ctx.SaveChangesAsync();
    }

    /// <summary>
    /// Creates an HttpClient with test authentication pre-configured.
    /// The test user has SuperAdmin role and PropertyId = PropertyAId.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        // TestAuthHandler reads these headers to build claims
        client.DefaultRequestHeaders.Add("X-Test-UserId", UserAId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-PropertyId", PropertyAId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", SystemRoles.SuperAdmin);
        client.DefaultRequestHeaders.Add("X-Test-Email", UserAEmail);
        return client;
    }

    /// <summary>
    /// Creates an HttpClient with NO authentication (anonymous).
    /// </summary>
    public HttpClient CreateAnonymousClient()
    {
        return CreateClient();
    }

    private class SuperAdminTenantProvider : ITenantProvider
    {
        public Guid CurrentPropertyId => Guid.Empty;
        public bool IsSuperAdmin => true;
        public bool HasTenantContext => true;
        public void ValidatePropertyAccess(Guid requestedPropertyId) { }
    }
}
