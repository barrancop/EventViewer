using EventViewer.Domain.Entities;
using EventViewer.Domain.ValueObjects;

namespace EventViewer.Application.Interfaces;

/// <summary>
/// Contract for the repository that retrieves Service Bus events
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Searches for events based on specified criteria
    /// </summary>
    /// <param name="criteria">Search criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of events matching the criteria</returns>
    Task<IEnumerable<ServiceBusEvent>> SearchEventsAsync(
        EventSearchCriteria criteria, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all events associated with a specific CorrelationId
    /// </summary>
    /// <param name="correlationId">Correlation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of correlated events</returns>
    Task<IEnumerable<ServiceBusEvent>> GetEventsByCorrelationIdAsync(
        string correlationId, 
        CancellationToken cancellationToken = default);
}
