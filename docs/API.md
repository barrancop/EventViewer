# EventViewer API Documentation

## Overview
The EventViewer API provides endpoints to search, retrieve, and manage event flows between microservices. Events come from the integration event log exposed by the events gateway and are enriched with Application Insights logs.

## Base URL
```
http://localhost:5189/api
https://localhost:7179/api
```

## Authentication
Every endpoint requires an Azure AD B2C bearer token, the same token the events gateway accepts:

```
Authorization: Bearer <token>
```

The API validates the token (issuer, audience, signature and lifetime) and forwards it to the gateway. Requests without a valid token, or whose token the gateway rejects, get `401 Unauthorized`. In Development, `/openapi/v1.json` is available without a token.

## Endpoints

### 1. Search Event Flows

Search for event flows based on multiple criteria.

**Endpoint:** `POST /api/eventflow/search`

**Request Body:**
```json
{
  "startDate": "2026-10-14T06:00:00Z",
  "endDate": "2026-10-14T08:00:00Z",
  "correlationId": "11111111-2222-3333-4444-555555555555",
  "action": "Subscribing",
  "userId": "66666666-7777-8888-9999-000000000000",
  "eventName": "OrderCreatedIntegrationEvent",
  "microservices": ["OrderService"],
  "status": "Failed",
  "keyword": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "includeInsightsLogs": true,
  "saveResults": false
}
```

**Rules:**
- A date range or a `correlationId` is required; every other criterion is optional and they are combined with AND
- `eventName` must be the exact event type name; `keyword` is a business key linked to the event (e.g. an entity ID)
- `action`: `Publishing` | `Subscribing`; `status`: `Processing` | `Processed` | `Failed`
- With more than one microservice, that filter is applied by this API instead of the gateway

**Response:**
```json
{
  "isSuccess": true,
  "message": "Found 3 event flow(s)",
  "flows": [
    {
      "correlationId": "abc-123-def-456",
      "startTime": "2024-01-15T10:30:00Z",
      "endTime": "2024-01-15T10:30:05Z",
      "events": [...],
      "insightsLogs": [...],
      "durationMs": 5000,
      "hasFailures": false,
      "microservicesCount": 3
    }
  ],
  "savedFilePath": null
}
```

**Status Codes:**
- `200 OK` - Search completed successfully
- `400 Bad Request` - Invalid search criteria
- `401 Unauthorized` - Missing or invalid token, or token rejected by the events gateway
- `500 Internal Server Error` - Server error occurred

---

### 2. Get Event Flow by Correlation ID

Retrieve a specific event flow using its correlation ID.

**Endpoint:** `GET /api/eventflow/flow/{correlationId}`

**Path Parameters:**
- `correlationId` (string, required) - The correlation ID of the event flow

**Query Parameters:**
- `includeInsightsLogs` (boolean, optional, default: `true`) - Include Application Insights logs
- `saveResults` (boolean, optional, default: `false`) - Save the result to JSON file

**Example:**
```
GET /api/eventflow/flow/abc-123-def-456?includeInsightsLogs=true&saveResults=false
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "Found flow with CorrelationId: abc-123-def-456",
  "flows": [
    {
      "correlationId": "abc-123-def-456",
      "startTime": "2024-01-15T10:30:00Z",
      "endTime": "2024-01-15T10:30:05Z",
      "events": [
        {
          "eventId": "evt-001",
          "correlationId": "abc-123-def-456",
          "eventName": "OrderCreated",
          "timestamp": "2024-01-15T10:30:00Z",
          "action": "Publishing",
          "status": "Processed",
          "metadata": {
            "microserviceName": "OrderService",
            "topicName": "",
            "subscriptionName": null,
            "ruleName": null,
            "userId": "66666666-7777-8888-9999-000000000000"
          },
          "payload": null,
          "errorMessage": null,
          "childEventIds": []
        }
      ],
      "insightsLogs": [
        {
          "logId": "log-001",
          "correlationId": "abc-123-def-456",
          "timestamp": "2024-01-15T10:30:01Z",
          "severityLevel": "Information",
          "message": "Order processed successfully",
          "microserviceName": "OrderService",
          "operationName": "ProcessOrder",
          "operationId": "op-123",
          "exceptionStackTrace": null,
          "exceptionType": null,
          "customProperties": {
            "orderId": "456"
          }
        }
      ],
      "durationMs": 5000,
      "hasFailures": false,
      "microservicesCount": 3
    }
  ],
  "savedFilePath": null
}
```

