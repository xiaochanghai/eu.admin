namespace EU.Core.Api.MCP.Attributes;

/// <summary>
/// MCP 工具装饰器 - 类似 FastMCP 的 @mcp.tool
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class McpToolAttribute : Attribute
{
    public string Name { get; set; }
    public string Description { get; set; }
    public object? InputSchema { get; set; }

    public McpToolAttribute(string name = "", string description = "")
    {
        Name = name;
        Description = description;
    }
}
