using EventViewer.API.DTOs;
using EventViewer.API.Mappers;
using EventViewer.Application.Interfaces;
using EventViewer.Application.UseCases;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EventViewer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EventFlowController : ControllerBase
{
    private readonly SearchEventFlowUseCase _searchUseCase;
    private readonly IEventFlowPersistence _persistence;
    private readonly ILogger<EventFlowController> _logger;

    public EventFlowController(
        SearchEventFlowUseCase searchUseCase,
        IEventFlowPersistence persistence,
        ILogger<EventFlowController> logger)
    {
        _searchUseCase = searchUseCase;
        _persistence = persistence;
        _logger = logger;
    }

    /// <summary>
    /// Searches for event flows based on criteria
    /// </summary>
    /// <param name="request">Search criteria</param>
    /// <returns>Event flows matching the criteria</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(EventFlowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventFlowResponse>> SearchEventFlows([FromBody] SearchEventFlowRequest request)
    {
        try
        {
            _logger.LogInformation("Searching event flows with criteria: {@Criteria}", request);
            
            var searchCriteria = EventFlowMapper.ToSearchCriteria(request);
            var result = await _searchUseCase.ExecuteAsync(
                searchCriteria, 
                request.IncludeInsightsLogs, 
                request.SaveResults);
            
            if (!result.IsSuccess)
            {
                return BadRequest(new EventFlowResponse
                {
                    IsSuccess = false,
                    Message = result.Message
                });
            }
            
            var response = new EventFlowResponse
            {
                IsSuccess = true,
                Message = result.Message,
                Flows = result.Flows.Select(EventFlowMapper.ToDto).ToList(),
                SavedFilePath = result.SavedFilePath
            };
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "The events API rejected the credentials");
            return EventsApiUnauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching event flows");
            return InternalError();
        }
    }

    /// <summary>
    /// Gets an event flow by correlation ID
    /// </summary>
    /// <param name="correlationId">Correlation ID</param>
    /// <param name="includeInsightsLogs">Include Application Insights logs</param>
    /// <param name="saveResults">Save results to JSON</param>
    /// <returns>Event flow for the specified correlation ID</returns>
    [HttpGet("flow/{correlationId}")]
    [ProducesResponseType(typeof(EventFlowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventFlowResponse>> GetEventFlowByCorrelationId(
        string correlationId,
        [FromQuery] bool includeInsightsLogs = true,
        [FromQuery] bool saveResults = false)
    {
        try
        {
            _logger.LogInformation("Getting event flow for CorrelationId: {CorrelationId}", correlationId);
            
            var result = await _searchUseCase.GetFlowByCorrelationIdAsync(
                correlationId, 
                includeInsightsLogs, 
                saveResults);
            
            if (!result.IsSuccess || !result.Flows.Any())
            {
                return NotFound(new EventFlowResponse
                {
                    IsSuccess = false,
                    Message = result.Message
                });
            }
            
            var response = new EventFlowResponse
            {
                IsSuccess = true,
                Message = result.Message,
                Flows = result.Flows.Select(EventFlowMapper.ToDto).ToList(),
                SavedFilePath = result.SavedFilePath
            };
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "The events API rejected the credentials");
            return EventsApiUnauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event flow for CorrelationId: {CorrelationId}", correlationId);
            return InternalError();
        }
    }

    /// <summary>
    /// Lists all saved event flow files
    /// </summary>
    /// <returns>List of saved flow file paths</returns>
    [HttpGet("saved")]
    [ProducesResponseType(typeof(SavedFlowsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SavedFlowsResponse>> GetSavedFlows()
    {
        try
        {
            _logger.LogInformation("Getting list of saved flows");
            
            var filePaths = await _persistence.ListSavedFlowsAsync();
            
            var response = new SavedFlowsResponse
            {
                FilePaths = filePaths.ToList(),
                Count = filePaths.Count()
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved flows");
            return InternalError();
        }
    }

    /// <summary>
    /// Loads a specific saved event flow
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <returns>Event flow from the saved file</returns>
    [HttpGet("load/{fileName}")]
    [ProducesResponseType(typeof(EventFlowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventFlowResponse>> LoadSavedFlow(string fileName)
    {
        try
        {
            _logger.LogInformation("Loading saved flow: {FileName}", fileName);
            
            var flow = await _persistence.LoadEventFlowAsync(fileName);
            
            if (flow == null)
            {
                return NotFound(new EventFlowResponse
                {
                    IsSuccess = false,
                    Message = $"No flow found in file: {fileName}"
                });
            }
            
            var response = new EventFlowResponse
            {
                IsSuccess = true,
                Message = $"Loaded flow from {fileName}",
                Flows = new List<EventFlowDto> { EventFlowMapper.ToDto(flow) }
            };
            
            return Ok(response);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new EventFlowResponse
            {
                IsSuccess = false,
                Message = $"File not found: {fileName}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading saved flow: {FileName}", fileName);
            return InternalError();
        }
    }

    private ObjectResult EventsApiUnauthorized() =>
        StatusCode(StatusCodes.Status401Unauthorized, new EventFlowResponse
        {
            IsSuccess = false,
            Message = "The events API rejected the credentials. Send a valid, non-expired bearer token."
        });

    // Details stay in the logs; the TraceId lets developers find them
    private ObjectResult InternalError() =>
        StatusCode(StatusCodes.Status500InternalServerError, new EventFlowResponse
        {
            IsSuccess = false,
            Message = $"An unexpected error occurred. TraceId: {Activity.Current?.TraceId.ToString() ?? HttpContext.TraceIdentifier}"
        });
}
