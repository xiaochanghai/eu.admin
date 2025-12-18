using EU.Core.Model.ViewModels.Extend;
using EU.Core.Models;
using ModelContextProtocol.Client;
//using ModelContextProtocol.Protocol.Transport;

namespace EU.Core.Api.Controllers.MCP;

/// <summary>
/// MCP 协议控制器 - 处理标准的 MCP 协议请求
/// </summary>
[ApiController, Route("api/Stream"), Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_Other), GlobalActionFilter]
public class StreamController : ControllerBase
{
    private readonly ILogger<StreamController> _logger;
    private readonly IFileAttachmentServices _fileAttachmentServices;
    //private readonly ChatAIClient chatAIClient;

    public StreamController(ILogger<StreamController> logger, IFileAttachmentServices fileAttachmentServices)
    {

        // 创建聊天客户端实例
        //chatAIClient = new ChatAIClient();
        _logger = logger;
        _fileAttachmentServices = fileAttachmentServices;
    }
    /// <summary>
    /// 处理 MCP 流式请求
    /// </summary>
    [HttpPost("chat/{chatId}")]
    public async Task<IActionResult> HandleStreamRequest([FromBody] StreamRequest request, Guid chatId)
    {
        try
        {
            _logger.LogInformation("收到 MCP 流式请求: {Method}", request.messages);

            // 设置响应头以支持流式传输
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("Access-Control-Allow-Headers", "Cache-Control");

            var config = new HttpClientTransport(
                new HttpClientTransportOptions()
                {
                    Endpoint = new Uri("http://localhost:8020/Supplier/mcp"),
                }
            );

            // Create an MCP client instance using the above configuration
            var client = await McpClient.CreateAsync(config);

            // 调用客户端的 ListToolsAsync 方法，获取可用工具列表
            var listToolsResult = await client.ListToolsAsync();


            //await chatAIClient.ProcessQueryAsync("测试", listToolsResult);

            if (request.messages[0].fileId.IsNotEmptyOrNull())
                request.messages[0].content += $"fileId:{request.messages[0].fileId}";

            var cancellationToken = HttpContext.RequestAborted;
            await foreach (var streamEvent in ChatHelper.CallStreamAsync(chatId, request.messages[0].content, listToolsResult, cancellationToken))
            {
                //var eventData = System.Text.Json.JsonSerializer.Serialize(streamEvent);
                //var sseMessage = $"data: {eventData}\n\n";

                //await Response.WriteAsync("开始执行工具", HttpContext.RequestAborted);
                //await Response.Body.FlushAsync(HttpContext.RequestAborted);

                //var eventData = $"event: {streamEvent.EventType}\ndata: {JsonHelper.ObjToJson(streamEvent.Data)}\nid: {streamEvent.Id}\n\n";

                var obj = new
                {
                    content = streamEvent.Data
                };

                var eventData = $"event: {streamEvent.EventType}\ndata: {JsonHelper.ObjToJson(obj)}\nid: {streamEvent.Id}\n\n";
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
            _logger.LogError(ex, "处理 MCP 流式请求失败: {Method}", request.messages[0].content);

            var errorResponse = new McpResponse
            {
                Id = Guid.NewGuid().ToString(),
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
        var name1 = RoutePrefix.Name;
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


    #region 上传附件接口
    /// <summary>
    /// 上传附件接口
    /// </summary>
    /// <param name="upload"></param>
    /// <returns></returns>
    [HttpPost("Upload"), AllowAnonymous]
    public async Task<ServiceResult<AnalysisUploadResult>> UploadAsync([FromForm] UploadForm upload) => await _fileAttachmentServices.AnalysisUploadAsync(upload);
    #endregion

}

public class StreamRequest
{
    public List<StreamRequestMessages> messages { get; set; }
    public bool? stream { get; set; }
    public string model { get; set; }
    public StreamThinking thinking { get; set; }
}

public class StreamThinking
{
    public string type { get; set; }
}

public class StreamRequestMessages
{
    public string role { get; set; }
    public string content { get; set; }
    public string fileId { get; set; }
}
