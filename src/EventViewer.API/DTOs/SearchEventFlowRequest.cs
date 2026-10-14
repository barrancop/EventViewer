using EventViewer.Domain.Enums;

namespace EventViewer.API.DTOs;

/// <summary>
/// Request DTO for searching event flows
/// </summary>
public class SearchEventFlowRequest
{
    /// <summary>
    /// Search range start date
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// Search range end date
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Specific correlation ID
    /// </summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// Event action (Publishing/Subscribing)
    /// </summary>
    public EventAction? Action { get; set; }
    
    /// <summary>
    /// User ID
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// Event name
    /// </summary>
    public string? EventName { get; set; }
    
    /// <summary>
    /// List of microservice names to filter
    /// </summary>
    public List<string>? Microservices { get; set; }
    
    /// <summary>
    /// Event status (Processed/Processing/Failed)
    /// </summary>
    public EventStatus? Status { get; set; }
    
    /// <summary>
    /// Business key linked to the event (e.g. an entity ID); exact match
    /// </summary>
    public string? Keyword { get; set; }
    
    /// <summary>
    /// Indicates whether to include Application Insights logs
    /// </summary>
    public bool IncludeInsightsLogs { get; set; } = true;
    
    /// <summary>
    /// Indicates whether to save results to JSON
    /// </summary>
    public bool SaveResults { get; set; } = false;
}
