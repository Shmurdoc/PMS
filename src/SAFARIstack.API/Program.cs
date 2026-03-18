using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MediatR;
using Serilog;
using FluentValidation;
using AspNetCoreRateLimit;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Infrastructure.Data.Seeders;
using SAFARIstack.Infrastructure.Authentication;
using SAFARIstack.Infrastructure.Resilience;
using SAFARIstack.Infrastructure.Repositories;
using SAFARIstack.Infrastructure;
using SAFARIstack.Infrastructure.Services;
using SAFARIstack.Infrastructure.Services.Payments.Providers;
using SAFARIstack.Core.Domain.Interfaces;
using SAFARIstack.Core.Domain.Services;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.API.Endpoints;
using SAFARIstack.API.Endpoints.Channels;
using SAFARIstack.API.Services;
using SAFARIstack.API.Hubs;
using Polly;
using Polly.Extensions.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════
//  LOGGING (Serilog — structured, enriched, file + console)
// ═══════════════════════════════════════════════════════════════════
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "SAFARIstack-PMS")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ═══════════════════════════════════════════════════════════════════
//  API DOCUMENTATION (config-driven — disabled by default in production)
// ═══════════════════════════════════════════════════════════════════
var swaggerEnabled = builder.Configuration.GetValue<bool>("SwaggerSettings:Enabled");
if (swaggerEnabled)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "SAFARIstack PMS API",
            Version = "v1",
            Description = "Enterprise Lodge & Hotel Management System — Clean Architecture with DDD"
        });

        options.AddSecurityDefinition("Bearer", new()
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        options.AddSecurityDefinition("X-Reader-API-Key", new()
        {
            Description = "RFID Reader API Key authentication",
            Name = "X-Reader-API-Key",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
        });

        options.AddSecurityRequirement(new()
        {
            {
                new()
                {
                    Reference = new()
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
}

// ═══════════════════════════════════════════════════════════════════
//  DATABASE — override connection string from environment BEFORE use
// ═══════════════════════════════════════════════════════════════════
var envConnStr = Environment.GetEnvironmentVariable("SAFARISTACK_CONNECTION_STRING");
if (!string.IsNullOrEmpty(envConnStr))
    builder.Configuration["ConnectionStrings:DefaultConnection"] = envConnStr;

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// ═══════════════════════════════════════════════════════════════════
//  JSON SERIALIZATION — enums as strings for REST API ergonomics
// ═══════════════════════════════════════════════════════════════════
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

// ═══════════════════════════════════════════════════════════════════
//  UNIT OF WORK & REPOSITORIES
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IGuestRepository, GuestRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRoomTypeRepository, RoomTypeRepository>();
builder.Services.AddScoped<IFolioRepository, FolioRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IRateRepository, RateRepository>();
builder.Services.AddScoped<IHousekeepingRepository, HousekeepingRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IPropertySettingsRepository, PropertySettingsRepository>();
builder.Services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
builder.Services.AddScoped<IMerchantConfigurationRepository, MerchantConfigurationRepository>();

// ═══════════════════════════════════════════════════════════════════
//  DOMAIN SERVICES
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IHousekeepingScheduler, HousekeepingScheduler>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDomainEventPublisher, MediatRDomainEventDispatcher>();
builder.Services.AddScoped<MediatRDomainEventDispatcher>();

// ═══════════════════════════════════════════════════════════════════
//  POS SERVICES (Point of Sale, Inventory Management)
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddScoped<IPOSService, SAFARIstack.Infrastructure.Services.POSService>();
builder.Services.AddScoped<IInventoryService, SAFARIstack.Infrastructure.Services.InventoryService>();

// ═══════════════════════════════════════════════════════════════════
//  ENTERPRISE SERVICES (Upgrade Modules)
// ═══════════════════════════════════════════════════════════════════
SAFARIstack.Modules.Analytics.AnalyticsModule.RegisterServices(builder.Services);
SAFARIstack.Modules.Revenue.RevenueModule.RegisterServices(builder.Services);
SAFARIstack.Modules.Channels.ChannelsModule.RegisterServices(builder.Services);
SAFARIstack.Modules.Events.EventsModule.RegisterEventBus(builder.Services);
SAFARIstack.Modules.Addons.AddonsModule.RegisterServices(builder.Services);

builder.Services.AddScoped<IMultiPropertyService, SAFARIstack.Infrastructure.Services.MultiPropertyService>();
builder.Services.AddScoped<IUpsellEngine, SAFARIstack.Infrastructure.Services.UpsellEngine>();
builder.Services.AddScoped<IGiftCardService, SAFARIstack.Infrastructure.Services.GiftCardService>();
builder.Services.AddScoped<IExperienceBookingService, SAFARIstack.Infrastructure.Services.ExperienceBookingService>();
builder.Services.AddScoped<IAiConciergeService, SAFARIstack.Infrastructure.Services.AiConciergeService>();
builder.Services.AddScoped<IGuestInboxService, SAFARIstack.Infrastructure.Services.GuestInboxService>();
builder.Services.AddScoped<IDigitalCheckInService, SAFARIstack.Infrastructure.Services.DigitalCheckInService>();
builder.Services.AddScoped<IReportService, SAFARIstack.Infrastructure.Services.ReportService>();

// ═══════════════════════════════════════════════════════════════════
//  PAYMENT GATEWAY SERVICES (Ozow, PayFast)
// ═══════════════════════════════════════════════════════════════════
// Register payment gateway configurations from appsettings
var ozowConfig = builder.Configuration.GetSection("PaymentGateways:Ozow")
    .Get<SAFARIstack.Infrastructure.Services.Payments.Providers.OzowConfiguration>()
    ?? new SAFARIstack.Infrastructure.Services.Payments.Providers.OzowConfiguration
    {
        ApiKey = Environment.GetEnvironmentVariable("OZOW_API_KEY") ?? "",
        WebhookSecret = Environment.GetEnvironmentVariable("OZOW_WEBHOOK_SECRET") ?? "",
        IsLive = !builder.Environment.IsDevelopment()
    };

var payFastConfig = builder.Configuration.GetSection("PaymentGateways:PayFast")
    .Get<SAFARIstack.Infrastructure.Services.Payments.Providers.PayFastConfiguration>()
    ?? new SAFARIstack.Infrastructure.Services.Payments.Providers.PayFastConfiguration
    {
        MerchantId = Environment.GetEnvironmentVariable("PAYFAST_MERCHANT_ID") ?? "",
        MerchantKey = Environment.GetEnvironmentVariable("PAYFAST_MERCHANT_KEY") ?? "",
        PassPhrase = Environment.GetEnvironmentVariable("PAYFAST_PASSPHRASE"),
        IsLive = !builder.Environment.IsDevelopment(),
        ReturnUrl = builder.Configuration["PaymentGateways:PayFast:ReturnUrl"],
        CancelUrl = builder.Configuration["PaymentGateways:PayFast:CancelUrl"],
        NotifyUrl = builder.Configuration["PaymentGateways:PayFast:NotifyUrl"]
    };

var yocoConfig = builder.Configuration.GetSection("PaymentGateways:Yoco")
    .Get<SAFARIstack.Infrastructure.Services.Payments.Providers.YocoConfiguration>()
    ?? new SAFARIstack.Infrastructure.Services.Payments.Providers.YocoConfiguration
    {
        ApiKey = Environment.GetEnvironmentVariable("YOCO_API_KEY") ?? "",
        WebhookSecret = Environment.GetEnvironmentVariable("YOCO_WEBHOOK_SECRET") ?? "",
        IsLive = !builder.Environment.IsDevelopment()
    };

builder.Services.AddSingleton(ozowConfig);
builder.Services.AddSingleton(payFastConfig);
builder.Services.AddSingleton(yocoConfig);

// Register HTTP clients for payment providers
builder.Services.AddHttpClient<SAFARIstack.Infrastructure.Services.Payments.Providers.OzowPaymentProvider>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

builder.Services.AddHttpClient<SAFARIstack.Infrastructure.Services.Payments.Providers.PayFastPaymentProvider>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

builder.Services.AddHttpClient<SAFARIstack.Infrastructure.Services.Payments.Providers.YocoPaymentProvider>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

// Register payment providers as scoped services
builder.Services.AddScoped<SAFARIstack.Infrastructure.Services.Payments.Providers.OzowPaymentProvider>();
builder.Services.AddScoped<SAFARIstack.Infrastructure.Services.Payments.Providers.PayFastPaymentProvider>();
builder.Services.AddScoped<SAFARIstack.Infrastructure.Services.Payments.Providers.YocoPaymentProvider>();

// Register payment gateway service (orchestrates provider selection)
builder.Services.AddScoped<IPaymentGatewayService, SAFARIstack.Infrastructure.Services.Payments.PaymentGatewayService>();

// ═══════════════════════════════════════════════════════════════════
//  NOTIFICATIONS (Email & SMS)
// ═══════════════════════════════════════════════════════════════════
builder.Services.Configure<SAFARIstack.Infrastructure.Services.Notifications.EmailConfiguration>(
    builder.Configuration.GetSection("Email"));
builder.Services.Configure<SAFARIstack.Infrastructure.Services.Notifications.SmsConfiguration>(
    builder.Configuration.GetSection("Sms"));

// Email service (SMTP)
builder.Services.AddScoped<SAFARIstack.Infrastructure.Services.Notifications.IEmailService,
    SAFARIstack.Infrastructure.Services.Notifications.SmtpEmailService>();

// SMS service (multi-provider support)
builder.Services.AddHttpClient<SAFARIstack.Infrastructure.Services.Notifications.ISmsService,
    SAFARIstack.Infrastructure.Services.Notifications.SmsService>()
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10));

// Notification coordinator (email + SMS orchestration)
builder.Services.AddScoped<SAFARIstack.Infrastructure.Services.Notifications.INotificationService,
    SAFARIstack.Infrastructure.Services.Notifications.NotificationService>();

// ═══════════════════════════════════════════════════════════════════
//  REPORT GENERATION (QuestPDF)
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddScoped<SAFARIstack.Infrastructure.Services.Reports.IReportService,
    SAFARIstack.Infrastructure.Services.Reports.ReportService>();

// ═══════════════════════════════════════════════════════════════════
//  MEDIATR (CQRS)
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(Program).Assembly,
        typeof(SAFARIstack.Core.Application.Bookings.Commands.CreateBookingCommand).Assembly,
        typeof(SAFARIstack.Modules.Staff.Application.Attendance.Commands.RfidCheckInCommand).Assembly);
    cfg.AddBehavior(typeof(MediatR.IPipelineBehavior<,>), typeof(SAFARIstack.Core.Application.ValidationBehavior<,>));
});