**Status Codes:**
- `200 OK` - Event flow found
- `401 Unauthorized` - Missing or invalid token, or token rejected by the events gateway
- `404 Not Found` - No event flow found with the specified correlation ID
- `500 Internal Server Error` - Server error occurred

---

### 3. List Saved Flows

Get a list of all saved event flow files.

**Endpoint:** `GET /api/eventflow/saved`

**Response:**
```json
{
  "filePaths": [
    "EventFlow_11111111_20261014_080000.json",
    "EventFlow_22222222_20261014_093000.json"
  ],
  "count": 2
}
```

File names follow `EventFlow_<first 8 characters of the CorrelationId>_<UTC timestamp>.json` and are the values expected by the load endpoint.

**Status Codes:**
- `200 OK` - List retrieved successfully
- `401 Unauthorized` - Missing or invalid token
- `500 Internal Server Error` - Server error occurred

---

### 4. Load Saved Flow

Load a previously saved event flow from a JSON file.

**Endpoint:** `GET /api/eventflow/load/{fileName}`

**Path Parameters:**
- `fileName` (string, required) - A file name returned by `GET /api/eventflow/saved`; paths are not accepted

**Example:**
```
GET /api/eventflow/load/EventFlow_11111111_20261014_080000.json
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "Loaded flow from EventFlow_11111111_20261014_080000.json",
  "flows": [
    {
      "correlationId": "abc-123",
      "startTime": "2024-01-15T10:30:00Z",
      "endTime": "2024-01-15T10:30:05Z",
      "events": [...],
      "insightsLogs": [...],
      "durationMs": 5000,
      "hasFailures": false,
      "microservicesCount": 3
    }
  ],
  "savedFilePath": null
}
```

**Status Codes:**
- `200 OK` - File loaded successfully
- `401 Unauthorized` - Missing or invalid token
- `404 Not Found` - File not found
- `500 Internal Server Error` - Server error occurred

---

## Data Models

### SearchEventFlowRequest
```typescript
{
  startDate?: string;           // ISO 8601 date format
  endDate?: string;             // ISO 8601 date format
  correlationId?: string;       // Specific correlation ID
  action?: "Publishing" | "Subscribing";
  userId?: string;              // User identifier
  eventName?: string;           // Exact event type name
  microservices?: string[];     // Array of microservice names
  status?: "Processed" | "Processing" | "Failed";
  keyword?: string;             // Business key linked to the event (e.g. an entity ID)
  includeInsightsLogs: boolean; // Default: true
  saveResults: boolean;         // Default: false
}
```

### EventFlowResponse
```typescript
{
  isSuccess: boolean;
  message: string;
  flows?: EventFlowDto[];
  savedFilePath?: string;
}
```

### EventFlowDto
```typescript
{
  correlationId: string;
  startTime: string;            // ISO 8601 date format
  endTime?: string;             // ISO 8601 date format
  events: ServiceBusEventDto[];
  insightsLogs: ApplicationInsightsLogDto[];
  durationMs?: number;          // Duration in milliseconds
  hasFailures: boolean;
  microservicesCount: number;
}
```

### ServiceBusEventDto
```typescript
{
  eventId: string;              // Shared by a publication and all of its subscriptions
  correlationId: string;
  eventName: string;
  timestamp: string;            // ISO 8601 date format
  action: string;               // "Publishing" or "Subscribing"
  status: string;               // "Processed", "Processing", or "Failed"
  metadata: EventMetadataDto;
  payload?: string;             // Not provided by the events gateway
  errorMessage?: string;
  childEventIds: string[];      // Reserved for causal links (currently empty)
}
```

