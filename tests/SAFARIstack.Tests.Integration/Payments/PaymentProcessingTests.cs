using Xunit;
using Microsoft.EntityFrameworkCore;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.Tests.Integration.Payments;

public class OzowPaymentProcessingTests : IAsyncLifetime
{
    private ApplicationDbContext _dbContext;
    private Guid _propertyId;
    private Guid _bookingId;
    private Guid _folioId;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        // Create test property
        var property = Property.Create("Test Lodge", "test-property", "123 Main St", "Johannesburg", "Gauteng");
        _propertyId = property.Id;
        _dbContext.Properties.Add(property);

        // Create Ozow merchant configuration
        var ozowConfig = MerchantConfiguration.Create(
            _propertyId,
            "Ozow",
            MerchantProviderType.Ozow,
            "ozow-merchant-id",
            "ozow-api-key",
            "ozow-api-secret",
            isLive: false);
        ozowConfig.UpdateUrls(
            "https://localhost/api/webhooks/ozow",
            "https://localhost/payment-return",
            "https://localhost/payment-cancel",
            "https://localhost/api/webhooks/ozow"
        );
        _dbContext.MerchantConfigurations.Add(ozowConfig);

        // Create test booking and folio
        var guest = Guest.Create(_propertyId, "John", "Doe", "john@test.local", "+27123456789");
        _dbContext.Guests.Add(guest);

        var booking = Booking.Create(
            _propertyId,
            guest.Id,
            "BK-OZOW-001",
            DateTime.Now.AddDays(1),
            DateTime.Now.AddDays(3),
            2,
            0,
            Guid.NewGuid()
        );
        _bookingId = booking.Id;
        _dbContext.Bookings.Add(booking);

        var folio = Folio.Create(_propertyId, _bookingId, guest.Id, "F-OZOW-0001");
        _folioId = folio.Id;
        _dbContext.Folios.Add(folio);

        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task CreateOzowPayment_StoresPaymentRecord()
    {
        var payment = Payment.Create(
            _propertyId,
            _folioId,
            1000.00m,
            PaymentMethod.Ozow,
            "OZOW-PAY-001",
            _bookingId
        );

        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();

        var storedPayment = await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.TransactionReference == "OZOW-PAY-001");

        Assert.NotNull(storedPayment);
        Assert.Equal(1000.00m, storedPayment.Amount);
        Assert.Equal(PaymentMethod.Ozow, storedPayment.Method);
        Assert.Equal(_folioId, storedPayment.FolioId);
    }

    [Fact]
    public async Task RecordOzowPaymentOnFolio_UpdatesFolioBalance()
    {
        var folio = await _dbContext.Folios
            .FirstOrDefaultAsync(f => f.Id == _folioId);
        Assert.NotNull(folio);

        folio.AddCharge("Room Charge", 1000.00m, ChargeCategory.RoomCharge);
        await _dbContext.SaveChangesAsync();

        var payment = Payment.Create(
            _propertyId,
            _folioId,
            500.00m,
            PaymentMethod.Ozow,
            "OZOW-PAY-002"
        );

        folio.RecordPayment(payment);
        await _dbContext.SaveChangesAsync();

        var updatedFolio = await _dbContext.Folios
            .FirstOrDefaultAsync(f => f.Id == _folioId);

        Assert.Equal(1000.00m, updatedFolio!.TotalCharges);
        Assert.Equal(500.00m, updatedFolio.TotalPayments);
        Assert.Equal(500.00m, updatedFolio.Balance);
    }

    [Fact]
    public async Task LogWebhook_CreatesAuditTrail()
    {
        var webhook = WebhookLog.Create(
            "Ozow",
            "ozow-txn-123",
            "completed",
            @"{ ""status"": ""Complete"", ""amount"": 100000 }");

        _dbContext.WebhookLogs.Add(webhook);
        await _dbContext.SaveChangesAsync();

        var stored = await _dbContext.WebhookLogs
            .FirstOrDefaultAsync(w => w.TransactionId == "ozow-txn-123");

        Assert.NotNull(stored);
        Assert.Equal("Ozow", stored.Provider);
        Assert.Equal("completed", stored.Status);
        Assert.False(stored.IsProcessed);
    }
}

public class PayFastPaymentProcessingTests : IAsyncLifetime
{
    private ApplicationDbContext _dbContext;
    private Guid _propertyId;
    private Guid _bookingId;
    private Guid _folioId;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        // Create test property
        var property = Property.Create("Test Lodge", "test-property", "456 Oak Ave", "Cape Town", "Western Cape");
        _propertyId = property.Id;
        _dbContext.Properties.Add(property);

        // Create PayFast merchant configuration
        var payFastConfig = MerchantConfiguration.Create(
            _propertyId,
            "PayFast",
            MerchantProviderType.PayFast,
            "10000001",
            "payfast-merchant-key",
            null,
            isLive: false);
        payFastConfig.UpdateCredentials(null, null, null, "test-passphrase", null);
        payFastConfig.UpdateUrls(
            "https://localhost/api/webhooks/payfast",
            "https://localhost/payment-return",
            "https://localhost/payment-cancel",
            "https://localhost/api/webhooks/payfast"
        );
        _dbContext.MerchantConfigurations.Add(payFastConfig);

        // Create test booking and folio
        var guest = Guest.Create(_propertyId, "Jane", "Smith", "jane@test.local", "+27987654321");
        _dbContext.Guests.Add(guest);

