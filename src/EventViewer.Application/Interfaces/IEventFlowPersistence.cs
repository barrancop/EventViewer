using EventViewer.Domain.Entities;

namespace EventViewer.Application.Interfaces;

/// <summary>
/// Contract for persisting event flows in JSON format
/// </summary>
public interface IEventFlowPersistence
{
    /// <summary>
    /// Saves an event flow in JSON format
    /// </summary>
    /// <param name="eventFlow">Event flow to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Saved file name</returns>
    Task<string> SaveEventFlowAsync(
        EventFlow eventFlow, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves multiple event flows in JSON format
    /// </summary>
    /// <param name="eventFlows">List of event flows</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of saved file names</returns>
    Task<IEnumerable<string>> SaveEventFlowsAsync(
        IEnumerable<EventFlow> eventFlows, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reads an event flow from a JSON file
    /// </summary>
    /// <param name="fileName">Saved flow file name, as returned by ListSavedFlowsAsync</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized event flow</returns>
    Task<EventFlow?> LoadEventFlowAsync(
        string fileName, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lists all saved event flows
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Saved flow file names, newest first</returns>
    Task<IEnumerable<string>> ListSavedFlowsAsync(
        CancellationToken cancellationToken = default);
}
