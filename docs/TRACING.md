# OpenTelemetry Tracing - EventViewer

## Overview

Distributed tracing has been added to the EventViewer application using **OpenTelemetry** to provide observability into event flow operations.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     EventViewer.API                         │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  ASP.NET Core Instrumentation (automatic)            │  │
│  │  - HTTP requests                                      │  │
│  │  - Middleware pipeline                                │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  HttpClient Instrumentation (automatic)              │  │
│  │  - Outgoing HTTP calls to REST API                   │  │
│  │  - Application Insights API calls                    │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              EventViewer.Application                        │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Custom ActivitySource: "EventViewer.Application"    │  │
│  │  - SearchEventFlow                                    │  │
│  │  - SearchEvents                                       │  │
│  │  - BuildEventFlows                                    │  │
│  │  - EnrichWithInsightsLogs                            │  │
│  │  - SaveEventFlows                                     │  │
│  │  - GetFlowByCorrelationId                            │  │
│  │  - GetEventsByCorrelationId                          │  │
│  │  - GetInsightsLogs                                    │  │
│  │  - SaveEventFlow                                      │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   OTLP Exporter                             │
│              http://localhost:4318/v1/traces                │
│                                                             │
│          AI Toolkit Trace Viewer (VS Code)                  │
└─────────────────────────────────────────────────────────────┘
```

## Components

### 1. API Layer (`Program.cs`)

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddSource("EventViewer.Application")
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(otlpEndpoint);
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        }));
```

**Features**:
- Service name configured from `appsettings.json`
- OTLP endpoint configurable (default: `http://localhost:4318/v1/traces`); with HTTP/protobuf the URL must include the `/v1/traces` path
- Records exceptions automatically
- Listens to custom ActivitySource

### 2. Application Layer (`ActivitySources.cs`)

```csharp
public static class ActivitySources
{
    public static readonly ActivitySource EventViewer = new("EventViewer.Application", "1.0.0");
}
```

Central location for all ActivitySources used across the application.

### 3. Use Cases (`SearchEventFlowUseCase.cs`)

Custom spans with tags:

#### `SearchEventFlow` Span
Main operation span that encompasses the entire search workflow.

**Tags**:
- `includeInsightsLogs`: Boolean
- `saveResults`: Boolean
- `flowsBuilt`: Number of flows constructed
- `status`: Ok or Error

#### `SearchEvents` Span
Queries the event repository.

**Tags**:
- `correlationId`: Correlation ID (if provided)
- `eventName`: Event name filter (if provided)
- `eventsFound`: Number of events retrieved

#### `BuildEventFlows` Span
Constructs event flow graphs.

**Tags**:
- `flowsBuilt`: Number of flows created

#### `EnrichWithInsightsLogs` Span
Fetches Application Insights logs.

**Tags**:
- None (inherits context)

#### `SaveEventFlows` Span
Persists flows to JSON files.

**Tags**:
- `savedPath`: File paths where flows were saved

#### `GetFlowByCorrelationId` Span
Retrieves a specific flow by correlation ID.

**Tags**:
- `correlationId`: The correlation ID being searched
- `includeInsightsLogs`: Boolean
- `saveResults`: Boolean
- `eventsFound`: Number of events found

#### `GetInsightsLogs` Span
Fetches logs from Application Insights for a specific flow.

**Tags**:
- `logsFound`: Number of logs retrieved

## Configuration

### appsettings.json

```json
{
  "OpenTelemetry": {
    "ServiceName": "EventViewer.API",
    "OtlpEndpoint": "http://localhost:4318/v1/traces"
  }
}
```

### Environment Variables (Alternative)

The exporter endpoint is set from configuration, so use the .NET configuration keys (`OTEL_EXPORTER_OTLP_*` variables are overridden):

```powershell
$env:OpenTelemetry__ServiceName = "EventViewer.API"
$env:OpenTelemetry__OtlpEndpoint = "http://localhost:4318/v1/traces"
```

## Usage

### Starting the Trace Collector

In VS Code:
1. Open Command Palette (`Ctrl+Shift+P`)
2. Run: `AI Toolkit: Open Trace Viewer`
3. The OTLP collector will start on `http://localhost:4318`

### Running the Application

```powershell
dotnet run --project src/EventViewer.API
```

### Viewing Traces

1. Execute API operations (search events, get flows)
2. Open the AI Toolkit trace viewer in VS Code
3. View the distributed trace with all spans:
   - HTTP request span (ASP.NET Core)
   - Custom business logic spans (Application)
   - HTTP client spans (outgoing calls)

### Trace Example

```
SearchEventFlow (5.2s)
├─ SearchEvents (2.1s)
│  └─ HTTP POST /api/gateway/search/searchByIntegrationEventLog/ (2.0s)
├─ BuildEventFlows (0.5s)
├─ EnrichWithInsightsLogs (2.4s)
│  └─ HTTP POST https://api.loganalytics.io/v1/workspaces/{id}/query (2.3s)
└─ SaveEventFlows (0.2s)
```

## Benefits

1. **End-to-End Visibility**: See the complete request flow from API to repositories
2. **Performance Monitoring**: Identify bottlenecks in event search and Insights queries
3. **Error Tracking**: Exceptions are recorded with full context
4. **Debugging**: Understand the execution path of complex multi-step operations
5. **Correlation**: Tags like `correlationId` help trace specific event flows
6. **Metrics**: Count operations (eventsFound, flowsBuilt, logsFound)

## Dependencies

```xml
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.19.1" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.19.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.19.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.19.0" />
```

## Production Considerations

For production deployments, consider:

1. **Export to APM**: Configure OTLP exporter to send to Azure Monitor, Jaeger, Zipkin, etc.
2. **Sampling**: Implement sampling strategies for high-volume scenarios
3. **Security**: Use authenticated endpoints for trace data
4. **Performance**: Monitor overhead of tracing instrumentation

### Example: Export to Azure Monitor

```csharp
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("https://your-apm-endpoint/v1/traces");
    options.Headers = "Authorization=Bearer YOUR_TOKEN";
})
```

## Best Practices

1. ✅ Use `using` statements for spans to ensure proper disposal
2. ✅ Add meaningful tags to spans for filtering and analysis
3. ✅ Set span status (Ok/Error) explicitly
4. ✅ Keep span names concise and descriptive
5. ✅ Don't create too many nested spans (impacts performance)
6. ✅ Use automatic instrumentation where available
7. ✅ Record exceptions with full context

## Troubleshooting

### Traces not appearing

1. Verify OTLP collector is running (`http://localhost:4318`)
2. Check that `OpenTelemetry:OtlpEndpoint` includes the `/v1/traces` path
3. Ensure ActivitySource name matches: `"EventViewer.Application"`
4. Verify OpenTelemetry packages are restored

### Performance impact

- Tracing adds minimal overhead (<1% in most cases)
- Use sampling in high-volume scenarios
- Monitor memory usage if keeping spans for extended periods

## Next Steps

- [ ] Add tracing to Infrastructure layer (repositories)
- [ ] Implement custom metrics alongside traces
- [ ] Add trace correlation with Application Insights logs
- [ ] Configure sampling strategies for production
- [ ] Export traces to centralized APM platform
