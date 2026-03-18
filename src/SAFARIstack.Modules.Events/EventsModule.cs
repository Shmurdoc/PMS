namespace SAFARIstack.Modules.Events;

using MassTransit;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Event messaging module registration
/// Sets up event publishing/subscribing infrastructure
/// Uses MassTransit for resilient, decoupled event distribution
/// </summary>
public static class EventsModule
{
    /// <summary>
    /// Register event bus and messaging infrastructure
    /// Call from Program.cs: EventsModule.RegisterEventBus(builder.Services)
    /// </summary>
    public static void RegisterEventBus(IServiceCollection services)
    {
        // Configure MassTransit for event distribution
        // Could use RabbitMQ, Azure Service Bus, or in-memory for development
        services.AddMassTransit(x =>
        {
            // Register all event consumers from dependent modules
            // Each module registers its own event handlers
            x.AddConsumers(typeof(EventsModule).Assembly);

            // Use in-memory transport for development
            // TODO: Switch to RabbitMQ or Azure Service Bus for production
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });

            // Alternatively, configure RabbitMQ:
            // x.UsingRabbitMq((context, cfg) =>
            // {
            //     cfg.Host("localhost", h =>
            //     {
            //         h.Username("guest");
            //         h.Password("guest");
            //     });
            //     cfg.ConfigureEndpoints(context);
            // });
        });

        // Add outbox for transactional event publishing
        // Ensures events are published even if publishing fails initially
        // services.AddScoped<IEventPublisher, TransactionalEventPublisher>();
    }
}

/// <summary>
/// Contract for publishing domain events
/// Implemented by infrastructure layer with transactional guarantees
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publish event asynchronously
    /// Event goes to all registered subscribers without blocking
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : class;

    /// <summary>
    /// Publish event synchronously within a transaction
    /// Ensures event is recorded even if publishing fails
    /// </summary>
    Task PublishTransactionalAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : class;
}

/// <summary>
/// Example event consumer (implemented by Analytics module)
/// Demonstrates how other modules subscribe to events without direct coupling
/// </summary>
public interface IEventConsumer<TEvent> where TEvent : class
{
    Task Consume(TEvent @event, CancellationToken ct = default);
}
