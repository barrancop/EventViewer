using EventViewer.API.DTOs;
using EventViewer.Domain.Entities;
using EventViewer.Domain.ValueObjects;

namespace EventViewer.API.Mappers;

/// <summary>
/// Maps domain entities to DTOs and vice versa
/// </summary>
public static class EventFlowMapper
{
    /// <summary>
    /// Maps SearchEventFlowRequest to EventSearchCriteria
    /// </summary>
    public static EventSearchCriteria ToSearchCriteria(SearchEventFlowRequest request)
    {
        return new EventSearchCriteria
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CorrelationId = request.CorrelationId,
            Action = request.Action,
            UserId = request.UserId,
            EventName = request.EventName,
            Microservices = request.Microservices,
            Status = request.Status,
            Keyword = request.Keyword
        };
    }
    
    /// <summary>
    /// Maps EventFlow to EventFlowDto
    /// </summary>
    public static EventFlowDto ToDto(EventFlow flow)
    {
        return new EventFlowDto
        {
            CorrelationId = flow.CorrelationId,
            StartTime = flow.StartTime,
            EndTime = flow.EndTime,
            Events = flow.Events.Select(ToDto).ToList(),
            InsightsLogs = flow.InsightsLogs.Select(ToDto).ToList(),
            DurationMs = flow.DurationMs,
            HasFailures = flow.HasFailures,
            MicroservicesCount = flow.MicroservicesCount
        };
    }
    
    /// <summary>
    /// Maps ServiceBusEvent to ServiceBusEventDto
    /// </summary>
    public static ServiceBusEventDto ToDto(ServiceBusEvent evt)
    {
        return new ServiceBusEventDto
        {
            EventId = evt.EventId,
            CorrelationId = evt.CorrelationId,
            EventName = evt.EventName,
            Timestamp = evt.Timestamp,
            Action = evt.Action.ToString(),
            Status = evt.Status.ToString(),
            Metadata = ToDto(evt.Metadata),
            Payload = evt.Payload,
            ErrorMessage = evt.ErrorMessage,
            ChildEventIds = evt.ChildEventIds
        };
    }
    
    /// <summary>
    /// Maps EventMetadata to EventMetadataDto
    /// </summary>
    public static EventMetadataDto ToDto(EventMetadata metadata)
    {
        return new EventMetadataDto
        {
            MicroserviceName = metadata.MicroserviceName,
            TopicName = metadata.TopicName,
            SubscriptionName = metadata.SubscriptionName,
            RuleName = metadata.RuleName,
            UserId = metadata.UserId
        };
    }
    
    /// <summary>
    /// Maps ApplicationInsightsLog to ApplicationInsightsLogDto
    /// </summary>
    public static ApplicationInsightsLogDto ToDto(ApplicationInsightsLog log)
    {
        return new ApplicationInsightsLogDto
        {
            LogId = log.LogId,
            CorrelationId = log.CorrelationId,
            Timestamp = log.Timestamp,
            SeverityLevel = log.SeverityLevel,
            Message = log.Message,
            MicroserviceName = log.MicroserviceName,
            OperationName = log.OperationName,
            OperationId = log.OperationId,
            ExceptionStackTrace = log.ExceptionStackTrace,
            ExceptionType = log.ExceptionType,
            CustomProperties = log.CustomProperties
        };
    }
}
