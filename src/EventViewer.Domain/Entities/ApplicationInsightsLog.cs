namespace EventViewer.Domain.Entities;

/// <summary>
/// Represents an Application Insights log correlated with events
/// </summary>
public class ApplicationInsightsLog
{
    /// <summary>
    /// Unique log identifier
    /// </summary>
    public string LogId { get; set; } = string.Empty;
    
    /// <summary>
    /// Correlation ID that links to events
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
    
    /// <summary>
    /// Log date and time
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Severity level (Information, Warning, Error, Critical)
    /// </summary>
    public string SeverityLevel { get; set; } = string.Empty;
    
    /// <summary>
    /// Log message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the microservice that generated the log
    /// </summary>
    public string MicroserviceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Operation name in Application Insights
    /// </summary>
    public string? OperationName { get; set; }
    
    /// <summary>
    /// Operation ID in Application Insights
    /// </summary>
    public string? OperationId { get; set; }
    
    /// <summary>
    /// Exception stack trace (if an error)
    /// </summary>
    public string? ExceptionStackTrace { get; set; }
    
    /// <summary>
    /// Exception type (if an error)
    /// </summary>
    public string? ExceptionType { get; set; }
    
    /// <summary>
    /// Additional custom properties of the log
    /// </summary>
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}
