namespace EventViewer.Domain.Entities;

/// <summary>
/// Metadata associated with a Service Bus event
/// </summary>
public class EventMetadata
{
    /// <summary>
    /// Name of the microservice that processed/published the event
    /// </summary>
    public string MicroserviceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Service Bus topic name
    /// </summary>
    public string TopicName { get; set; } = string.Empty;
    
    /// <summary>
    /// Service Bus subscription name
    /// </summary>
    public string? SubscriptionName { get; set; }
    
    /// <summary>
    /// Rule name applied in the subscription
    /// </summary>
    public string? RuleName { get; set; }
    
    /// <summary>
    /// User ID that triggered the action (if applicable)
    /// </summary>
    public string? UserId { get; set; }
}
