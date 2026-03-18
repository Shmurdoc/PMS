using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SAFARIstack.Core.Application.Services;
using SAFARIstack.Core.Domain.Addons;
using SAFARIstack.Infrastructure.Data.Models;

namespace SAFARIstack.Infrastructure.Data.Services;

/// <summary>
/// Implementation of addon manager for handling installation, configuration, and lifecycle
/// </summary>
public class AddOnManager : IAddOnManager
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AddOnManager> _logger;
    private readonly Dictionary<string, (IAddOn Instance, AddOnLifecyclePhase Phase)> _loadedAddOns = new();

    public AddOnManager(
        ApplicationDbContext dbContext,
        IServiceProvider serviceProvider,
        ILogger<AddOnManager> logger)
    {
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<AddOnOperationResult> InstallAddOnAsync(
        string addOnId,
        string version,
        string packagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if already installed
            var existing = await _dbContext.InstalledAddOns
                .FirstOrDefaultAsync(a => a.AddOnId == addOnId, cancellationToken);

            if (existing != null)
                return AddOnOperationResult.Fail($"AddOn {addOnId} is already installed");

            // Load addon assembly and get IAddOn instance
            var addOnInstance = LoadAddOnAssembly(packagePath, addOnId);
            if (addOnInstance == null)
                return AddOnOperationResult.Fail($"Failed to load AddOn assembly from {packagePath}");

            // Call OnInstall hook
            var installResult = await addOnInstance.OnInstallAsync(cancellationToken);
            if (!installResult.Success)
                return installResult;

            // Record in database
            var addOnRecord = new InstalledAddOn
            {
                Id = Guid.NewGuid(),
                AddOnId = addOnId,
                Name = addOnInstance.Name,
                Version = version,
                Status = AddOnLifecyclePhase.Installed,
                IsEnabled = true,
                InstalledAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.InstalledAddOns.Add(addOnRecord);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Load into memory
            _loadedAddOns[addOnId] = (addOnInstance, AddOnLifecyclePhase.Installed);

            _logger.LogInformation("AddOn installed: {AddOnId} v{Version}", addOnId, version);

            // Call OnInitialize hook
            var initResult = await addOnInstance.OnInitializeAsync(cancellationToken);
            if (!initResult.Success)
                _logger.LogWarning("AddOn initialization warning: {Message}", initResult.Message);

            return AddOnOperationResult.Ok($"AddOn {addOnId} installed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install AddOn {AddOnId}", addOnId);
            return AddOnOperationResult.Fail("Installation failed", ex);
        }
    }

    public async Task<AddOnOperationResult> UninstallAddOnAsync(
        string addOnId,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var addOnRecord = await _dbContext.InstalledAddOns
                .FirstOrDefaultAsync(a => a.AddOnId == addOnId, cancellationToken);

            if (addOnRecord == null)
                return AddOnOperationResult.Fail($"AddOn {addOnId} not found");

            if (!_loadedAddOns.ContainsKey(addOnId) && !force)
                return AddOnOperationResult.Fail($"AddOn {addOnId} is not loaded");

            // Call OnUninstall hook if loaded
            if (_loadedAddOns.TryGetValue(addOnId, out var addOnData))
            {
                var uninstallResult = await addOnData.Instance.OnUninstallAsync(cancellationToken);
                if (!uninstallResult.Success && !force)
                    return uninstallResult;

                _loadedAddOns.Remove(addOnId);
            }

            // Remove from database
            _dbContext.InstalledAddOns.Remove(addOnRecord);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("AddOn uninstalled: {AddOnId}", addOnId);
            return AddOnOperationResult.Ok($"AddOn {addOnId} uninstalled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall AddOn {AddOnId}", addOnId);
            return AddOnOperationResult.Fail("Uninstallation failed", ex);
        }
    }

    public async Task<AddOnOperationResult> UpdateAddOnAsync(
        string addOnId,
        string newVersion,
        string packagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var addOnRecord = await _dbContext.InstalledAddOns
                .FirstOrDefaultAsync(a => a.AddOnId == addOnId, cancellationToken);

            if (addOnRecord == null)
                return AddOnOperationResult.Fail($"AddOn {addOnId} not found");

            if (!_loadedAddOns.TryGetValue(addOnId, out var addOnData))
                return AddOnOperationResult.Fail($"AddOn {addOnId} is not loaded");

            var oldVersion = addOnRecord.Version;

            // Call OnUpdate hook
            var updateResult = await addOnData.Instance.OnUpdateAsync(oldVersion, cancellationToken);
            if (!updateResult.Success)
                return updateResult;

            // Update database
            addOnRecord.Version = newVersion;
            addOnRecord.UpdatedAt = DateTime.UtcNow;
            _dbContext.InstalledAddOns.Update(addOnRecord);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("AddOn updated: {AddOnId} from v{OldVersion} to v{NewVersion}",
                addOnId, oldVersion, newVersion);

            return AddOnOperationResult.Ok($"AddOn {addOnId} updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update AddOn {AddOnId}", addOnId);
            return AddOnOperationResult.Fail("Update failed", ex);
        }
    }

    public async Task<AddOnOperationResult> SetAddOnStateAsync(
        string addOnId,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        var addOnRecord = await _dbContext.InstalledAddOns
            .FirstOrDefaultAsync(a => a.AddOnId == addOnId, cancellationToken);

        if (addOnRecord == null)
            return AddOnOperationResult.Fail($"AddOn {addOnId} not found");

        addOnRecord.IsEnabled = enabled;
        addOnRecord.UpdatedAt = DateTime.UtcNow;
        _dbContext.InstalledAddOns.Update(addOnRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("AddOn state changed: {AddOnId} Enabled={Enabled}", addOnId, enabled);
        return AddOnOperationResult.Ok($"AddOn {addOnId} {(enabled ? "enabled" : "disabled")}");
    }

    public async Task<List<AddOnLifecyclePhase>> GetInstalledAddOnsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.InstalledAddOns
            .Where(a => a.IsEnabled)
            .Select(a => a.Status)
            .ToListAsync(cancellationToken);
    }

    public async Task<AddOnMetadata?> GetAddOnMetadataAsync(
        string addOnId,
        CancellationToken cancellationToken = default)
    {
        if (!_loadedAddOns.TryGetValue(addOnId, out var addOnData))
            return null;

        return await Task.FromResult(addOnData.Instance.GetMetadata());
    }

    public async Task<Dictionary<string, object>> GetAddOnConfigAsync(
        string addOnId,
        CancellationToken cancellationToken = default)
    {
        if (!_loadedAddOns.TryGetValue(addOnId, out var addOnData))
            return new Dictionary<string, object>();

        return await addOnData.Instance.GetConfigAsync(cancellationToken);
    }

    public async Task<AddOnOperationResult> SetAddOnConfigAsync(
        string addOnId,
        Dictionary<string, object> config,
        CancellationToken cancellationToken = default)
    {
        if (!_loadedAddOns.TryGetValue(addOnId, out var addOnData))
            return AddOnOperationResult.Fail($"AddOn {addOnId} not loaded");

        return await addOnData.Instance.SetConfigAsync(config, cancellationToken);
    }

    public async Task<bool> IsAddOnHealthyAsync(string addOnId, CancellationToken cancellationToken = default)
    {
        if (!_loadedAddOns.TryGetValue(addOnId, out var addOnData))
            return false;

        try
        {
            return await addOnData.Instance.IsHealthyAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<AddOnOperationResult>> RaiseEventAsync(
        AddOnEventHook eventHook,
        Dictionary<string, object> eventData,
        CancellationToken cancellationToken = default)
    {
        var results = new List<AddOnOperationResult>();

        foreach (var (addOnId, (instance, _)) in _loadedAddOns.Where(x => x.Value.Phase == AddOnLifecyclePhase.Installed))
        {
            try
            {
                if (instance is IAddOnEventListener listener && listener.GetSubscribedEvents().Contains(eventHook))
                {
                    var result = await listener.HandleEventAsync(eventHook, eventData, cancellationToken);
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising event {EventHook} to AddOn {AddOnId}", eventHook, addOnId);
                results.Add(AddOnOperationResult.Fail($"Error in {addOnId}", ex));
            }
        }

        return results;
    }

    public async Task<List<AddOnApiRoute>> GetAddOnRoutesAsync(string addOnId, CancellationToken cancellationToken = default)
    {
        if (!_loadedAddOns.TryGetValue(addOnId, out var addOnData))
            return new List<AddOnApiRoute>();

        return await Task.FromResult(addOnData.Instance.GetApiRoutes().ToList());
    }

    private IAddOn? LoadAddOnAssembly(string packagePath, string addOnId)
    {
        try
        {
            var assembly = Assembly.LoadFrom(packagePath);
            var addOnTypes = assembly.GetTypes()
                .Where(t => typeof(IAddOn).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            if (addOnTypes.Count == 0)
            {
                _logger.LogWarning("No IAddOn implementations found in {PackagePath}", packagePath);
                return null;
            }

            var addOnType = addOnTypes.First();
            var instance = Activator.CreateInstance(addOnType) as IAddOn;

            return instance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load AddOn assembly: {PackagePath}", packagePath);
            return null;
        }
    }
}
