using System.Text.Json.Serialization;

namespace EU.Core.MCP.Attributes;

/// <summary>
/// MCP 工具装饰器 - 类似 FastMCP 的 @mcp.tool
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class McpToolAttribute : Attribute
{
    public string Name { get; set; }
    public string Description { get; set; }
    public object InputSchema { get; set; }
    
    public McpToolAttribute(string name = "", string description = "")
    {
        Name = name;
        Description = description;
    }
}

/// <summary>
/// MCP 资源装饰器 - 类似 FastMCP 的 @mcp.resource
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class McpResourceAttribute : Attribute
{
    public string Uri { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string MimeType { get; set; } = "application/json";
    
    public McpResourceAttribute(string uri, string name = "", string description = "")
    {
        Uri = uri;
        Name = name;
        Description = description;
    }
}

/// <summary>
/// MCP 提示装饰器 - 类似 FastMCP 的 @mcp.prompt
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class McpPromptAttribute : Attribute
{
    public string Name { get; set; }
    public string Description { get; set; }
    
    public McpPromptAttribute(string name = "", string description = "")
    {
        Name = name;
        Description = description;
    }
} 