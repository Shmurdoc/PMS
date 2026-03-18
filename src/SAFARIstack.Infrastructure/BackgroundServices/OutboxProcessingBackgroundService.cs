using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SAFARIstack.Infrastructure.Events;

namespace SAFARIstack.Infrastructure.BackgroundServices;

/// <summary>
/// Outbox Processing Background Service — Runs every 10 seconds.
/// 
/// Responsibilities:
/// 1. Periodically process pending OutboxEvents
/// 2. Publish events to message bus with retries
/// 3. Handle failures gracefully without stopping the service
/// 4. Log all operations for observability
///
/// Deployment:
/// - Runs continuously in production
/// - Single instance per environment (no horizontal scaling issues)
/// - Graceful shutdown (waits for in-flight events)
/// </summary>
public class OutboxProcessingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessingBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

    public OutboxProcessingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Outbox Processing Background Service starting (interval: {Interval}s)",
            _interval.TotalSeconds);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processing Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Create scope for each iteration to get fresh DbContext
                using (var scope = _serviceProvider.CreateScope())
                {
                    var outboxPublisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();

                    // Process pending events
                    await outboxPublisher.PublishPendingEventsAsync(stoppingToken);
                }

                // Wait before next iteration
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Outbox Processing Background Service cancellation initiated");
                break;
            }
            catch (Exception ex)
            {
                // Log error but continue processing (don't crash the background service)
                _logger.LogError(ex, "Error in Outbox Processing Background Service iteration");

                // Sleep briefly before retrying to avoid tight error loop
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Outbox Processing Background Service stopping");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Outbox Processing Background Service stopped");
    }
}
