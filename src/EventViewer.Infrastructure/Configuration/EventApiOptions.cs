namespace EventViewer.Infrastructure.Configuration;

/// <summary>
/// Configuration options for the events REST API
/// </summary>
public class EventApiOptions
{
    public const string SectionName = "EventApi";
    
    /// <summary>
    /// Base URL (host root) of the events gateway
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Timeout in seconds for HTTP requests
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Maximum number of events retrieved per search (the gateway is paged)
    /// </summary>
    public int MaxResults { get; set; } = 1000;
}
