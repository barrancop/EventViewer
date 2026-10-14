using EventViewer.Application.Diagnostics;
using EventViewer.Application.Interfaces;
using EventViewer.Application.Services;
using EventViewer.Domain.Entities;
using EventViewer.Domain.ValueObjects;
using System.Diagnostics;

namespace EventViewer.Application.UseCases;

/// <summary>
/// Main use case for searching and building event flows
/// </summary>
public class SearchEventFlowUseCase
{
    private readonly IEventRepository _eventRepository;
    private readonly IInsightsRepository _insightsRepository;
    private readonly IEventFlowPersistence _eventFlowPersistence;
    private readonly EventFlowBuilder _eventFlowBuilder;
    
    public SearchEventFlowUseCase(
        IEventRepository eventRepository,
        IInsightsRepository insightsRepository,
        IEventFlowPersistence eventFlowPersistence,
        EventFlowBuilder eventFlowBuilder)
    {
        _eventRepository = eventRepository;
        _insightsRepository = insightsRepository;
        _eventFlowPersistence = eventFlowPersistence;
        _eventFlowBuilder = eventFlowBuilder;
    }
    
    /// <summary>
    /// Executes the event flow search based on specified criteria
    /// </summary>
    /// <param name="criteria">Search criteria</param>
    /// <param name="includeInsightsLogs">Indicates whether to include Application Insights logs</param>
    /// <param name="saveResults">Indicates whether to save results to JSON</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search result with found flows</returns>
    public async Task<SearchEventFlowResult> ExecuteAsync(
        EventSearchCriteria criteria,
        bool includeInsightsLogs = true,
        bool saveResults = false,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.EventViewer.StartActivity("SearchEventFlow");
        activity?.SetTag("includeInsightsLogs", includeInsightsLogs);
        activity?.SetTag("saveResults", saveResults);
        
        // Validate criteria
        if (!criteria.IsValid(out var validationMessage))
        {
            activity?.SetStatus(ActivityStatusCode.Error, validationMessage);
            return SearchEventFlowResult.Failed(validationMessage);
        }
        
        // Search for events
        IEnumerable<ServiceBusEvent> events;
        using (var searchActivity = ActivitySources.EventViewer.StartActivity("SearchEvents"))
        {
            searchActivity?.SetTag("correlationId", criteria.CorrelationId);
            searchActivity?.SetTag("eventName", criteria.EventName);
            
            events = await _eventRepository.SearchEventsAsync(criteria, cancellationToken);
            
            if (!events.Any())
            {
                activity?.SetTag("eventsFound", 0);
                return SearchEventFlowResult.Success(
                    Array.Empty<EventFlow>(), 
                    "No events found with the specified criteria");
            }
            
            searchActivity?.SetTag("eventsFound", events.Count());
        }
        
        // Build flows grouped by CorrelationId
        List<EventFlow> flows;
        using (var buildActivity = ActivitySources.EventViewer.StartActivity("BuildEventFlows"))
        {
            flows = _eventFlowBuilder.BuildMultipleFlows(events).ToList();
            buildActivity?.SetTag("flowsBuilt", flows.Count);
            activity?.SetTag("flowsBuilt", flows.Count);
        }
        
        // If Insights logs are requested, fetch them
        if (includeInsightsLogs)
        {
            using var insightsActivity = ActivitySources.EventViewer.StartActivity("EnrichWithInsightsLogs");
            await EnrichFlowsWithInsightsLogsAsync(flows, cancellationToken);
        }
        
        // Save results if requested
        string? savedPath = null;
        if (saveResults)
        {
            using var saveActivity = ActivitySources.EventViewer.StartActivity("SaveEventFlows");
            var savedPaths = await _eventFlowPersistence.SaveEventFlowsAsync(flows, cancellationToken);
            savedPath = string.Join(", ", savedPaths);
            saveActivity?.SetTag("savedPath", savedPath);
        }
        
        activity?.SetStatus(ActivityStatusCode.Ok);
        return SearchEventFlowResult.Success(
            flows, 
            $"Found {flows.Count} event flow(s)",
            savedPath);
    }
    
