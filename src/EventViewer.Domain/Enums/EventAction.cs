namespace EventViewer.Domain.Enums;

/// <summary>
/// Represents the action performed by the microservice with the event
/// </summary>
public enum EventAction
{
    /// <summary>
    /// The microservice published the event to the Service Bus
    /// </summary>
    Publishing,
    
    /// <summary>
    /// The microservice subscribed and received the event from the Service Bus
    /// </summary>
    Subscribing
}
