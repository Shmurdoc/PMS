using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAFARIstack.Core.Domain.Entities;
using SAFARIstack.Infrastructure.Data;

namespace SAFARIstack.Infrastructure.Data.Seeders;

/// <summary>
/// DataSeeder — Seed reference data with idempotency checks
/// 
/// Responsibilities:
/// - Create default inventory items per property
/// - Seed master data (payment methods, categories)
/// - Ensure data is never duplicated on re-runs
/// - Log all changes for audit trail
/// </summary>
public interface IDataSeeder
{
    /// <summary>Seed all reference data</summary>
    Task SeedAsync(CancellationToken cancellationToken = default);

    /// <summary>Seed inventory items for a property</summary>
    Task SeedInventoryForPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default);

    /// <summary>Get seed operation status</summary>
    Task<SeedingStatus> GetStatusAsync();
}

public class DataSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(ApplicationDbContext db, ILogger<DataSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting data seeding...");

            // Seed inventory for all properties that don't have any
            await SeedInventoryItemsAsync(cancellationToken);

            _logger.LogInformation("Data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data seeding");
            throw;
        }
    }

    public async Task SeedInventoryForPropertyAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        var property = await _db.Set<Property>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == propertyId, cancellationToken);

        if (property == null)
        {
            _logger.LogWarning("Property {PropertyId} not found for inventory seeding", propertyId);
            return;
        }

        // Check if inventory already seeded for this property
        var existingCount = await _db.Set<InventoryItem>()
            .Where(ii => ii.PropertyId == propertyId)
            .CountAsync(cancellationToken);

        if (existingCount > 0)
        {
            _logger.LogInformation("Property {PropertyId} already has {Count} inventory items, skipping seed", propertyId, existingCount);
            return;
        }

        var items = GetDefaultInventoryItems(propertyId);
        _db.Set<InventoryItem>().AddRange(items);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded {Count} inventory items for property {PropertyId}",
            items.Count, propertyId);
    }

    public async Task<SeedingStatus> GetStatusAsync()
    {
        var totalProperties = await _db.Set<Property>().CountAsync();
        var propertiesWithInventory = await _db.Set<InventoryItem>()
            .Select(ii => ii.PropertyId)
            .Distinct()
            .CountAsync();

        return new SeedingStatus
        {
            TotalProperties = totalProperties,
            PropertiesWithInventorySeeded = propertiesWithInventory,
            PercentComplete = totalProperties > 0 
                ? (propertiesWithInventory / (double)totalProperties) * 100 
                : 100,
            IsComplete = propertiesWithInventory >= totalProperties
        };
    }

    /// <summary>Seed inventory items if not already present</summary>
    private async Task SeedInventoryItemsAsync(CancellationToken cancellationToken)
    {
        // Get all properties
        var properties = await _db.Set<Property>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} properties, checking inventory seeding...", properties.Count);

        int seedCount = 0;

        foreach (var property in properties)
        {
            // Check if inventory already exists for this property
            var hasInventory = await _db.Set<InventoryItem>()
                .AnyAsync(ii => ii.PropertyId == property.Id, cancellationToken);

            if (hasInventory)
            {
                continue; // Already seeded
            }

            // Create default inventory items
            var items = GetDefaultInventoryItems(property.Id);
            _db.Set<InventoryItem>().AddRange(items);

            seedCount += items.Count;
        }

        if (seedCount > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded {Count} inventory items across properties", seedCount);
        }
        else
        {
            _logger.LogInformation("All properties already have inventory seeded");
        }
    }

    /// <summary>Get default inventory items to seed per property</summary>
    private static List<InventoryItem> GetDefaultInventoryItems(Guid propertyId)
    {
        return new List<InventoryItem>
        {
            // Beverages
            InventoryItem.Create(
                propertyId: propertyId,
                sku: "BEV-COFFEE-01",
                name: "Espresso Coffee",
                category: "Beverages",
                initialStock: 50m,
                reorderLevel: 10m,
                stockUnit: "unit",
                costPrice: 2.50m,
                sellingPrice: 5.50m),

            InventoryItem.Create(
                propertyId: propertyId,
                sku: "BEV-TEA-01",
                name: "Black Tea",
                category: "Beverages",
                initialStock: 30m,
                reorderLevel: 5m,
                stockUnit: "unit",
                costPrice: 1.50m,
                sellingPrice: 3.50m),

            InventoryItem.Create(
                propertyId: propertyId,
                sku: "BEV-JUICE-01",
                name: "Orange Juice (1L)",
                category: "Beverages",
                initialStock: 10m,
                reorderLevel: 2m,
                stockUnit: "unit",
                costPrice: 25.00m,
                sellingPrice: 45.00m),

            InventoryItem.Create(
                propertyId: propertyId,
                sku: "BEV-WATER-01",
                name: "Bottled Water (500ml)",
                category: "Beverages",
                initialStock: 100m,
                reorderLevel: 20m,
                stockUnit: "unit",
                costPrice: 5.00m,
                sellingPrice: 12.00m),

            // Snacks
            InventoryItem.Create(
                propertyId: propertyId,
                sku: "SNACK-NUTS-01",
                name: "Mixed Nuts (100g)",
                category: "Snacks",
                initialStock: 25m,
                reorderLevel: 5m,
                stockUnit: "unit",
                costPrice: 15.00m,
                sellingPrice: 35.00m),

            InventoryItem.Create(
                propertyId: propertyId,
                sku: "SNACK-CHOC-01",
                name: "Chocolate Bar",
                category: "Snacks",
                initialStock: 40m,
                reorderLevel: 10m,
                stockUnit: "unit",
                costPrice: 8.00m,
                sellingPrice: 18.00m),

            // Merchandise
            InventoryItem.Create(
                propertyId: propertyId,
                sku: "MERCH-TSHIRT-01",
                name: "Property T-Shirt (M)",
                category: "Merchandise",
                initialStock: 15m,
                reorderLevel: 3m,
                stockUnit: "unit",
                costPrice: 75.00m,
                sellingPrice: 150.00m),

            InventoryItem.Create(
                propertyId: propertyId,
                sku: "MERCH-HAT-01",
                name: "Property Cap",
                category: "Merchandise",
                initialStock: 20m,
                reorderLevel: 5m,
                stockUnit: "unit",
                costPrice: 35.00m,
                sellingPrice: 75.00m),
        };
    }
}

/// <summary>Status of seeding operations</summary>
public class SeedingStatus
{
    public int TotalProperties { get; init; }
    public int PropertiesWithInventorySeeded { get; init; }
    public double PercentComplete { get; init; }
    public bool IsComplete { get; init; }

    public override string ToString() =>
        $"SeedingStatus: {PropertiesWithInventorySeeded}/{TotalProperties} properties seeded ({PercentComplete:F1}%)";
}
