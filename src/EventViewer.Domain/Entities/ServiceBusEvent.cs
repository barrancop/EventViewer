using EventViewer.Domain.Enums;

namespace EventViewer.Domain.Entities;

/// <summary>
/// Represents an Azure Service Bus event
/// </summary>
public class ServiceBusEvent
{
    /// <summary>
    /// Event (message) identifier, shared by the publication and all of its subscriptions
    /// </summary>
    public string EventId { get; set; } = string.Empty;
    
    /// <summary>
    /// Correlation ID to track the entire event flow
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Event name (message type)
    /// </summary>
    public string EventName { get; set; } = string.Empty;
    
    /// <summary>
    /// Date and time when the event occurred
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Action performed (Publishing or Subscribing)
    /// </summary>
    public EventAction Action { get; set; }
    
    /// <summary>
    /// Event processing status
    /// </summary>
    public EventStatus Status { get; set; }
    
    /// <summary>
    /// Metadata associated with the event
    /// </summary>
    public EventMetadata Metadata { get; set; } = new();
    
    /// <summary>
    /// Event content (payload) in JSON format
    /// </summary>
    public string? Payload { get; set; }
    
    /// <summary>
    /// Error message if status is Failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// List of child event IDs that were triggered by this event
    /// </summary>
    public List<string> ChildEventIds { get; set; } = new();
}