// ═══════════════════════════════════════════════════════════════════
//  FLUENT VALIDATION
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddValidatorsFromAssemblyContaining<SAFARIstack.Core.Application.Bookings.CreateBookingCommandValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<SAFARIstack.API.Endpoints.CreateGuestRequestValidator>();

// ═══════════════════════════════════════════════════════════════════
//  AUTHENTICATION
// ═══════════════════════════════════════════════════════════════════
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
var rfidSettings = builder.Configuration.GetSection("RfidAuthentication").Get<RfidAuthenticationSettings>() ?? new RfidAuthenticationSettings();

// Override JWT secret from environment variable (production) or user-secrets (dev)
var envSecret = Environment.GetEnvironmentVariable("SAFARISTACK_JWT_SECRET");
if (!string.IsNullOrEmpty(envSecret))
    jwtSettings.SecretKey = envSecret;

// Note: Connection string env-var override is now applied earlier (before AddDbContext)

// ═══ FAIL-FAST: Validate critical configuration on startup ═══
if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey) ||
    jwtSettings.SecretKey.StartsWith("OVERRIDE") ||
    jwtSettings.SecretKey.Length < 32)
{
    throw new InvalidOperationException(
        "FATAL: JWT secret key is not configured or too short (minimum 32 characters). " +
        "Set the SAFARISTACK_JWT_SECRET environment variable or configure JwtSettings:SecretKey via user-secrets. " +
        "Application cannot start without a valid signing key.");
}

var currentConnStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
if (currentConnStr.Contains("OVERRIDE") || string.IsNullOrWhiteSpace(currentConnStr))
{
    throw new InvalidOperationException(
        "FATAL: Database connection string is not configured. " +
        "Set the SAFARISTACK_CONNECTION_STRING environment variable or configure ConnectionStrings:DefaultConnection via user-secrets.");
}

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(rfidSettings);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
    };

    // SignalR sends JWT via query-string — extract it for /hubs/* requests
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
})
.AddScheme<RfidReaderAuthenticationOptions, RfidReaderAuthenticationHandler>(
    RfidReaderAuthenticationOptions.SchemeName,
    options => { });

builder.Services.AddAuthorization(options =>
{
    // ─── Permission-based policies ─────────────────────────────────
    // Each policy requires the "permission" claim matching the permission name.
    // SuperAdmin/PropertyAdmin get ALL permissions via seeded role-permission mappings.
    foreach (var (permName, _) in Permissions.GetAll())
    {
        var policyName = permName.Replace(".", "").Replace(" ", "");
        // Capitalize first letter of each segment for policy name: "bookings.view" → "BookingsView"
        var parts = permName.Split('.');
        var formattedName = string.Concat(parts.Select(p => char.ToUpper(p[0]) + p[1..]));
        options.AddPolicy(formattedName, policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim("permission", permName) ||
                context.User.IsInRole(SystemRoles.SuperAdmin) ||
                context.User.IsInRole(SystemRoles.PropertyAdmin)));
    }

    // ─── Role-based shortcut policies ──────────────────────────────
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(SystemRoles.SuperAdmin, SystemRoles.PropertyAdmin));

    options.AddPolicy("ManagerOrAbove", policy =>
        policy.RequireRole(SystemRoles.SuperAdmin, SystemRoles.PropertyAdmin, SystemRoles.Manager));

    // ─── RFID reader policy ────────────────────────────────────────
    options.AddPolicy(RfidReaderAuthenticationOptions.SchemeName, policy =>
    {
        policy.AuthenticationSchemes.Add(RfidReaderAuthenticationOptions.SchemeName);
        policy.RequireAuthenticatedUser();
    });
});

