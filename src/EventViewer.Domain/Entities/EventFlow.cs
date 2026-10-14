namespace EventViewer.Domain.Entities;

/// <summary>
/// Represents the complete flow of correlated events and their associated logs
/// </summary>
public class EventFlow
{
    /// <summary>
    /// Flow correlation ID
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Flow start date and time (first event timestamp)
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// Flow end date and time (last event timestamp)
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// List of all flow events ordered chronologically
    /// </summary>
    public List<ServiceBusEvent> Events { get; set; } = new();
    
    /// <summary>
    /// Application Insights logs associated with the flow
    /// </summary>
    public List<ApplicationInsightsLog> InsightsLogs { get; set; } = new();
    
    /// <summary>
    /// Total flow duration in milliseconds
    /// </summary>
    public long? DurationMs => EndTime.HasValue 
        ? (long)(EndTime.Value - StartTime).TotalMilliseconds 
        : null;
    
    /// <summary>
    /// Indicates if the flow has any failed events
    /// </summary>
    public bool HasFailures => Events.Any(e => e.Status == Enums.EventStatus.Failed);
    
    /// <summary>
    /// Total number of microservices involved in the flow
    /// </summary>
    public int MicroservicesCount => Events.Select(e => e.Metadata.MicroserviceName).Distinct().Count();
}
