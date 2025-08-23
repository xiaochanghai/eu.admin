using EU.Core.MCP.Interfaces;
using EU.Core.MCP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text; 

namespace EU.Core.Api.Controllers.MCP;

/// <summary>
/// MCP 流式控制器 - 专门处理流式请求
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class McpStreamController : ControllerBase
{
    private readonly IMcpService _mcpService;
    private readonly ILogger<McpStreamController> _logger;

    public McpStreamController(IMcpService mcpService, ILogger<McpStreamController> logger)
    {
        _mcpService = mcpService;
        _logger = logger;
    }

    /// <summary>
    /// 流式工具调用 - 使用 Server-Sent Events
    /// </summary>
    [HttpPost("tools/call")]
    public async Task<IActionResult> CallToolStream([FromBody] ToolCallRequest request)
    {
        try
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("Access-Control-Allow-Headers", "Cache-Control");

            var cancellationToken = HttpContext.RequestAborted;

            await foreach (var streamEvent in _mcpService.CallToolStreamAsync(request.Name, request.Arguments, cancellationToken))
            {
                var eventData = $"event: {streamEvent.EventType}\ndata: {JsonHelper.ObjToJson(streamEvent.Data)}\nid: {streamEvent.Id}\n\n";
                var bytes = Encoding.UTF8.GetBytes(eventData);
                await Response.Body.WriteAsync(bytes, 0, bytes.Length);
                await Response.Body.FlushAsync();
                
                cancellationToken.ThrowIfCancellationRequested();
            }

            return new EmptyResult();
        }
        catch (OperationCanceledException)
        {
            // 客户端断开连接
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "流式工具调用失败: {ToolName}", request.Name);
            var errorEvent = $"event: error\ndata: {JsonHelper.ObjToJson(new { error = ex.Message })}\n\n";
            var bytes = Encoding.UTF8.GetBytes(errorEvent);
            await Response.Body.WriteAsync(bytes, 0, bytes.Length);
            await Response.Body.FlushAsync();
            return new EmptyResult();
        }
    }

    /// <summary>
    /// 流式资源读取
    /// </summary>
    [HttpPost("resources/read")]
    public async Task<IActionResult> ReadResourceStream([FromBody] ResourceReadRequest request)
    {
        try
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("Access-Control-Allow-Headers", "Cache-Control");

            var cancellationToken = HttpContext.RequestAborted;

            // 发送开始事件
            await SendServerSentEvent("resource_started", "开始读取资源");

            // 读取资源
            var result = await _mcpService.GetResourceAsync(request.Uri);
            await SendServerSentEvent("resource_data", JsonHelper.ObjToJson(result));

            // 发送完成事件
            await SendServerSentEvent("resource_completed", "资源读取完成");

            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "流式资源读取失败: {Uri}", request.Uri);
            await SendServerSentEvent("error", ex.Message);
            return new EmptyResult();
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
 