// ═══════════════════════════════════════════════════════════════════
//  AUTH SERVICE (RBAC — Registration, Login, Role Management)
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ═══════════════════════════════════════════════════════════════════
//  EVENT OUTBOX (Transactional Outbox Pattern for Reliable Events)
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddScoped<SAFARIstack.Infrastructure.Events.IOutboxPublisher, 
    SAFARIstack.Infrastructure.Events.OutboxPublisher>();
builder.Services.AddHostedService<SAFARIstack.Infrastructure.BackgroundServices.OutboxProcessingBackgroundService>();

// ═══════════════════════════════════════════════════════════════════
//  DATA SEEDING (Reference Data, Inventory Items)
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddScoped<IDataSeeder, DataSeeder>();

// ═══════════════════════════════════════════════════════════════════
//  GLOBAL EXCEPTION HANDLER
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ═══════════════════════════════════════════════════════════════════
//  RESILIENCE & CACHING
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IEdgeBuffer, EdgeBuffer>();

// ═══════════════════════════════════════════════════════════════════
//  RESPONSE COMPRESSION
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "application/problem+json" });
});

// ═══════════════════════════════════════════════════════════════════
//  HEALTH CHECKS (Kubernetes/ECS readiness & liveness probes)
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        timeout: TimeSpan.FromSeconds(5),
        tags: new[] { "ready", "db" });

// ═══════════════════════════════════════════════════════════════════
//  REQUEST BODY SIZE LIMITS
// ═══════════════════════════════════════════════════════════════════
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 5 * 1024 * 1024; // 5 MB max (PMS has no file uploads)
});

// ═══════════════════════════════════════════════════════════════════
//  CORS
// ═══════════════════════════════════════════════════════════════════
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? (builder.Environment.IsDevelopment()
        ? new[] { "http://localhost:3000", "http://localhost:5173", "http://localhost:4200" }
        : Array.Empty<string>());

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // SignalR requires AllowCredentials — cannot combine with AllowAnyOrigin
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// ═══════════════════════════════════════════════════════════════════
//  SIGNALR (Real-time PMS Hub)
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});

// Register SignalR notification service (real-time broadcast helper)
builder.Services.AddScoped<ISignalRNotificationService, SignalRNotificationService>();

// ═════════════════════════════════════════════════════════════════
//  RATE LIMITING
// ═════════════════════════════════════════════════════════════════
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();

