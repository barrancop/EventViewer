using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EventViewer.Application.Interfaces;
using EventViewer.Domain.Entities;
using EventViewer.Domain.Enums;
using EventViewer.Domain.ValueObjects;
using EventViewer.Infrastructure.Authentication;
using EventViewer.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace EventViewer.Infrastructure.Repositories;

/// <summary>
/// Event repository backed by the gateway's integration event log search endpoint
/// </summary>
public class RestApiEventRepository : IEventRepository
{
    private const string SearchPath = "api/gateway/search/searchByIntegrationEventLog/";
    private const int PageSize = 100;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly CultureInfo GatewayCulture = CultureInfo.GetCultureInfo("en-US");

    private readonly HttpClient _httpClient;
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly EventApiOptions _options;

    public RestApiEventRepository(
        HttpClient httpClient,
        IAccessTokenProvider tokenProvider,
        IOptions<EventApiOptions> options)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _options = options.Value;
    }

    public async Task<IEnumerable<ServiceBusEvent>> SearchEventsAsync(
        EventSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<GatewayFilter>();

        if (criteria.StartDate.HasValue)
            filters.Add(new GatewayFilter("createdDateFrom", FormatDate(criteria.StartDate.Value)));

        if (criteria.EndDate.HasValue)
            filters.Add(new GatewayFilter("createdDateTo", FormatDate(criteria.EndDate.Value)));

        if (!string.IsNullOrWhiteSpace(criteria.CorrelationId))
            filters.Add(new GatewayFilter("CorrelationId", criteria.CorrelationId));

        if (criteria.Action.HasValue)
            filters.Add(new GatewayFilter("Action", criteria.Action.Value.ToString()));

        if (!string.IsNullOrWhiteSpace(criteria.UserId))
            filters.Add(new GatewayFilter("UserId", criteria.UserId));

        if (!string.IsNullOrWhiteSpace(criteria.EventName))
            filters.Add(new GatewayFilter("EventTypeName", criteria.EventName));

        if (criteria.Status.HasValue)
            filters.Add(new GatewayFilter("State", ToGatewayState(criteria.Status.Value)));

        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
            filters.Add(new GatewayFilter("keys", criteria.Keyword));

        // The gateway takes a single ServiceName; several microservices are filtered locally
        if (criteria.Microservices is { Count: 1 })
            filters.Add(new GatewayFilter("ServiceName", criteria.Microservices[0]));

        var logs = await FetchAllPagesAsync(filters, cancellationToken);

        return logs
            .Select(MapToEntity)
            .Where(evt => MatchesLocalCriteria(evt, criteria))
            .ToList();
    }

    public Task<IEnumerable<ServiceBusEvent>> GetEventsByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return SearchEventsAsync(new EventSearchCriteria { CorrelationId = correlationId }, cancellationToken);
    }

    private async Task<List<IntegrationEventLogDto>> FetchAllPagesAsync(
        IReadOnlyList<GatewayFilter> filters,
        CancellationToken cancellationToken)
    {
        var results = new List<IntegrationEventLogDto>();

        for (var page = 1; results.Count < _options.MaxResults; page++)
        {
            var request = new GatewaySearchRequest(new GatewayPaging(page, PageSize), filters);
            var data = await FetchPageAsync(request, cancellationToken);
            results.AddRange(data.Results);

            if (data.Results.Count < PageSize || results.Count >= data.TotalResults)
                break;
        }

        return results.Take(_options.MaxResults).ToList();
    }

    private async Task<GatewaySearchData> FetchPageAsync(
        GatewaySearchRequest body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, SearchPath)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException($"The events API returned {(int)response.StatusCode} ({response.StatusCode})");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"The events API returned {(int)response.StatusCode} ({response.StatusCode}): {error[..Math.Min(error.Length, 500)]}");
        }

        var result = await response.Content.ReadFromJsonAsync<GatewaySearchResponse>(JsonOptions, cancellationToken);

        if (result is not { Success: true, Data: not null })
            throw new InvalidOperationException($"The events API reported an error: {result?.Errors}");

        return result.Data;
    }

    private static bool MatchesLocalCriteria(ServiceBusEvent evt, EventSearchCriteria criteria) =>
        criteria.Microservices is not { Count: > 1 }
        || criteria.Microservices.Contains(evt.Metadata.MicroserviceName, StringComparer.OrdinalIgnoreCase);

    private static ServiceBusEvent MapToEntity(IntegrationEventLogDto dto) => new()
    {
        EventId = dto.Id,
        CorrelationId = dto.CorrelationId,
        EventName = dto.EventTypeName,
        // en-US text without offset; observed values are UTC
        Timestamp = DateTime.Parse(dto.CreationDate, GatewayCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
        Action = Enum.Parse<EventAction>(dto.Action, ignoreCase: true),
        Status = ToEventStatus(dto.State),
        Metadata = new EventMetadata
        {
            MicroserviceName = dto.ServiceName,
            UserId = dto.UserId
        },
        ErrorMessage = string.IsNullOrWhiteSpace(dto.LastException) ? null : dto.LastException
    };

    private static EventStatus ToEventStatus(string state) => state switch
    {
        "Processing" => EventStatus.Processing,
        "Processed" => EventStatus.Processed,
        "ProcessedFailed" => EventStatus.Failed,
        _ => throw new InvalidOperationException($"Unknown event state returned by the events API: '{state}'")
    };

    private static string ToGatewayState(EventStatus status) => status switch
    {
        EventStatus.Processing => "Processing",
        EventStatus.Processed => "Processed",
        EventStatus.Failed => "ProcessedFailed",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    private static DateTime ToUtc(DateTime date) =>
        date.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : date.ToUniversalTime();

    private static string FormatDate(DateTime date) =>
        ToUtc(date).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
}

// Gateway search contract (POST api/gateway/search/searchByIntegrationEventLog/)
internal sealed record GatewaySearchRequest(GatewayPaging Paging, IReadOnlyList<GatewayFilter> Filters)
{
    public object[] Sorts { get; } = [];
}

internal sealed record GatewayPaging(int Number, int Size);

internal sealed record GatewayFilter(string By, string Value, string Type = "eq");

internal sealed class GatewaySearchResponse
{
    public GatewaySearchData? Data { get; set; }
    public JsonElement? Errors { get; set; }
    public bool Success { get; set; }
}

internal sealed class GatewaySearchData
{
    public List<IntegrationEventLogDto> Results { get; set; } = new();
    public int TotalResults { get; set; }
}

internal sealed class IntegrationEventLogDto
{
    public string Id { get; set; } = string.Empty;
    public string CreationDate { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EventTypeName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? LastException { get; set; }
}
