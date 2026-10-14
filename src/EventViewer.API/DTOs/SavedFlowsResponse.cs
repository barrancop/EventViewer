namespace EventViewer.API.DTOs;

/// <summary>
/// Response DTO for listing saved flows
/// </summary>
public class SavedFlowsResponse
{
    /// <summary>
    /// List of saved flow file paths
    /// </summary>
    public List<string> FilePaths { get; set; } = new();
    
    /// <summary>
    /// Total number of saved flows
    /// </summary>
    public int Count { get; set; }
}
