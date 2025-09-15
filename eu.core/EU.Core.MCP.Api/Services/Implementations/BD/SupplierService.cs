using EU.Core.Api.MCP.Attributes;
using EU.Core.Api.MCP.Interfaces;
using EU.Core.Api.MCP.Models.Mcp;
using EU.Core.Common.Helper;
using System.ComponentModel;


namespace EU.Core.Api.MCP.Services.Implementations;

public class InputSchemaArguments()
{
    /// <summary>
    /// 供应商名称
    /// </summary>
    [Description("供应商名称")]
    public string? name { get; set; }

}

public class SupplierService : BaseService<SupplierService>, ISupplierService
{
    private readonly string moudleCode = "BD_SUPPLIER_MNG";

    public SupplierService(ILogger<SupplierService> logger) : base(logger)
    {
        // 在构造函数末尾初始化服务
        InitializeService(this);
    }

    #region 获取供应商列表 
    /// <summary>
    /// 获取供应商管理模块的页面代码，用于加载供应商列表界面
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    [McpTool(
    "query_supplier_list",
    "用于打开供应商列表页面。当用户希望浏览、查看或管理供应商信息时使用。" +
    "此工具会返回前端模块标识 'BD_SUPPLIER_MNG'，客户端应据此加载对应的表格组件或跳转至管理界面。" +
    "注意：这是一个页面导航操作，即使之前已调用过，只要用户再次提出查看请求，也应重新调用本工具。")]
    public McpToolResult QuerySupplierList(object arguments)
    {
        return new McpToolResult
        {
            Content = new[]
            {
                new McpContent
                {
                    Type = "text",
                    Text = JsonHelper.ObjToJson(new
                    {
                        type = "module_list",
                        moudleCode
                    })
                }
            }
        };
    }
    #endregion

    #region 创建供应商 
    /// <summary>
    /// 创建供应商管理模块的页面代码，用于加载供应商列表界面
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    [McpTool(
    "create_supplier",
    "用于打开创建供应商表单页面。当用户希望创建供应商信息时使用。" +
    "此工具会返回前端模块标识 'BD_SUPPLIER_MNG'，客户端应据此加载对应的表单界面。" +
    "注意：这是一个页面导航操作，即使之前已调用过，只要用户再次提出查看请求，也应重新调用本工具。")]
    public McpToolResult CreateSupplierList(object arguments)
    {
        return new McpToolResult
        {
            Content = new[]
            {
                new McpContent
                {
                    Type = "text",
                    Text = JsonHelper.ObjToJson(new
                    {
                        type = "module_edit",
                        moudleCode
                    })
                }
            }
        };
    }
    #endregion

    [McpTool("test_hello_Supplier3", "A simple test supplier tool that says hello", typeof(InputSchemaArguments))]
    public async Task<McpToolResult> HandleTestHello2(object arguments)
    {
        var aaqq = JsonHelper.JsonToObj<InputSchemaArguments>(JsonHelper.ObjToJson(arguments));

        //if (arguments.ValueKind != JsonValueKind.Undefined &&
        //    arguments.TryGetProperty("name", out var nameProperty))
        //{
        //    name = nameProperty.GetString() ?? "World";
        //}

        return new McpToolResult
        {
            Content = new[]
            {
                new McpContent
                {
                    Type = "text",
                    Text = $"Hello, {aaqq.name},{DateTime.Now}! Supplier MCP server is working! "
                }
            }
        };
    }

    //public object GetAvailableTools()
    //{
    //    var allTools = GetTools().ToArray();

    //    _logger.LogInformation($"Returning {allTools.Length} available tools");
    //    return new { tools = allTools };
    //}

    //public async Task<McpToolResult> HandleToolCallAsync(JsonElement? parameters)
    //{
    //    if (parameters == null)
    //        throw new ArgumentException("Missing parameters");

    //    var toolName = parameters.Value.GetProperty("name").GetString();
    //    var arguments = parameters.Value.TryGetProperty("arguments", out var args) ? args : default;

    //    if (string.IsNullOrEmpty(toolName))
    //        throw new ArgumentException("Tool name is required");

    //    _logger.LogInformation($"Executing tool: {toolName}");

    //    // Find the service that can handle this tool
    //    var isExist = CanHandle(toolName);

    //    if (!isExist)
    //        throw new ArgumentException($"No service found for tool: {toolName}");

    //    return await ExecuteToolAsync(toolName, arguments);
    //}

    //public IEnumerable<McpTool> GetTools()
    //{
    //    return
    //    [
    //        new McpTool
    //        {
    //            Name = "test_hello_Supplier",
    //            Description = "A simple test tool that says hello",
    //            InputSchema = new
    //            {
    //                type = "object",
    //                properties = new
    //                {
    //                    name = new { type = "string", description = "Name to greet" }
    //                }
    //            }
    //        }
    //    ];
    //}

    //public bool CanHandle(string toolName)
    //{
    //    var tools = GetTools();
    //    return tools.Any(x => x.Name == toolName); ;
    //}


    //public async Task<McpToolResult> ExecuteToolAsync(string toolName, JsonElement arguments)
    //{
    //    _logger.LogInformation($"Executing test tool: {toolName}");

    //    return toolName switch
    //    {
    //        "test_hello_Supplier" => await HandleTestHello(arguments),
    //        _ => throw new ArgumentException($"Unknown tool: {toolName}")
    //    };
    //}

    //private async Task<McpToolResult> HandleTestHello(JsonElement arguments)
    //{
    //    await Task.Delay(10); // Simulate async work

    //    var name = "World";
    //    if (arguments.ValueKind != JsonValueKind.Undefined &&
    //        arguments.TryGetProperty("name", out var nameProperty))
    //    {
    //        name = nameProperty.GetString() ?? "World";
    //    }

    //    return new McpToolResult
    //    {
    //        Content = new[]
    //        {
    //            new McpContent
    //            {
    //                Type = "text",
    //                Text = $"Hello, {name},{DateTime.Now}! Supplier MCP server is working! "
    //            }
    //        }
    //    };
    //}
}