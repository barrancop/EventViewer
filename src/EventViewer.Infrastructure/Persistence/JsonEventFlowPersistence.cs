using System.Text.Json;
using System.Text.Json.Serialization;
using EventViewer.Application.Interfaces;
using EventViewer.Domain.Entities;
using EventViewer.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace EventViewer.Infrastructure.Persistence;

/// <summary>
/// Event flow persistence implementation in JSON format
/// </summary>
public class JsonEventFlowPersistence : IEventFlowPersistence
{
    private readonly JsonPersistenceOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonEventFlowPersistence(IOptions<JsonPersistenceOptions> options)
    {
        _options = options.Value;
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = _options.UseIndentedJson,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        EnsureStorageDirectoryExists();
    }

    public async Task<string> SaveEventFlowAsync(
        EventFlow eventFlow,
        CancellationToken cancellationToken = default)
    {
        var fileName = GenerateFileName(eventFlow);
        var filePath = Path.Combine(_options.StoragePath, fileName);

        try
        {
            var json = JsonSerializer.Serialize(eventFlow, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            return fileName;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error saving event flow to {filePath}: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<string>> SaveEventFlowsAsync(
        IEnumerable<EventFlow> eventFlows,
        CancellationToken cancellationToken = default)
    {
        var savedPaths = new List<string>();

        foreach (var eventFlow in eventFlows)
        {
            var path = await SaveEventFlowAsync(eventFlow, cancellationToken);
            savedPaths.Add(path);
        }

        return savedPaths;
    }

    public async Task<EventFlow?> LoadEventFlowAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        // Only bare names following the naming convention are accepted, so reads cannot leave StoragePath
        var safeFileName = Path.GetFileName(fileName);
        var filePath = Path.Combine(_options.StoragePath, safeFileName);
        var isSavedFlowFile = safeFileName.StartsWith(_options.FileNamePrefix, StringComparison.OrdinalIgnoreCase)
            && safeFileName.EndsWith(_options.FileExtension, StringComparison.OrdinalIgnoreCase);

        if (!isSavedFlowFile || !File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {fileName}");
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonSerializer.Deserialize<EventFlow>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error loading event flow from {filePath}: {ex.Message}", ex);
        }
    }

    public Task<IEnumerable<string>> ListSavedFlowsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(_options.StoragePath))
            {
                return Task.FromResult(Enumerable.Empty<string>());
            }

            var searchPattern = $"{_options.FileNamePrefix}*{_options.FileExtension}";
            var fileNames = Directory.GetFiles(_options.StoragePath, searchPattern)
                .OrderByDescending(f => File.GetCreationTime(f))
                .Select(f => Path.GetFileName(f))
                .ToList();

            return Task.FromResult<IEnumerable<string>>(fileNames);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error listing saved flows: {ex.Message}", ex);
        }
    }

    private void EnsureStorageDirectoryExists()
    {
        if (_options.CreateDirectoryIfNotExists && !Directory.Exists(_options.StoragePath))
        {
            Directory.CreateDirectory(_options.StoragePath);
        }
    }

    private string GenerateFileName(EventFlow eventFlow)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        // CorrelationId comes from external data, so characters that could alter the path are replaced
        var invalidChars = Path.GetInvalidFileNameChars();
        var correlationIdShort = new string(eventFlow.CorrelationId
            .Take(8)
            .Select(c => invalidChars.Contains(c) ? '_' : c)
            .ToArray());

        return $"{_options.FileNamePrefix}{correlationIdShort}_{timestamp}{_options.FileExtension}";
    }
}
