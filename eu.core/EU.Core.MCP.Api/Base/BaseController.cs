using ClaudeMCP.API.Models.Mcp;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace EU.Core.MCP.Controllers;

/// <summary>
/// MCP基础服务
/// </summary>
/// <typeparam name="IServiceBase"></typeparam>
[ApiController, Route("/[controller]")]
public class BaseController<IServiceBase> : ControllerBase
{
    #region 初始化
    protected readonly IServiceBase _service;
    protected readonly ILogger<BaseController<IServiceBase>> _logger;

    /// <summary>
    /// 初始化 (注入)
    /// </summary>
    public BaseController(IServiceBase service, ILogger<BaseController<IServiceBase>> logger)
    {
        _service = service;
        _logger = logger;
    }
    #endregion

    #region MyRegion
    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult HealthCheck()
    {
        return Ok("ClaudeMCP API is running!");
    }
    #endregion


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
        _logger.LogInformation($"Received MCP request.Method: {request.Method}");
        switch (request.Method)
        {
            case "initialize":
                var result = InvokeService("HandleInitialize", [request.Params!]);
                return result;
            case "tools/list":
                return InvokeService("GetAvailableTools", []);

            case "tools/call":
                return await InvokeServiceAsync("HandleToolCallAsync", [request.Params!]);
            default:
                throw new ArgumentException($"Unknown method: {request.Method}");
        }
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

    private object InvokeService(string methodName, object[] parameters)
    {
        return _service.GetType().GetMethod(methodName).Invoke(_service, parameters);
    }


    [NonAction]
    private async Task<object> InvokeServiceAsync(string methodName, object[] parameters)
    {
        var task = _service.GetType().InvokeMember(methodName, BindingFlags.InvokeMethod, null, _service, parameters) as Task;
        if (task != null) await task;
        var result = task?.GetType().GetProperty("Result")?.GetValue(task);
        return result;
    }
}