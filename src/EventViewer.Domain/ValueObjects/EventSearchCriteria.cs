using EventViewer.Domain.Enums;

namespace EventViewer.Domain.ValueObjects;

/// <summary>
/// Value Object representing event search criteria
/// </summary>
public class EventSearchCriteria
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
    /// Validates that the search criteria are correct
    /// </summary>
    public bool IsValid(out string validationMessage)
    {
        if (StartDate.HasValue && EndDate.HasValue && StartDate > EndDate)
        {
            validationMessage = "Start date cannot be greater than end date";
            return false;
        }
        
        if (!StartDate.HasValue && !EndDate.HasValue && string.IsNullOrWhiteSpace(CorrelationId))
        {
            validationMessage = "You must provide at least a date range or a CorrelationId";
            return false;
        }
        
        validationMessage = string.Empty;
        return true;
    }
}