### EventMetadataDto
```typescript
{
  microserviceName: string;
  topicName: string;
  subscriptionName?: string;
  ruleName?: string;
  userId?: string;
}
```

### ApplicationInsightsLogDto
```typescript
{
  logId: string;
  correlationId: string;
  timestamp: string;            // ISO 8601 date format
  severityLevel: string;        // "Information", "Warning", "Error", etc.
  message: string;
  microserviceName: string;
  operationName?: string;
  operationId?: string;
  exceptionStackTrace?: string;
  exceptionType?: string;
  customProperties: { [key: string]: string };
}
```

## Error Handling

All endpoints follow a consistent error response format:

```json
{
  "isSuccess": false,
  "message": "Error description here"
}
```

Common error scenarios:
- **400 Bad Request**: Invalid search criteria or missing required fields
- **401 Unauthorized**: Missing or invalid token, or token rejected by the events gateway
- **404 Not Found**: Resource not found (correlation ID or file)
- **500 Internal Server Error**: Unexpected server error; the message only contains a `TraceId` (details are in the server logs and traces)

## CORS Configuration

The API is configured to accept requests from the following origins:
- `http://localhost:3000` (React default)
- `http://localhost:5173` (Vite default)
- `http://localhost:4200` (Angular default, development only)

## OpenTelemetry Tracing

All API requests are traced with OpenTelemetry and exported over OTLP to `OpenTelemetry:OtlpEndpoint` (default `http://localhost:4318/v1/traces`). See [TRACING.md](TRACING.md).

## Configuration

See the *Configuration* section of the [README](../README.md). Environment-specific values (gateway URL, B2C metadata and audience, workspace ID) are kept in user secrets, not in `appsettings.json`.

## Running the API

### Development
```bash
dotnet run --project src/EventViewer.API --launch-profile http
```

The API will be available at:
- HTTP: `http://localhost:5189`
- HTTPS: `https://localhost:7179` (`https` launch profile)

### Production
```bash
dotnet publish -c Release
cd bin/Release/net10.0/publish
dotnet EventViewer.API.dll
```

## Example Usage

Sample requests for the REST Client extension are in [EventViewer.API.http](../src/EventViewer.API/EventViewer.API.http).

### Using cURL

**Search for events:**
```bash
curl -X POST http://localhost:5189/api/eventflow/search \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "startDate": "2026-10-14T06:00:00Z",
    "endDate": "2026-10-14T08:00:00Z",
    "eventName": "OrderCreatedIntegrationEvent",
    "includeInsightsLogs": true
  }'
```

**Get flow by correlation ID:**
```bash
curl -H "Authorization: Bearer $TOKEN" "http://localhost:5189/api/eventflow/flow/abc-123-def-456?includeInsightsLogs=true"
```

**List saved flows:**
```bash
curl -H "Authorization: Bearer $TOKEN" http://localhost:5189/api/eventflow/saved
```

### Using JavaScript (Fetch API)

```javascript
// Search for events
const searchEvents = async (token) => {
  const response = await fetch('http://localhost:5189/api/eventflow/search', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      startDate: '2026-10-14T06:00:00Z',
      endDate: '2026-10-14T08:00:00Z',
      eventName: 'OrderCreatedIntegrationEvent',
      includeInsightsLogs: true,
      saveResults: false
    })
  });
  
  const data = await response.json();
  console.log(data);
};

// Get flow by correlation ID
const getFlow = async (correlationId, token) => {
  const response = await fetch(
    `http://localhost:5189/api/eventflow/flow/${correlationId}?includeInsightsLogs=true`,
    { headers: { 'Authorization': `Bearer ${token}` } }
  );
  
  const data = await response.json();
  console.log(data);
};
```

## Next Steps

1. **Add Rate Limiting**: Prevent API abuse
2. **Add Caching**: Cache frequently accessed flows
3. **Add Pagination**: Support large result sets
4. **Add WebSockets**: Real-time event flow updates

## Support

For issues or questions, please refer to the main project README or create an issue in the repository.
