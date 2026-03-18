namespace SAFARIstack.Modules.Channels;

using Microsoft.Extensions.DependencyInjection;
using SAFARIstack.Modules.Channels.Application.Services;
using SAFARIstack.Modules.Channels.Domain.Interfaces;

/// <summary>
/// Channel Manager Module Registration
/// Registers OTA synchronization services
/// </summary>
public static class ChannelsModule
{
    /// <summary>
    /// Register channel management services
    /// Call from Program.cs: ChannelsModule.RegisterServices(builder.Services)
    /// </summary>
    public static void RegisterServices(IServiceCollection services)
    {
        // Core channel manager service
        services.AddScoped<IChannelManager, ChannelManager>();

        // Conflict resolution engine
        services.AddScoped<IConflictResolver, ConflictResolver>();

        // OTA client implementations (add as needed)
        // services.AddScoped<IOTAClient, BookingComClient>();
        // services.AddScoped<IOTAClient, ExpediaClient>();
        // services.AddScoped<IOTAClient, AirbnbClient>();

        // Background job for periodic sync
        services.AddHostedService<ChannelSyncBackgroundService>();
    }
}
