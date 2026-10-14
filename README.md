# EventViewer - Microservices Event Tracking System

.NET 10 backend for tracking and visualizing event flows between microservices that communicate through Azure Service Bus, enriched with Application Insights logs.

## 🎯 Objective

Facilitate tracking of event chains between microservices, correlating Service Bus events with Application Insights logs for debugging and analysis.

## 🏗️ Architecture

The project follows **Clean Architecture** with 4 layers:

```
EventViewer/
├── src/
│   ├── EventViewer.Domain/          # Entities and business logic
│   ├── EventViewer.Application/     # Use cases and interfaces
│   ├── EventViewer.Infrastructure/  # Events gateway client, Application Insights, JSON persistence
│   └── EventViewer.API/             # REST Endpoints for UI
└── EventViewer.sln
```

### Project Dependencies

```
API → Infrastructure → Application → Domain
```

### How it works

1. The API receives a search (criteria or a CorrelationId) with the caller's Azure AD B2C bearer token.
2. The token is validated and forwarded to the events gateway, which returns the integration event log entries.
3. Events are grouped into flows by `CorrelationId` and ordered by time.
4. Optionally, each flow is enriched with the Application Insights logs of its `CorrelationId`.
5. Optionally, flows are saved as JSON files to be reloaded later.

## 📦 EventViewer.Domain

Contains domain entities without external dependencies.

### Main Entities

- **`ServiceBusEvent`**: Integration event log entry (one publication or one subscription)
  - EventId (message ID, shared by the publication and all of its subscriptions)
  - CorrelationId, EventName (event type), Timestamp (UTC)
  - Action (Publishing/Subscribing), Status (Processing/Processed/Failed)
  - Metadata (microservice, user), ErrorMessage
  - Payload, topic and subscription are not provided by the events gateway
  - ChildEventIds (reserved for causal links; empty until the source provides causation data)

- **`EventMetadata`**: Microservice information
  - MicroserviceName, TopicName
  - SubscriptionName, RuleName, UserId

- **`ApplicationInsightsLog`**: Insights log
  - LogId, CorrelationId, Timestamp
  - SeverityLevel, Message, MicroserviceName
  - OperationName, OperationId
  - ExceptionStackTrace, ExceptionType
  - CustomProperties

- **`EventFlow`**: Complete flow aggregate
  - CorrelationId, StartTime, EndTime
  - Ordered Events list
  - Correlated InsightsLogs
  - DurationMs, HasFailures, MicroservicesCount

### Value Objects

- **`EventSearchCriteria`**: Search criteria
  - StartDate, EndDate, CorrelationId
  - Action, UserId, EventName
  - Microservices (list), Status, Keyword (business key linked to the event)
  - `IsValid()`: requires a date range or a CorrelationId

### Enums

- **`EventAction`**: Publishing | Subscribing
- **`EventStatus`**: Processed | Processing | Failed

## 📋 EventViewer.Application

Contains application logic and use cases.

### Interfaces

#### `IEventRepository`
```csharp
Task<IEnumerable<ServiceBusEvent>> SearchEventsAsync(EventSearchCriteria criteria);
Task<IEnumerable<ServiceBusEvent>> GetEventsByCorrelationIdAsync(string correlationId);
```

#### `IInsightsRepository`
```csharp
Task<IEnumerable<ApplicationInsightsLog>> GetLogsByCorrelationIdAsync(string correlationId, DateTime? startDate, DateTime? endDate);
Task<Dictionary<string, List<ApplicationInsightsLog>>> GetLogsByCorrelationIdsAsync(IEnumerable<string> correlationIds, DateTime? startDate, DateTime? endDate);
```

#### `IEventFlowPersistence`
```csharp
Task<string> SaveEventFlowAsync(EventFlow eventFlow);
Task<IEnumerable<string>> SaveEventFlowsAsync(IEnumerable<EventFlow> eventFlows);
Task<EventFlow?> LoadEventFlowAsync(string fileName);
Task<IEnumerable<string>> ListSavedFlowsAsync();
```