        var booking = Booking.Create(
            _propertyId,
            guest.Id,
            "BK-PF-001",
            DateTime.Now.AddDays(1),
            DateTime.Now.AddDays(3),
            1,
            0,
            Guid.NewGuid()
        );
        _bookingId = booking.Id;
        _dbContext.Bookings.Add(booking);

        var folio = Folio.Create(_propertyId, _bookingId, guest.Id, "F-PF-0001");
        _folioId = folio.Id;
        _dbContext.Folios.Add(folio);

        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task CreatePayFastPayment_StoresPaymentRecord()
    {
        var payment = Payment.Create(
            _propertyId,
            _folioId,
            1500.00m,
            PaymentMethod.PayFast,
            "PF-PAY-001",
            _bookingId
        );

        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();

        var storedPayment = await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.TransactionReference == "PF-PAY-001");

        Assert.NotNull(storedPayment);
        Assert.Equal(1500.00m, storedPayment.Amount);
        Assert.Equal(PaymentMethod.PayFast, storedPayment.Method);
        Assert.Equal(_folioId, storedPayment.FolioId);
    }

    [Fact]
    public async Task RecordPayFastPaymentOnFolio_UpdatesFolioBalance()
    {
        var folio = await _dbContext.Folios
            .FirstOrDefaultAsync(f => f.Id == _folioId);
        Assert.NotNull(folio);

        folio.AddCharge("Room Charge", 1500.00m, ChargeCategory.RoomCharge);
        folio.AddCharge("WiFi", 100.00m, ChargeCategory.Internet);
        await _dbContext.SaveChangesAsync();

        var payment = Payment.Create(
            _propertyId,
            _folioId,
            800.00m,
            PaymentMethod.PayFast,
            "PF-PAY-002"
        );

        folio.RecordPayment(payment);
        await _dbContext.SaveChangesAsync();

        var updatedFolio = await _dbContext.Folios
            .FirstOrDefaultAsync(f => f.Id == _folioId);

        Assert.Equal(1600.00m, updatedFolio!.TotalCharges);
        Assert.Equal(800.00m, updatedFolio.TotalPayments);
        Assert.Equal(800.00m, updatedFolio.Balance);
    }

    [Fact]
    public async Task ProcessPartialPayment_AllowsFurtherPayments()
    {
        var folio = await _dbContext.Folios
            .FirstOrDefaultAsync(f => f.Id == _folioId);
        Assert.NotNull(folio);

        folio.AddCharge("Room Charge", 2000.00m, ChargeCategory.RoomCharge);
        await _dbContext.SaveChangesAsync();

        // First payment
        var payment1 = Payment.Create(
            _propertyId,
            _folioId,
            500.00m,
            PaymentMethod.PayFast,
            "PF-PAY-003"
        );
        folio.RecordPayment(payment1);
        await _dbContext.SaveChangesAsync();

        // Second payment
        var payment2 = Payment.Create(
            _propertyId,
            _folioId,
            1000.00m,
            PaymentMethod.PayFast,
            "PF-PAY-004"
        );
        folio.RecordPayment(payment2);
        await _dbContext.SaveChangesAsync();

        var updatedFolio = await _dbContext.Folios
            .FirstOrDefaultAsync(f => f.Id == _folioId);

        Assert.Equal(2000.00m, updatedFolio!.TotalCharges);
        Assert.Equal(1500.00m, updatedFolio.TotalPayments);
        Assert.Equal(500.00m, updatedFolio.Balance);
    }

    [Fact]
    public async Task MerchantConfigurationCanBeRetrieved()
    {
        var config = await _dbContext.MerchantConfigurations
            .FirstOrDefaultAsync(m =>
                m.PropertyId == _propertyId &&
                m.ProviderType == MerchantProviderType.PayFast);

        Assert.NotNull(config);
        Assert.Equal("10000001", config.MerchantId);
        Assert.Equal("PayFast", config.ProviderName);
        Assert.False(config.IsLive);
        Assert.True(config.IsActive);
    }
}

public class PaymentMethodSupportTests : IAsyncLifetime
{
    private ApplicationDbContext _dbContext;
    private Guid _propertyId;
    private Guid _folioId;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        var property = Property.Create("Test", "test", "1 st", "Durban", "KZN");
        _propertyId = property.Id;
        _dbContext.Properties.Add(property);

        var guest = Guest.Create(_propertyId, "Test", "User", "test@test.local", "+27000000000");
        var booking = Booking.Create(
            _propertyId, guest.Id, "TEST-001",
            DateTime.Now.AddDays(1), DateTime.Now.AddDays(3), 1, 0, Guid.NewGuid()
        );
        var folio = Folio.Create(_propertyId, booking.Id, guest.Id, "FOLIO-TEST");

        _folioId = folio.Id;
        _dbContext.Guests.Add(guest);
        _dbContext.Bookings.Add(booking);
        _dbContext.Folios.Add(folio);

        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync() => await _dbContext.DisposeAsync();

    [Theory]
    [InlineData(PaymentMethod.Cash)]
    [InlineData(PaymentMethod.CreditCard)]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.PayFast)]
    [InlineData(PaymentMethod.Ozow)]
    [InlineData(PaymentMethod.Yoco)]
    [InlineData(PaymentMethod.Voucher)]
    public async Task Payment_SupportsDifferentMethods(PaymentMethod method)
    {
        var payment = Payment.Create(
            _propertyId,
            _folioId,
            100.00m,
            method,
            $"PAY-{method}"
        );

        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();

        var stored = await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.Method == method);

        Assert.NotNull(stored);
        Assert.Equal(method, stored.Method);
    }
}
