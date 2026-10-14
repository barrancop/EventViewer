namespace EventViewer.Domain.Enums;

/// <summary>
/// Represents the event processing status
/// </summary>
public enum EventStatus
{
    /// <summary>
    /// The event was processed successfully
    /// </summary>
    Processed,
    
    /// <summary>
    /// The event is currently being processed
    /// </summary>
    Processing,
    
    /// <summary>
    /// Event processing failed
    /// </summary>
    Failed
}