// ═══════════════════════════════════════════════════════════════════
//  MIDDLEWARE PIPELINE
// ═══════════════════════════════════════════════════════════════════
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SAFARIstack PMS API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseExceptionHandler();
app.UseResponseCompression();

// ─── Security Headers (all environments) ────────────────────────
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }
    await next();
});

app.UseIpRateLimiting();
app.UseCorrelationId();               // Correlation ID for request tracing
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.Items["CorrelationId"]?.ToString() ?? "unknown");
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown");
        diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value ?? "anonymous");
        }
    };
});
app.UseHttpsRedirection();
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();

// ═══════════════════════════════════════════════════════════════════
//  MAP ALL ENDPOINTS
// ═══════════════════════════════════════════════════════════════════
app.MapAuthEndpoints();
app.MapBookingEndpoints();
app.MapStaffEndpoints();
app.MapStaffManagementEndpoints();
app.MapRfidEndpoints();
app.MapRfidManagementEndpoints();
app.MapGuestEndpoints();
app.MapRoomEndpoints();
app.MapFinancialEndpoints();
app.MapPaymentEndpoints();
app.MapHousekeepingEndpoints();
app.MapRateEndpoints();
app.MapSettingsEndpoints();

// ─── Enterprise Upgrade Endpoints ───────────────────────────────
app.MapMultiPropertyEndpoints();
app.MapUpsellEndpoints();
app.MapGiftCardEndpoints();
app.MapExperienceEndpoints();
app.MapGuestInboxEndpoints();
app.MapAiConciergeEndpoints();
app.MapDigitalCheckInEndpoints();
app.MapReportEndpoints();app.MapChannelEndpoints();app.MapCompatibilityEndpoints();   // Route aliases for all frontend clients
app.MapPropertyEndpoints();
app.MapAuditLogEndpoints();
app.MapAmenityEndpoints();
app.MapCancellationPolicyEndpoints();
app.MapRatePlanEndpoints();
app.MapSeasonEndpoints();
app.MapGuestPreferenceEndpoints();
app.MapRoomTypeCrudEndpoints();
app.MapBookingOperationsEndpoints();
app.MapRoomOperationsEndpoints();
app.MapFinancialOperationsEndpoints();
app.MapAdminEndpoints();

// ─── Advanced Analytics & Revenue ──────────────────────────────
app.MapAnalyticsEndpoints();
app.MapRevenueEndpoints();

// ─── Event Publishing ───────────────────────────────────────────
app.MapEventsEndpoints();

// ─── Charts & Real-time ─────────────────────────────────────────
app.MapChartEndpoints();
app.MapDashboardDataEndpoints();
app.MapHub<PmsHub>("/hubs/pms");

// ─── Staff & Guest Services ──────────────────────────────────────
//app.MapOvertimeEndpoints();  // TODO: Fix phantom compiler error
//app.MapGuestFeedbackEndpoints();

// ─── Health Checks (standard ASP.NET Core — K8s/ECS compatible) ──
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            Status = report.Status.ToString(),
            Duration = report.TotalDuration.TotalMilliseconds,
            Checks = report.Entries.Select(e => new
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Duration = e.Value.Duration.TotalMilliseconds,
                Error = e.Value.Exception?.Message
            }),
            Timestamp = DateTime.UtcNow,
            Version = "2.1.0",
            Environment = app.Environment.EnvironmentName
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(result,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // liveness = app process is running, no dependency checks
}).AllowAnonymous();

