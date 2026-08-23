using Microsoft.AspNetCore.Authorization;

namespace EU.Core.MCP.Controllers;

/// <summary>
/// Shared MCP JSON-RPC controller behavior.
/// </summary>
[ApiController, Authorize(Permissions.Name)]
public abstract class BaseController<TService> : ControllerBase
    where TService : class, IBaseService
{
    protected readonly TService? _service;
    protected readonly ILogger<BaseController<TService>> _logger;

    protected BaseController(
        TService? service,
        ILogger<BaseController<TService>> logger)
    {
        _service = service;
        _logger = logger;
    }

    [NonAction]
    protected async Task<JsonRpcResponse> HandleMcpRequestAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received MCP request: {Method}", request.Method);
        try
        {
            object result = await ProcessMcpMethodAsync(request, cancellationToken);
            return new JsonRpcResponse { Result = result, Id = request.Id };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error handling MCP request: {Method}", request.Method);
            return new JsonRpcResponse
            {
                Error = new JsonRpcError
                {
                    Code = GetErrorCode(exception),
                    Message = exception is ArgumentException or NotSupportedException
                        ? exception.Message
                        : "Internal error."
                },
                Id = request.Id
            };
        }
    }

    private async Task<object> ProcessMcpMethodAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        if (_service is null)
        {
            throw new InvalidOperationException("MCP service is unavailable.");
        }

        return request.Method switch
        {
            "notifications/initialized" => new { },
            "initialize" => _service.HandleInitialize(request.Params),
            "tools/list" => _service.GetAvailableTools(),
            "tools/call" => await _service.HandleToolCallAsync(request.Params, cancellationToken),
            _ => throw new NotSupportedException("Unknown MCP method.")
        };
    }

    private static int GetErrorCode(Exception exception) => exception switch
    {
        ArgumentException => -32602,
        NotSupportedException => -32601,
        _ => -32603
    };
}
