using ClaudeMCP.API.Models.Mcp;
using ClaudeMCP.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeMCP.API.Controllers;

[ApiController]
[Route("/")]
public class McpController : ControllerBase
{
    private readonly IMcpService _mcpService;
    private readonly ILogger<McpController> _logger;

    public McpController(IMcpService mcpService, ILogger<McpController> logger)
    {
        _mcpService = mcpService;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult HealthCheck()
    {
        return Ok("ClaudeMCP API is running!");
    }

    /// <summary>
    /// Main MCP endpoint for JSON-RPC requests
    /// </summary>
    [HttpPost("mcp")]
    public async Task<JsonRpcResponse> HandleMcpRequest([FromBody] JsonRpcRequest request)
    {
        _logger.LogInformation($"Received MCP request: {request.Method}");
        
        try
        {
            var result = await ProcessMcpMethod(request);

            return new JsonRpcResponse
            {
                Result = result,
                Id = request.Id
                // Don't set Error field when there's no error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling MCP request: {request.Method}");
            
            return new JsonRpcResponse
            {
                Error = new JsonRpcError
                {
                    Code = GetErrorCode(ex),
                    Message = ex.Message
                },
                Id = request.Id
            };
        }
    }

    private async Task<object> ProcessMcpMethod(JsonRpcRequest request)
    {
        return request.Method switch
        {
            "initialize" => _mcpService.HandleInitialize(request.Params),
            "tools/list" => _mcpService.GetAvailableTools(),
            "tools/call" => await _mcpService.HandleToolCallAsync(request.Params),
            _ => throw new ArgumentException($"Unknown method: {request.Method}")
        };
    }

    private static int GetErrorCode(Exception ex)
    {
        return ex switch
        {
            ArgumentException => -32602, // Invalid params
            NotSupportedException => -32601, // Method not found
            _ => -32603 // Internal error
        };
    }
}