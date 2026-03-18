using MediatR;
using SAFARIstack.Core.Domain.Services;
using SAFARIstack.Infrastructure.Data;
using SAFARIstack.Shared.Domain;

namespace SAFARIstack.Infrastructure;

/// <summary>
/// Dispatches domain events via MediatR after the DbContext saves changes
/// </summary>
public class MediatRDomainEventDispatcher : IDomainEventPublisher
{
    private readonly IMediator _mediator;

    public MediatRDomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Implements IDomainEventPublisher — publishes a batch of domain events via MediatR
    /// </summary>
    public async Task PublishEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var domainEvent in events)
        {
            await _mediator.Publish(domainEvent, ct);
        }
    }

    /// <summary>
    /// Dispatches all pending domain events collected during SaveChangesAsync
    /// </summary>
    public async Task DispatchPendingEventsAsync(ApplicationDbContext context, CancellationToken ct = default)
    {
        var events = context.PendingDomainEvents.ToList();
        context.PendingDomainEvents.Clear();

        foreach (var domainEvent in events)
        {
            await _mediator.Publish(domainEvent, ct);
        }
    }
}
