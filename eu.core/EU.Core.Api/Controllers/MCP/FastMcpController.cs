using EU.Core.MCP.Interfaces;
using EU.Core.MCP.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace EU.Core.Api.Controllers.MCP;

/// <summary>
/// FastMCP 风格控制器 - 支持装饰器和流式传输
/// </summary>
[ApiController]
//[Authorize]
[Route("api/fastmcp")]
public class FastMcpController : ControllerBase
{
    private readonly IMcpService _mcpService;
    private readonly ILogger<FastMcpController> _logger;

    public FastMcpController(IMcpService mcpService, ILogger<FastMcpController> logger)
    {
        _mcpService = mcpService;
        _logger = logger;
    }

    /// <summary>
    /// 获取可用工具列表
    /// </summary>
    [HttpGet("tools")]
    public async Task<IActionResult> GetTools()
    {
        try
        {
            var tools = await _mcpService.GetToolsAsync();
            return Ok(new { success = true, data = tools });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工具列表失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 获取可用资源列表
    /// </summary>
    [HttpGet("resources")]
    public async Task<IActionResult> GetResources()
    {
        try
        {
            var resources = await _mcpService.GetResourcesAsync();
            return Ok(new { success = true, data = resources });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取资源列表失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 执行工具调用（同步）
    /// </summary>
    [HttpPost("tools/call")]
    public async Task<IActionResult> CallTool([FromBody] McpToolCallRequest request)
    {
        try
        {
            var result = await _mcpService.CallToolAsync(request.ToolName, request.Parameters);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行工具调用失败: {ToolName}", request.ToolName);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 执行工具调用（流式响应）
    /// </summary>
    [HttpPost("tools/call/stream")]
    public async Task<IActionResult> CallToolStream([FromBody] McpToolCallRequest request)
    {
        try
        {
            // 设置响应头以支持流式传输
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("Access-Control-Allow-Headers", "Cache-Control");

            // 开始流式响应
            await foreach (var streamEvent in _mcpService.CallToolStreamAsync(
                request.ToolName, 
                request.Parameters, 
                HttpContext.RequestAborted))
            {
                var eventData =  JsonHelper.ObjToJson(streamEvent);
                var sseMessage = $"data: {eventData}\n\n";
                
                await Response.WriteAsync(sseMessage, HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
                
                if (HttpContext.RequestAborted.IsCancellationRequested)
                    break;
            }

            return new EmptyResult();
        }
        catch (OperationCanceledException)
        {
            // 客户端取消请求
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行流式工具调用失败: {ToolName}", request.ToolName);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 获取资源内容
    /// </summary>
    [HttpGet("resources/read")]
    public async Task<IActionResult> GetResource([FromQuery] string uri)
    {
        try
        {
            var resource = await _mcpService.GetResourceAsync(uri);
            return Ok(new { success = true, data = resource });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取资源失败: {Uri}", uri);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 处理通用MCP请求
    /// </summary>
    [HttpPost("request")]
    public async Task<IActionResult> HandleRequest([FromBody] McpRequest request)
    {
        try
        {
            var response = await _mcpService.HandleRequestAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理MCP请求失败: {Method}", request.Method);
            return StatusCode(500, new { success = false, message = ex.Message });
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
            service = "FastMCP Service",
            version = "1.0.0"
        });
    }
}

/// <summary>
/// MCP 工具调用请求模型
/// </summary>
public class McpToolCallRequest
{
    public string ToolName { get; set; } = string.Empty;
    public object Parameters { get; set; } = new();
} 