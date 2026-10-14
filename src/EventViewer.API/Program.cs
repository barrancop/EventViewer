using EventViewer.API.Authentication;
using EventViewer.Infrastructure;
using EventViewer.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Add Infrastructure layer services (repositories, persistence, use cases)
builder.Services.AddInfrastructure(builder.Configuration);

// The caller's bearer token is forwarded to the events API
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAccessTokenProvider, ForwardedAccessTokenProvider>();

// Callers use the same Azure AD B2C token the gateway accepts; it is validated here before being forwarded
var metadataAddress = builder.Configuration["Authentication:MetadataAddress"];
var audience = builder.Configuration["Authentication:Audience"];
if (string.IsNullOrWhiteSpace(metadataAddress) || string.IsNullOrWhiteSpace(audience))
{
    throw new InvalidOperationException("Authentication:MetadataAddress and Authentication:Audience must be configured");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MetadataAddress = metadataAddress;
        options.Audience = audience;
    });

builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure OpenTelemetry Tracing
var serviceName = builder.Configuration.GetValue<string>("OpenTelemetry:ServiceName") ?? "EventViewer.API";
var otlpEndpoint = builder.Configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint") ?? "http://localhost:4318/v1/traces";

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
            // With HttpProtobuf the endpoint must include the signal path (/v1/traces)
            options.Endpoint = new Uri(otlpEndpoint);
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();
