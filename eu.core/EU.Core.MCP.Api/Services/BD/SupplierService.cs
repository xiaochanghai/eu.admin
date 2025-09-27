using EU.Core.Api.MCP.Attributes;
using EU.Core.Api.MCP.Interfaces;
using EU.Core.Api.MCP.Models.Mcp;
using EU.Core.Common.Helper;
using System.ComponentModel;


namespace EU.Core.Api.MCP.Services.BD;

public class InputSchemaArguments()
{
    /// <summary>
    /// 供应商名称
    /// </summary>
    [Description("供应商名称")]
    public string? name { get; set; }

}

public class CreateSupplierFromFileArguments()
{
    /// <summary>
    /// 文件ID
    /// </summary>
    [Description("文件ID")]
    public string? fileId { get; set; }
}


public class UpdateSupplieArguments()
{
    /// <summary>
    /// 文件ID
    /// </summary>
    [Description("文件ID")]
    public string? fileId { get; set; }
}

public class SupplierService : BaseService<SupplierService>, ISupplierService
{
    private readonly string moudleCode = "BD_SUPPLIER_MNG";

    public SupplierService(ILogger<SupplierService> logger) : base(logger)
    {
    }

    #region 获取供应商列表 
    /// <summary>
    /// 获取供应商管理模块的页面代码，用于加载供应商列表界面
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    [McpTool(
    "get_supplier",
    "用于打开供应商列表页面。当用户希望浏览、查看或管理供应商信息时使用。" +
    "此工具会返回前端模块标识 'BD_SUPPLIER_MNG'，客户端应据此加载对应的表格组件或跳转至管理界面。" +
    "注意：这是一个页面导航操作，即使之前已调用过，只要用户再次提出查看请求，也应重新调用本工具。")]
    public McpToolResult GetSupplier(object arguments)
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
    public McpToolResult CreateSupplier(object arguments)
    {
        return new McpToolResult
        {
            Content =
            [
                new McpContent
                {
                    Type = "text",
                    Text = JsonHelper.ObjToJson(new
                    {
                        type = "module_edit",
                        moudleCode
                    })
                }
            ]
        };
    }
    #endregion

    #region 导入供应商 
    /// <summary>
    /// 导入供应商，根据传进来的fileId
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    [McpTool(
    "create_supplier_from_file",
    @"工具名称：create_supplier_from_file

功能描述：
仅当用户明确表示希望通过**已上传的文件**（如 Excel、CSV 等）**批量或自动创建新供应商**时，才调用此工具。  
该工具会根据提供的文件标识（fileId）解析文件内容，并在系统中创建对应的供应商记录。

输入参数：
- fileId（字符串，必填）：由文件上传服务返回的、已成功上传的文件唯一标识。

触发条件（必须同时满足）：
- 用户意图是**创建/新增/导入**供应商（而非查看、查询、获取、编辑）；
- 用户明确提及**文件、上传、导入、批量、模板、Excel、CSV**等关键词；
- 用户提供了或系统上下文中存在有效的 fileId。

行为说明：
- 工具将解析文件并校验数据（如必填字段、唯一性约束等）；
- 若校验失败（如格式错误、缺少字段、编码重复等），返回具体错误原因；
- 创建成功后，返回新供应商 ID 及前端跳转模块（如 BD_SUPPLIER_DETAIL）。

注意事项：
- ❗ 本工具为**数据写入操作**，不可用于查询、查看或获取已有供应商信息；
- ❗ 若用户仅说“获取供应商”“查看供应商列表”“查一下供应商”，**绝对不应调用此工具**；
- ❗ 请勿与仅用于打开表单页面的 create_supplier 或用于查询的 get_supplier 工具混淆；
- 调用前必须确认 fileId 有效且用户有权限访问该文件。",
            typeof(CreateSupplierFromFileArguments))]

    public McpToolResult CreateSupplierFromFile(object arguments)
    {
        return new McpToolResult
        {
            Content =
            [
                new McpContent
                {
                    Type = "text",
                    Text = JsonHelper.ObjToJson(new
                    {
                        type = "module_edit",
                        id = "a151f47a-92b8-4f81-8021-e2b1c3ad651a",
                        moudleCode
                    })
                }
            ]
        };
    }
    #endregion

    #region 修改供应商 
    /// <summary>
    /// 修改供应商，根据传进来的Id
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    [McpTool(
    "update_supplier",
    @"工具名称：update_supplier

功能描述：  
用于修改**已存在的供应商**信息。仅当用户明确表示要**编辑、更新或修改某个具体供应商**的数据（如名称、联系人、地址、银行信息等）时调用。  
系统将根据提供的 supplierId（系统唯一ID）或 supplierNo（业务供应商编号）定位目标供应商，并打开对应的编辑表单页面，预加载当前数据。

输入参数（至少提供其一）：  
- supplierId（字符串，可选）：系统生成的供应商唯一标识（如 ""6faa0538-1c99-433a-a4f3-6c7f40ffd4c2""）；  
- supplierNo（字符串，可选）：用户定义的供应商业务编号（如 ""VENDOR-2024-001""）。  

> ⚠️ 注意：supplierId 和 supplierNo 至少需提供一个。若两者同时提供，优先使用 supplierId。

触发条件（必须满足）：  
- 用户意图是**编辑、修改、更新**供应商（非创建、查询、删除）；  
- 用户明确指定了**某个供应商**，并通过 ID、编号、名称等提供了可识别信息；  
- 系统能从中解析出有效的 supplierId 或 supplierNo。

行为说明：  
- 工具返回前端模块标识 'BD_SUPPLIER_MNG'；  
- 客户端应加载供应商编辑表单，并根据传入的标识符自动查询并填充对应供应商的当前数据；  
- 实际数据保存由前端表单提交触发，本工具仅负责页面导航与数据预加载。

注意事项：  
- ❗ 本工具为**页面导航操作**，不直接修改数据库；  
- ❗ 若用户仅说“修改供应商”但未指定具体对象，应引导其提供供应商编号或名称，**不得盲目调用**；  
- ❗ 若用户意图是“创建新供应商”或“从文件导入”，应调用 create_supplier 或 create_supplier_from_file；  
- ❗ 若用户仅想“查看”供应商信息，应调用 get_supplier 类工具，而非本工具。",
            typeof(UpdateSupplieArguments))]

    public McpToolResult update_supplier(object arguments)
    {
        return new McpToolResult
        {
            Content =
            [
                new McpContent
                {
                    Type = "text",
                    Text = JsonHelper.ObjToJson(new
                    {
                        type = "module_edit",
                        id = "a151f47a-92b8-4f81-8021-e2b1c3ad651a",
                        moudleCode
                    })
                }
            ]
        };
    }
    #endregion

    //[McpTool("test_hello_Supplier3", "A simple test supplier tool that says hello", typeof(InputSchemaArguments))]
    //public async Task<McpToolResult> HandleTestHello2(object arguments)
    //{
    //    var aaqq = JsonHelper.JsonToObj<InputSchemaArguments>(JsonHelper.ObjToJson(arguments));

    //    //if (arguments.ValueKind != JsonValueKind.Undefined &&
    //    //    arguments.TryGetProperty("name", out var nameProperty))
    //    //{
    //    //    name = nameProperty.GetString() ?? "World";
    //    //}

    //    return new McpToolResult
    //    {
    //        Content = new[]
    //        {
    //            new McpContent
    //            {
    //                Type = "text",
    //                Text = $"Hello, {aaqq.name},{DateTime.Now}! Supplier MCP server is working! "
    //            }
    //        }
    //    };
    //}

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