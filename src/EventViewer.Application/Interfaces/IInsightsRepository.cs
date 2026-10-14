using EventViewer.Domain.Entities;

namespace EventViewer.Application.Interfaces;

/// <summary>
/// Contract for the repository that retrieves Application Insights logs
/// </summary>
public interface IInsightsRepository
{
    /// <summary>
    /// Gets Application Insights logs based on a CorrelationId
    /// </summary>
    /// <param name="correlationId">Correlation ID</param>
    /// <param name="startDate">Optional start date</param>
    /// <param name="endDate">Optional end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of correlated logs</returns>
    Task<IEnumerable<ApplicationInsightsLog>> GetLogsByCorrelationIdAsync(
        string correlationId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets Application Insights logs for multiple CorrelationIds
    /// </summary>
    /// <param name="correlationIds">List of correlation IDs</param>
    /// <param name="startDate">Optional start date</param>
    /// <param name="endDate">Optional end date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary with CorrelationId as key and list of logs as value</returns>
    Task<Dictionary<string, List<ApplicationInsightsLog>>> GetLogsByCorrelationIdsAsync(
        IEnumerable<string> correlationIds,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}
