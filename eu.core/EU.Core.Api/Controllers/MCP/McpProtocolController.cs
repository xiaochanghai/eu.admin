using EU.Core.MCP.Interfaces;
using EU.Core.MCP.Models;

namespace EU.Core.Api.Controllers.MCP;

/// <summary>
/// MCP 协议控制器 - 处理标准的 MCP 协议请求
/// </summary>
[ApiController]
[Route("api/mcp")]
public class McpProtocolController : ControllerBase
{
    private readonly IMcpService _mcpService;
    private readonly ILogger<McpProtocolController> _logger;

    public McpProtocolController(IMcpService mcpService, ILogger<McpProtocolController> logger)
    {
        _mcpService = mcpService;
        _logger = logger;
    }

    /// <summary>
    /// 处理 MCP 协议请求
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> HandleRequest([FromBody] McpRequest request)
    {
        try
        {
            _logger.LogInformation("收到 MCP 请求: {Method} {Id}", request.Method, request.Id);
            
            var response = await _mcpService.HandleRequestAsync(request);
            
            _logger.LogInformation("MCP 请求处理完成: {Method} {Id}", request.Method, request.Id);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 MCP 请求失败: {Method} {Id}", request.Method, request.Id);
            
            var errorResponse = new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32603,
                    Message = ex.Message,
                    Data = ex.StackTrace
                }
            };
            
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// 处理 MCP 流式请求
    /// </summary>
    [HttpPost("stream")]
    public async Task<IActionResult> HandleStreamRequest([FromBody] McpRequest request)
    {
        try
        {
            _logger.LogInformation("收到 MCP 流式请求: {Method} {Id}", request.Method, request.Id);

            // 设置响应头以支持流式传输
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("Access-Control-Allow-Headers", "Cache-Control");

            // 如果是工具调用，使用流式响应
            if (request.Method == "tools/call")
            {
                var callParams = System.Text.Json.JsonSerializer.Deserialize<McpToolsCallParams>(System.Text.Json.JsonSerializer.Serialize(request.Params));
                if (callParams != null)
                {
                    await foreach (var streamEvent in _mcpService.CallToolStreamAsync(
                        callParams.Name, 
                        callParams.Arguments ?? new { }, 
                        HttpContext.RequestAborted))
                    {
                        var eventData = System.Text.Json.JsonSerializer.Serialize(streamEvent);
                        var sseMessage = $"data: {eventData}\n\n";
                        
                        await Response.WriteAsync(sseMessage, HttpContext.RequestAborted);
                        await Response.Body.FlushAsync(HttpContext.RequestAborted);
                        
                        if (HttpContext.RequestAborted.IsCancellationRequested)
                            break;
                    }
                    
                    return new EmptyResult();
                }
            }
            
            // 非流式请求，返回普通响应
            var response = await _mcpService.HandleRequestAsync(request);
            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            // 客户端取消请求
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 MCP 流式请求失败: {Method} {Id}", request.Method, request.Id);
            
            var errorResponse = new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32603,
                    Message = ex.Message,
                    Data = ex.StackTrace
                }
            };
            
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// 健康检查
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            service = "MCP Protocol Service",
            version = "1.0.0",
            protocol = "MCP 2024-11-05"
        });
    }
} 