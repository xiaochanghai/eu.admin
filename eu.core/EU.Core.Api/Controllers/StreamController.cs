using EU.Core.MCP.Interfaces;
using EU.Core.MCP.Models;

namespace EU.Core.Api.Controllers.MCP;

/// <summary>
/// MCP 协议控制器 - 处理标准的 MCP 协议请求
/// </summary>
[ApiController]
[Route("api/Stream")]
public class StreamController : ControllerBase
{ 
    private readonly ILogger<McpProtocolController> _logger;

    public StreamController( ILogger<McpProtocolController> logger)
    { 
        _logger = logger;
    }
    /// <summary>
    /// 处理 MCP 流式请求
    /// </summary>
    [HttpPost("chat")]
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

            var cancellationToken = HttpContext.RequestAborted;
            await foreach (var streamEvent in CallToolStreamAsync(cancellationToken))
            {
                //var eventData = System.Text.Json.JsonSerializer.Serialize(streamEvent);
                //var sseMessage = $"data: {eventData}\n\n";

                //await Response.WriteAsync("开始执行工具", HttpContext.RequestAborted);
                //await Response.Body.FlushAsync(HttpContext.RequestAborted);

                var eventData = $"event: {streamEvent.EventType}\ndata: {JsonHelper.ObjToJson(streamEvent.Data)}\nid: {streamEvent.Id}\n\n";
                var bytes = Encoding.UTF8.GetBytes(eventData);
                await Response.Body.WriteAsync(bytes, 0, bytes.Length);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);


                if (cancellationToken.IsCancellationRequested)
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

    [NonAction]
    public async IAsyncEnumerable<McpStreamEvent> CallToolStreamAsync(
     [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new McpStreamEvent
        {
            EventType = "tool_started",
            Data = $"开始执行",
            Id = Guid.NewGuid().ToString()
        };

        for (int i = 0; i < 10; i++)
        {
            Thread.Sleep(1000); // 暂停1000毫秒（1秒）
            yield return new McpStreamEvent
            {
                EventType = "tool_result",
                Data = $"测试id: " + Guid.NewGuid(),
                Id = Guid.NewGuid().ToString()
            };
        }

        await foreach (var item in GenerateStreamingDataAsync(cancellationToken))
        {
            yield return new McpStreamEvent
            {
                EventType = "tool_progress",
                Data = item,
                Id = Guid.NewGuid().ToString()
            };
        }

        yield return new McpStreamEvent
        {
            EventType = "tool_completed",
            Data = $"执行完成",
            Id = Guid.NewGuid().ToString()
        };
    }
    // 模拟流式数据生成
    [NonAction]
    private async IAsyncEnumerable<object> GenerateStreamingDataAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < 10; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return new { index = i, data = $"流式数据 {i}", timestamp = DateTime.UtcNow };
            await Task.Delay(300, cancellationToken); // 模拟数据生成延迟
        }
    }

}