### Services

#### `EventFlowBuilder`
Groups correlated events into chronologically ordered flows:
- `BuildFlow(IEnumerable<ServiceBusEvent> events)`: Single flow
- `BuildMultipleFlows(IEnumerable<ServiceBusEvent> events)`: Multiple flows grouped by CorrelationId

The subscriptions of a publication are the `Subscribing` events with the same `EventId`.

### Use Cases

#### `SearchEventFlowUseCase`
Main use case:
```csharp
Task<SearchEventFlowResult> ExecuteAsync(
    EventSearchCriteria criteria,
    bool includeInsightsLogs = true,
    bool saveResults = false);

Task<SearchEventFlowResult> GetFlowByCorrelationIdAsync(
    string correlationId,
    bool includeInsightsLogs = true,
    bool saveResults = false);
```

**`SearchEventFlowResult`**: Result with IsSuccess, Flows, Message, SavedFilePath

## 🔧 EventViewer.Infrastructure

Concrete implementations of interfaces using Azure SDK and HttpClient.

### Implemented Repositories

#### `RestApiEventRepository`
Client for the events gateway (`POST api/gateway/search/searchByIntegrationEventLog/`).
- **Features**:
  - Sends the search criteria as gateway filters (see the table below)
  - Pages through the results (100 per page) up to `EventApi:MaxResults`
  - Forwards the caller's bearer token; a 401/403 from the gateway becomes a 401
  - Parses `creationDate` (en-US text, UTC) and maps the `ProcessedFailed` state to `Failed`

| API criterion | Gateway filter |
|---|---|
| `startDate` / `endDate` | `createdDateFrom` / `createdDateTo` |
| `correlationId` | `CorrelationId` |
| `action` | `Action` |
| `userId` | `UserId` |
| `eventName` | `EventTypeName` (exact name) |
| `status` | `State` |
| `keyword` | `keys` |
| `microservices` (one value) | `ServiceName` |

With several microservices, `ServiceName` is not sent and the API filters the results.

#### `InsightsApiRepository`
Queries Application Insights logs with `Azure.Monitor.Query` against the Log Analytics workspace.
- **Features**:
  - Workspace schema: `AppTraces`, `AppExceptions`, `AppRequests`, `AppDependencies`
  - Matches `Properties.CorrelationId` or `OperationId`; values are escaped with `LogsQueryClient.CreateQuery`
  - Searches from `LogsWindowMinutesBefore` (1) before the first event to `LogsWindowMinutesAfter` (10) after the last one
  - Authentication with `DefaultAzureCredential`

#### `JsonEventFlowPersistence`
Saves and loads event flows as JSON files.
- **Features**:
  - Configurable format (indented/compact)
  - File names `EventFlow_<CorrelationId prefix>_<UTC timestamp>.json`
  - Only file names inside `StoragePath` are accepted when loading

### Configuration

#### `DependencyInjection` Class
Extension method to register all services:
```csharp
services.AddInfrastructure(configuration);
```

#### Configuration Options

**`EventApiOptions`** (`appsettings.json` → `"EventApi"`)
```json
{
  "BaseUrl": "https://<gateway-host>",
  "TimeoutSeconds": 30,
  "MaxResults": 1000
}
```

**`ApplicationInsightsOptions`** (`appsettings.json` → `"ApplicationInsights"`)
```json
{
  "WorkspaceId": "<Log Analytics workspace ID>",
  "QueryTimeoutSeconds": 60,
  "MaxResults": 10000,
  "LogsWindowMinutesBefore": 1,
  "LogsWindowMinutesAfter": 10
}
```

**`JsonPersistenceOptions`** (`appsettings.json` → `"JsonPersistence"`)
```json
{
  "StoragePath": "./EventFlows",
  "CreateDirectoryIfNotExists": true,
  "UseIndentedJson": true,
  "FileNamePrefix": "EventFlow_",
  "FileExtension": ".json"
}
```

### Azure Authentication

