namespace EventViewer.API.DTOs;

/// <summary>
/// Response DTO for event flow operations
/// </summary>
public class EventFlowResponse
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// List of event flows found
    /// </summary>
    public List<EventFlowDto>? Flows { get; set; }
    
    /// <summary>
    /// Path to saved file (if SaveResults was true)
    /// </summary>
    public string? SavedFilePath { get; set; }
}

/// <summary>
/// DTO representing an event flow
/// </summary>
public class EventFlowDto
{
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<ServiceBusEventDto> Events { get; set; } = new();
    public List<ApplicationInsightsLogDto> InsightsLogs { get; set; } = new();
    public long? DurationMs { get; set; }
    public bool HasFailures { get; set; }
    public int MicroservicesCount { get; set; }
}

/// <summary>
/// DTO representing a Service Bus event
/// </summary>
public class ServiceBusEventDto
{
    public string EventId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public EventMetadataDto Metadata { get; set; } = new();
    public string? Payload { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ChildEventIds { get; set; } = new();
}

/// <summary>
/// DTO representing event metadata
/// </summary>
public class EventMetadataDto
{
    public string MicroserviceName { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public string? SubscriptionName { get; set; }
    public string? RuleName { get; set; }
    public string? UserId { get; set; }
}

/// <summary>
/// DTO representing an Application Insights log
/// </summary>
public class ApplicationInsightsLogDto
{
    public string LogId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string SeverityLevel { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string MicroserviceName { get; set; } = string.Empty;
    public string? OperationName { get; set; }
    public string? OperationId { get; set; }
    public string? ExceptionStackTrace { get; set; }
    public string? ExceptionType { get; set; }
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}
