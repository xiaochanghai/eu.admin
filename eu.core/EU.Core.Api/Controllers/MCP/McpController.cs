using EU.Core.MCP.Interfaces;
using EU.Core.MCP.Models; 

namespace EU.Core.Api.Controllers.MCP;

/// <summary>
/// MCP 服务控制器 - 支持流式 HTTP 请求
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    /// 获取可用工具列表
    /// </summary>
    [HttpGet("tools")]
    public async Task<ActionResult<List<McpTool>>> GetTools()
    {
        try
        {
            var tools = await _mcpService.GetToolsAsync();
            return Ok(tools);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工具列表失败");
            return StatusCode(500, new { error = "获取工具列表失败" });
        }
    }

    /// <summary>
    /// 获取可用资源列表
    /// </summary>
    [HttpGet("resources")]
    public async Task<ActionResult<List<McpResource>>> GetResources()
    {
        try
        {
            var resources = await _mcpService.GetResourcesAsync();
            return Ok(resources);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取资源列表失败");
            return StatusCode(500, new { error = "获取资源列表失败" });
        }
    }

    /// <summary>
    /// 执行工具调用 - 支持流式响应
    /// </summary>
    [HttpPost("tools/call/stream")]
    public async Task<IActionResult> CallToolStream([FromBody] ToolCallRequest request)
    {
        try
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("Access-Control-Allow-Headers", "Cache-Control");

            // 发送连接建立事件
            await SendServerSentEvent("connected", "工具调用流已建立");

            // 执行工具调用
            var result = await _mcpService.CallToolAsync(request.Name, request.Arguments);
            
            // 发送执行结果事件
            await SendServerSentEvent("tool_result", JsonHelper.ObjToJson(result));

            // 发送完成事件
            await SendServerSentEvent("completed", "工具调用完成");

            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "工具调用失败: {ToolName}", request.Name);
            await SendServerSentEvent("error", ex.Message);
            return new EmptyResult();
        }
    }

    /// <summary>
    /// 执行工具调用 - 传统响应
    /// </summary>
    [HttpPost("tools/call")]
    public async Task<ActionResult<object>> CallTool([FromBody] ToolCallRequest request)
    {
        try
        {
            var result = await _mcpService.CallToolAsync(request.Name, request.Arguments);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "工具调用失败: {ToolName}", request.Name);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取资源内容 - 支持流式响应
    /// </summary>
    [HttpPost("resources/read/stream")]
    public async Task<IActionResult> ReadResourceStream([FromBody] ResourceReadRequest request)
    {
        try
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("Access-Control-Allow-Headers", "Cache-Control");

            await SendServerSentEvent("connected", "资源读取流已建立");

            var result = await _mcpService.GetResourceAsync(request.Uri);
            
            await SendServerSentEvent("resource_data", System.Text.Json.JsonSerializer.Serialize(result));
            await SendServerSentEvent("completed", "资源读取完成");

            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取资源失败: {Uri}", request.Uri);
            await SendServerSentEvent("error", ex.Message);
            return new EmptyResult();
        }
    }

    /// <summary>
    /// 获取资源内容 - 传统响应
    /// </summary>
    [HttpPost("resources/read")]
    public async Task<ActionResult<object>> ReadResource([FromBody] ResourceReadRequest request)
    {
        try
        {
            var result = await _mcpService.GetResourceAsync(request.Uri);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取资源失败: {Uri}", request.Uri);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 处理 MCP 请求（兼容 JSON-RPC 2.0）- 支持流式响应
    /// </summary>
    [HttpPost("rpc/stream")]
    public async Task<IActionResult> HandleRpcRequestStream([FromBody] McpRequest request)
    {
        try
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("Access-Control-Allow-Headers", "Cache-Control");

            await SendServerSentEvent("connected", "RPC 流已建立");

            var response = await _mcpService.HandleRequestAsync(request);
            
            await SendServerSentEvent("rpc_response", JsonHelper.ObjToJson(response));
            await SendServerSentEvent("completed", "RPC 请求处理完成");

            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理RPC请求失败");
            await SendServerSentEvent("error", ex.Message);
            return new EmptyResult();
        }
    }

    /// <summary>
    /// 处理 MCP 请求（兼容 JSON-RPC 2.0）- 传统响应
    /// </summary>
    [HttpPost("rpc")]
    public async Task<ActionResult<McpResponse>> HandleRpcRequest([FromBody] McpRequest request)
    {
        try
        {
            var response = await _mcpService.HandleRequestAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理RPC请求失败");
            return StatusCode(500, new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32603,
                    Message = "内部错误"
                }
            });
        }
    }

    /// <summary>
    /// 长轮询工具调用 - 支持长时间运行的工具
    /// </summary>
    [HttpPost("tools/call/long-polling")]
    public async Task<IActionResult> CallToolLongPolling([FromBody] ToolCallRequest request)
    {
        try
        {
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // 5分钟超时
            
            var result = await _mcpService.CallToolWithProgressAsync(request.Name, request.Arguments, 
                progress => 
                {
                    // 这里可以发送进度更新
                    _logger.LogInformation("工具 {ToolName} 执行进度: {Progress}", request.Name, progress);
                }, 
                cancellationTokenSource.Token);
            
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = "请求超时" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "工具调用失败: {ToolName}", request.Name);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 发送服务器发送事件
    /// </summary>
    private async Task SendServerSentEvent(string eventType, string data)
    {
        var eventData = $"event: {eventType}\ndata: {data}\n\n";
        var bytes = Encoding.UTF8.GetBytes(eventData);
        await Response.Body.WriteAsync(bytes, 0, bytes.Length);
        await Response.Body.FlushAsync();
    }
}

// 请求模型
public class ToolCallRequest
{
    public string Name { get; set; } = string.Empty;
    public object Arguments { get; set; } = new();
}

public class ResourceReadRequest
{
    public string Uri { get; set; } = string.Empty;
} 