Application Insights is queried with `DefaultAzureCredential` (environment variables, managed identity, Visual Studio, Azure CLI, Azure PowerShell...). For local development, run `az login` with an account that has the *Log Analytics Reader* role on the workspace.

## 🌐 EventViewer.API

REST API that exposes use cases to the frontend.

### Documentation

For detailed API documentation, see [API.md](./docs/API.md).

### Endpoints

- **`POST /api/eventflow/search`** - Search for event flows based on criteria
- **`GET /api/eventflow/flow/{correlationId}`** - Get specific flow by correlation ID
- **`GET /api/eventflow/saved`** - List all saved event flow files
- **`GET /api/eventflow/load/{fileName}`** - Load a previously saved flow

### Authentication

Every endpoint requires `Authorization: Bearer <token>` with the same Azure AD B2C token the events gateway accepts. The API validates it (issuer, audience, signature and lifetime) and forwards it to the gateway.

### Features

- Consistent error responses; 500 responses only expose a TraceId
- CORS support for frontend applications
- OpenAPI document at `/openapi/v1.json` (Development)
- Structured logging and OpenTelemetry tracing

### OpenTelemetry Tracing

The API is instrumented with **OpenTelemetry** for distributed tracing:

**Configuration** (`appsettings.json`):
```json
{
  "OpenTelemetry": {
    "ServiceName": "EventViewer.API",
    "OtlpEndpoint": "http://localhost:4318/v1/traces"
  }
}
```

**Features**:
- ✅ Automatic ASP.NET Core instrumentation (HTTP requests, middleware)
- ✅ Automatic HttpClient instrumentation (outgoing HTTP calls)
- ✅ Custom spans for business operations:
  - `SearchEventFlow` - Main search operation
  - `SearchEvents` - Repository event search
  - `BuildEventFlows` - Flow construction
  - `EnrichWithInsightsLogs` - Insights enrichment
  - `SaveEventFlows` - Persistence operations
  - `GetFlowByCorrelationId` - Single flow retrieval

**Viewing Traces**:
1. Open AI Toolkit trace viewer in VS Code: Run command `AI Toolkit: Open Trace Viewer`
2. The OTLP collector listens on `http://localhost:4318`
3. Traces include tags for correlation IDs, event counts, and operation status

**ActivitySource**: `EventViewer.Application` (version 1.0.0)

## Configuration

Settings are in `src/EventViewer.API/appsettings*.json`. Environment-specific values are kept out of the repository with user secrets:

```powershell
cd src/EventViewer.API
dotnet user-secrets set "EventApi:BaseUrl" "https://<gateway-host>"
dotnet user-secrets set "Authentication:MetadataAddress" "https://<tenant>.b2clogin.com/tfp/<tenant>.onmicrosoft.com/<policy>/v2.0/.well-known/openid-configuration"
dotnet user-secrets set "Authentication:Audience" "<gateway API client ID>"
dotnet user-secrets set "ApplicationInsights:WorkspaceId" "<Log Analytics workspace ID>"
```

The API does not start without `Authentication:MetadataAddress` and `Authentication:Audience`.

## Running

```powershell
dotnet run --project src/EventViewer.API --launch-profile http
```

The API listens on `http://localhost:5189`. Sample requests are in [EventViewer.API.http](src/EventViewer.API/EventViewer.API.http); put the bearer token in a git-ignored `.env` file next to it (`EVENTVIEWER_TOKEN=<token>`).

## 🚀 Next Steps

- [ ] Unit and integration tests
- [ ] Frontend with the graphical flow visualization (technology to be decided)
- [ ] Causal links between events (requires causation data from the source)

## 🛠️ Technologies

- **.NET 10**
- **Azure Service Bus** (events via the events gateway)
- **Azure AD B2C** (JWT bearer authentication)
- **Azure Application Insights** (logs)
- **System.Text.Json** (persistence)
- **Clean Architecture**
- **OpenTelemetry** (distributed tracing)

## 📝 Build

```powershell
dotnet build
dotnet run --project src/EventViewer.API
```

## 📄 License

Internal company project for debugging tools development.
