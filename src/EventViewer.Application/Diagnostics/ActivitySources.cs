using System.Diagnostics;

namespace EventViewer.Application.Diagnostics;

/// <summary>
/// Central location for all ActivitySources used in the application for distributed tracing
/// </summary>
public static class ActivitySources
{
    /// <summary>
    /// ActivitySource for EventViewer application operations
    /// </summary>
    public static readonly ActivitySource EventViewer = new("EventViewer.Application", "1.0.0");
}