// ═══════════════════════════════════════════════════════════════════
//  DATABASE SEEDER — Seed system roles, permissions, and role-permission mappings
// ═══════════════════════════════════════════════════════════════════
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

    try
    {
        // Ensure migrations are applied
        await db.Database.MigrateAsync();

        // Seed default property if none exist
        if (!await db.Properties.AnyAsync())
        {
            logger.LogInformation("Seeding default property...");
            var defaultProperty = Property.Create(
                "Demo Lodge",
                "demo-lodge",
                "123 Safari Road",
                "Cape Town",
                "Western Cape");
            await db.Properties.AddAsync(defaultProperty);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded default property: {PropertyId}", defaultProperty.Id);
        }

        // Seed system roles if none exist
        if (!await db.Roles.AnyAsync())
        {
            logger.LogInformation("Seeding system roles...");
            var roles = new[]
            {
                Role.Create(SystemRoles.SuperAdmin, "System super administrator", true, 0),
                Role.Create(SystemRoles.PropertyAdmin, "Property administrator", true, 1),
                Role.Create(SystemRoles.Manager, "Property manager", true, 2),
                Role.Create(SystemRoles.FrontDesk, "Front desk staff", true, 3),
                Role.Create(SystemRoles.Housekeeping, "Housekeeping staff", true, 4),
                Role.Create(SystemRoles.Finance, "Finance staff", true, 5),
                Role.Create(SystemRoles.Maintenance, "Maintenance staff", true, 6),
                Role.Create(SystemRoles.Receptionist, "Receptionist", true, 7),
            };
            await db.Roles.AddRangeAsync(roles);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} system roles", roles.Length);
        }

        // Seed permissions if none exist
        if (!await db.Permissions.AnyAsync())
        {
            logger.LogInformation("Seeding permissions...");
            var permissions = Permissions.GetAll()
                .Select(p => Permission.Create(p.Name, p.Module, $"Permission: {p.Name}"))
                .ToList();
            await db.Permissions.AddRangeAsync(permissions);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} permissions", permissions.Count);
        }

        // Seed default users if none exist
        if (!await db.ApplicationUsers.AnyAsync())
        {
            logger.LogInformation("Seeding default users...");
            var property = await db.Properties.FirstAsync();
            
            // Password: Safari@2026! (hashed with bcrypt cost 12)
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Safari@2026!", BCrypt.Net.BCrypt.GenerateSalt(12));
            
            var users = new[]
            {
                ApplicationUser.Create(property.Id, "admin@safaristack.com", passwordHash, "Admin", "User", "+27820000000"),
                ApplicationUser.Create(property.Id, "manager@safaristack.com", passwordHash, "Manager", "User", "+27820000001"),
                ApplicationUser.Create(property.Id, "reception@safaristack.com", passwordHash, "Reception", "User", "+27820000002"),
            };
            
            await db.ApplicationUsers.AddRangeAsync(users);
            await db.SaveChangesAsync();

            // Assign roles to users
            var superAdminRole = await db.Roles.FirstAsync(r => r.NormalizedName == SystemRoles.SuperAdmin.ToUpperInvariant());
            var managerRole = await db.Roles.FirstAsync(r => r.NormalizedName == SystemRoles.Manager.ToUpperInvariant());
            var frontDeskRole = await db.Roles.FirstAsync(r => r.NormalizedName == SystemRoles.FrontDesk.ToUpperInvariant());

            var userRoles = new[]
            {
                UserRole.Create(users[0].Id, superAdminRole.Id),
                UserRole.Create(users[1].Id, managerRole.Id),
                UserRole.Create(users[2].Id, frontDeskRole.Id),
            };

            await db.UserRoles.AddRangeAsync(userRoles);
            await db.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} default users", users.Length);
        }

        // Also seed guest demo user
        if (!await db.ApplicationUsers.AnyAsync(u => u.Email == "guest@demo.com"))
        {
            logger.LogInformation("Seeding guest demo user...");
            var property = await db.Properties.FirstAsync();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Guest@2026!", BCrypt.Net.BCrypt.GenerateSalt(12));
            var guestUser = ApplicationUser.Create(property.Id, "guest@demo.com", passwordHash, "Guest", "Demo", "+27820000099");
            guestUser.ConfirmEmail();
            
            await db.ApplicationUsers.AddAsync(guestUser);
            await db.SaveChangesAsync();

            var frontDeskRole = await db.Roles.FirstAsync(r => r.NormalizedName == SystemRoles.FrontDesk.ToUpperInvariant());
            var guestUserRole = UserRole.Create(guestUser.Id, frontDeskRole.Id);
            await db.UserRoles.AddAsync(guestUserRole);
            await db.SaveChangesAsync();

            logger.LogInformation("Seeded guest demo user");
        }

        // Seed reference data (inventory items, etc.)
        var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database seeding skipped or failed — will retry on next startup");
    }
}

app.Run();

// ═══════════════════════════════════════════════════════════════════
//  Expose Program class for integration testing (WebApplicationFactory)
// ═══════════════════════════════════════════════════════════════════
public partial class Program { }
