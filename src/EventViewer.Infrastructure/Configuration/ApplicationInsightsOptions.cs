namespace EventViewer.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Application Insights
/// </summary>
public class ApplicationInsightsOptions
{
    public const string SectionName = "ApplicationInsights";
    
    /// <summary>
    /// ID of the Log Analytics workspace linked to Application Insights
    /// </summary>
    public string WorkspaceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Server-side query timeout in seconds
    /// </summary>
    public int QueryTimeoutSeconds { get; set; } = 60;
    
    /// <summary>
    /// Maximum number of results per query
    /// </summary>
    public int MaxResults { get; set; } = 10000;

    /// <summary>
    /// Minutes searched before the first event of a flow (clock skew between services)
    /// </summary>
    public int LogsWindowMinutesBefore { get; set; } = 1;

    /// <summary>
    /// Minutes searched after the last event of a flow (handlers keep logging after it is created)
    /// </summary>
    public int LogsWindowMinutesAfter { get; set; } = 10;
}
