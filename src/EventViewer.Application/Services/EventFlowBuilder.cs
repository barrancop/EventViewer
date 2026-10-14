using EventViewer.Domain.Entities;

namespace EventViewer.Application.Services;

/// <summary>
/// Service that groups correlated events into chronologically ordered flows
/// </summary>
public class EventFlowBuilder
{
    /// <summary>
    /// Builds the event flow from an unorganized list of events
    /// </summary>
    /// <param name="events">List of events</param>
    /// <returns>Organized event flow</returns>
    public EventFlow BuildFlow(IEnumerable<ServiceBusEvent> events)
    {
        var eventList = events.OrderBy(e => e.Timestamp).ToList();
        
        if (!eventList.Any())
        {
            throw new ArgumentException("The event list cannot be empty", nameof(events));
        }
        
        var correlationId = eventList.First().CorrelationId;
        
        // Validate that all events have the same CorrelationId
        if (eventList.Any(e => e.CorrelationId != correlationId))
        {
            throw new ArgumentException("All events must have the same CorrelationId", nameof(events));
        }
        
        return new EventFlow
        {
            CorrelationId = correlationId,
            StartTime = eventList.First().Timestamp,
            EndTime = eventList.Last().Timestamp,
            Events = eventList
        };
    }
    
    /// <summary>
    /// Builds multiple flows grouped by CorrelationId
    /// </summary>
    /// <param name="events">List of events from multiple flows</param>
    /// <returns>List of organized flows</returns>
    public IEnumerable<EventFlow> BuildMultipleFlows(IEnumerable<ServiceBusEvent> events)
    {
        var groupedEvents = events.GroupBy(e => e.CorrelationId);
        
        return groupedEvents.Select(group => BuildFlow(group));
    }
}
