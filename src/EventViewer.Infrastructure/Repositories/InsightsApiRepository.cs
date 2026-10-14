using Azure.Core;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using EventViewer.Application.Interfaces;
using EventViewer.Domain.Entities;
using EventViewer.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace EventViewer.Infrastructure.Repositories;

/// <summary>
/// Application Insights repository implementation using Azure Monitor Query
/// </summary>
public class InsightsApiRepository : IInsightsRepository
{
    private readonly LogsQueryClient _logsClient;
    private readonly ApplicationInsightsOptions _options;
    private readonly LogsQueryOptions _queryOptions;

    public InsightsApiRepository(IOptions<ApplicationInsightsOptions> options)
    {
        _options = options.Value;
        
        _queryOptions = new LogsQueryOptions
        {
            ServerTimeout = TimeSpan.FromSeconds(_options.QueryTimeoutSeconds)
        };

        // DefaultAzureCredential tries environment variables, managed identity, Visual Studio, Azure CLI, etc.
        _logsClient = new LogsQueryClient(new DefaultAzureCredential());
    }

    public async Task<IEnumerable<ApplicationInsightsLog>> GetLogsByCorrelationIdAsync(
        string correlationId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildKustoQuery(new[] { correlationId });
        var timeRange = BuildTimeRange(startDate, endDate);

        try
        {
            var response = await _logsClient.QueryWorkspaceAsync(
                _options.WorkspaceId,
                query,
                timeRange,
                _queryOptions,
                cancellationToken);

            return ParseLogsFromResponse(response.Value);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error querying Application Insights: {ex.Message}", ex);
        }
    }

    public async Task<Dictionary<string, List<ApplicationInsightsLog>>> GetLogsByCorrelationIdsAsync(
        IEnumerable<string> correlationIds,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, List<ApplicationInsightsLog>>();

        var correlationIdList = correlationIds.ToList();
        var query = BuildKustoQuery(correlationIdList);
        var timeRange = BuildTimeRange(startDate, endDate);

        try
        {
            var response = await _logsClient.QueryWorkspaceAsync(
                _options.WorkspaceId,
                query,
                timeRange,
                _queryOptions,
                cancellationToken);

            var logs = ParseLogsFromResponse(response.Value);

            // Group by CorrelationId
            foreach (var log in logs)
            {
                if (!result.ContainsKey(log.CorrelationId))
                {
                    result[log.CorrelationId] = new List<ApplicationInsightsLog>();
                }
                result[log.CorrelationId].Add(log);
            }

            // Ensure every requested CorrelationId has an entry, even if empty
            foreach (var correlationId in correlationIdList)
            {
                if (!result.ContainsKey(correlationId))
                {
                    result[correlationId] = new List<ApplicationInsightsLog>();
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error querying Application Insights for multiple CorrelationIds: {ex.Message}", ex);
        }
    }

    private string BuildKustoQuery(IEnumerable<string> correlationIds)
    {
        // CreateQuery quotes and escapes each value, preventing KQL injection
        var ids = string.Join(", ", correlationIds.Select(id => LogsQueryClient.CreateQuery($"{id}")));

        // Workspace-based schema (AppTraces, Properties, OperationId...), not the classic one (traces, customDimensions...)
        return $"""
            union AppTraces, AppExceptions, AppRequests, AppDependencies
            | extend correlationId = coalesce(tostring(Properties.CorrelationId), OperationId)
            | where correlationId in ({ids}) or OperationId in ({ids})
            | project
                timestamp = TimeGenerated,
                severityLevel = case(
                    SeverityLevel == 0, "Verbose",
                    SeverityLevel == 1, "Information",
                    SeverityLevel == 2, "Warning",
                    SeverityLevel == 3, "Error",
                    SeverityLevel == 4, "Critical",
                    Success == false, "Error",
                    "Information"),
                message = coalesce(Message, OuterMessage, Name),
                correlationId,
                microserviceName = AppRoleName,
                operationName = OperationName,
                operationId = OperationId,
                exceptionType = ExceptionType,
                exceptionStackTrace = tostring(Details),
                customProperties = tostring(Properties)
            | order by timestamp asc
            | limit {_options.MaxResults}
            """;
    }

    private QueryTimeRange BuildTimeRange(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue)
        {
            return new QueryTimeRange(
                startDate.Value.AddMinutes(-_options.LogsWindowMinutesBefore),
                endDate.Value.AddMinutes(_options.LogsWindowMinutesAfter));
        }
        
        if (startDate.HasValue)
        {
            return new QueryTimeRange(startDate.Value.AddMinutes(-_options.LogsWindowMinutesBefore), DateTime.UtcNow);
        }

        // Default: last 24 hours
        return new QueryTimeRange(TimeSpan.FromHours(24));
    }

    private List<ApplicationInsightsLog> ParseLogsFromResponse(LogsQueryResult result)
    {
        var logs = new List<ApplicationInsightsLog>();

        foreach (var row in result.Table.Rows)
        {
            var log = new ApplicationInsightsLog
            {
                LogId = Guid.NewGuid().ToString(),
                Timestamp = row.GetDateTimeOffset("timestamp")?.UtcDateTime ?? DateTime.UtcNow,
                SeverityLevel = row.GetString("severityLevel") ?? "Information",
                Message = row.GetString("message") ?? string.Empty,
                CorrelationId = row.GetString("correlationId") ?? string.Empty,
                MicroserviceName = row.GetString("microserviceName") ?? string.Empty,
                OperationName = row.GetString("operationName"),
                OperationId = row.GetString("operationId"),
                ExceptionType = row.GetString("exceptionType"),
                ExceptionStackTrace = row.GetString("exceptionStackTrace"),
                CustomProperties = new Dictionary<string, string>()
            };

            // Parse customProperties if present
            var customPropsValue = row.GetString("customProperties");
            if (!string.IsNullOrWhiteSpace(customPropsValue))
            {
                log.CustomProperties = ParseCustomProperties(customPropsValue);
            }

            logs.Add(log);
        }

        return logs;
    }

    private Dictionary<string, string> ParseCustomProperties(string? jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
            return new Dictionary<string, string>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString) 
                   ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
}
