namespace EventViewer.Infrastructure.Configuration;

/// <summary>
/// Configuration options for JSON persistence
/// </summary>
public class JsonPersistenceOptions
{
    public const string SectionName = "JsonPersistence";
    
    /// <summary>
    /// Base path where JSON files are stored
    /// </summary>
    public string StoragePath { get; set; } = "./EventFlows";
    
    /// <summary>
    /// Indicates whether to create the folder automatically if it does not exist
    /// </summary>
    public bool CreateDirectoryIfNotExists { get; set; } = true;
    
    /// <summary>
    /// Indicates whether to use indented JSON (pretty print)
    /// </summary>
    public bool UseIndentedJson { get; set; } = true;
    
    /// <summary>
    /// File name prefix
    /// </summary>
    public string FileNamePrefix { get; set; } = "EventFlow_";
    
    /// <summary>
    /// File extension
    /// </summary>
    public string FileExtension { get; set; } = ".json";
}