    /// <summary>
    /// Searches for a specific flow by CorrelationId
    /// </summary>
    /// <param name="correlationId">Correlation ID</param>
    /// <param name="includeInsightsLogs">Indicates whether to include Application Insights logs</param>
    /// <param name="saveResults">Indicates whether to save results to JSON</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Found event flow</returns>
    public async Task<SearchEventFlowResult> GetFlowByCorrelationIdAsync(
        string correlationId,
        bool includeInsightsLogs = true,
        bool saveResults = false,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.EventViewer.StartActivity("GetFlowByCorrelationId");
        activity?.SetTag("correlationId", correlationId);
        activity?.SetTag("includeInsightsLogs", includeInsightsLogs);
        activity?.SetTag("saveResults", saveResults);
        
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            activity?.SetStatus(ActivityStatusCode.Error, "CorrelationId cannot be empty");
            return SearchEventFlowResult.Failed("CorrelationId cannot be empty");
        }
        
        // Get events by CorrelationId
        EventFlow flow;
        using (var getEventsActivity = ActivitySources.EventViewer.StartActivity("GetEventsByCorrelationId"))
        {
            var events = await _eventRepository.GetEventsByCorrelationIdAsync(correlationId, cancellationToken);
            
            if (!events.Any())
            {
                activity?.SetTag("eventsFound", 0);
                return SearchEventFlowResult.Success(
                    Array.Empty<EventFlow>(), 
                    $"No events found with CorrelationId: {correlationId}");
            }
            
            getEventsActivity?.SetTag("eventsFound", events.Count());
            
            // Build the flow
            flow = _eventFlowBuilder.BuildFlow(events);
        }
        
        // Enrich with Insights logs if requested
        if (includeInsightsLogs)
        {
            using var insightsActivity = ActivitySources.EventViewer.StartActivity("GetInsightsLogs");
            var logs = await _insightsRepository.GetLogsByCorrelationIdAsync(
                correlationId, 
                flow.StartTime, 
                flow.EndTime, 
                cancellationToken);
            
            flow.InsightsLogs = logs.ToList();
            insightsActivity?.SetTag("logsFound", flow.InsightsLogs.Count);
        }
        
        // Save results if requested
        string? savedPath = null;
        if (saveResults)
        {
            using var saveActivity = ActivitySources.EventViewer.StartActivity("SaveEventFlow");
            savedPath = await _eventFlowPersistence.SaveEventFlowAsync(flow, cancellationToken);
            saveActivity?.SetTag("savedPath", savedPath);
        }
        
        activity?.SetStatus(ActivityStatusCode.Ok);
        return SearchEventFlowResult.Success(
            new[] { flow }, 
            $"Found flow with CorrelationId: {correlationId}",
            savedPath);
    }
    
    /// <summary>
    /// Enriches flows with Application Insights logs
    /// </summary>
    private async Task EnrichFlowsWithInsightsLogsAsync(
        List<EventFlow> flows,
        CancellationToken cancellationToken)
    {
        var correlationIds = flows.Select(f => f.CorrelationId).ToList();
        
        var logsDict = await _insightsRepository.GetLogsByCorrelationIdsAsync(
            correlationIds,
            flows.Min(f => f.StartTime),
            flows.Max(f => f.EndTime ?? f.StartTime),
            cancellationToken);
        
        foreach (var flow in flows)
        {
            if (logsDict.TryGetValue(flow.CorrelationId, out var logs))
            {
                flow.InsightsLogs = logs;
            }
        }
    }
}

/// <summary>
/// Event flow search result
/// </summary>
public class SearchEventFlowResult
{
    public bool IsSuccess { get; init; }
    public IEnumerable<EventFlow> Flows { get; init; } = Array.Empty<EventFlow>();
    public string Message { get; init; } = string.Empty;
    public string? SavedFilePath { get; init; }
    
    public static SearchEventFlowResult Success(
        IEnumerable<EventFlow> flows, 
        string message,
        string? savedFilePath = null)
    {
        return new SearchEventFlowResult
        {
            IsSuccess = true,
            Flows = flows,
            Message = message,
            SavedFilePath = savedFilePath
        };
    }
    
    public static SearchEventFlowResult Failed(string message)
    {
        return new SearchEventFlowResult
        {
            IsSuccess = false,
            Message = message
        };
    }
}
