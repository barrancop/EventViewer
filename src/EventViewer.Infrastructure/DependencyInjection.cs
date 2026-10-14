using Azure.Identity;
using EventViewer.Application.Interfaces;
using EventViewer.Application.Services;
using EventViewer.Application.UseCases;
using EventViewer.Infrastructure.Configuration;
using EventViewer.Infrastructure.Persistence;
using EventViewer.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventViewer.Infrastructure;

/// <summary>
/// Extension methods to configure Infrastructure services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all infrastructure services in the DI container
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<EventApiOptions>(
            configuration.GetSection(EventApiOptions.SectionName));
        
        services.Configure<ApplicationInsightsOptions>(
            configuration.GetSection(ApplicationInsightsOptions.SectionName));
        
        services.Configure<JsonPersistenceOptions>(
            configuration.GetSection(JsonPersistenceOptions.SectionName));

        // HttpClient for RestApiEventRepository
        services.AddHttpClient<IEventRepository, RestApiEventRepository>((serviceProvider, client) =>
        {
            var options = configuration
                .GetSection(EventApiOptions.SectionName)
                .Get<EventApiOptions>();

            if (!string.IsNullOrWhiteSpace(options?.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            }
        });

        // Repositories
        services.AddSingleton<IInsightsRepository, InsightsApiRepository>();
        services.AddSingleton<IEventFlowPersistence, JsonEventFlowPersistence>();

        // Application services
        services.AddScoped<EventFlowBuilder>();
        services.AddScoped<SearchEventFlowUseCase>();

        return services;
    }

    /// <summary>
    /// Registers a shared DefaultAzureCredential instance
    /// </summary>
    public static IServiceCollection AddAzureAuthentication(
        this IServiceCollection services)
    {
        // Not required by the repositories, which create their own credential
        services.AddSingleton(_ => new DefaultAzureCredential());
        
        return services;
    }
}
