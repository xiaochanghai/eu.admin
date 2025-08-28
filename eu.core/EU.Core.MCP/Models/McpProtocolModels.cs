using System.Text.Json.Serialization;

namespace EU.Core.MCP.Models;

/// <summary>
/// MCP 协议版本
/// </summary>
public static class McpProtocolVersion
{
    public const string V2024_11_05 = "2024-11-05";
    public const string V2024_11_05_BETA = "2024-11-05-beta";
}

/// <summary>
/// MCP 初始化请求
/// </summary>
public class McpInitializeRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("method")]
    public string Method { get; set; } = "initialize";
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("params")]
    public McpInitializeParams Params { get; set; } = new();
}

/// <summary>
/// MCP 初始化参数
/// </summary>
public class McpInitializeParams
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = string.Empty;
    
    [JsonPropertyName("capabilities")]
    public McpClientCapabilities Capabilities { get; set; } = new();
    
    [JsonPropertyName("clientInfo")]
    public McpClientInfo? ClientInfo { get; set; }
}

/// <summary>
/// MCP 客户端能力
/// </summary>
public class McpClientCapabilities
{
    [JsonPropertyName("roots")]
    public McpRootsCapability? Roots { get; set; }
    
    [JsonPropertyName("sampling")]
    public object? Sampling { get; set; }
    
    [JsonPropertyName("tools")]
    public object? Tools { get; set; }
    
    [JsonPropertyName("resources")]
    public object? Resources { get; set; }
    
    [JsonPropertyName("prompts")]
    public object? Prompts { get; set; }
}

/// <summary>
/// MCP 根能力
/// </summary>
public class McpRootsCapability
{
    [JsonPropertyName("listChanged")]
    public bool ListChanged { get; set; }
}

/// <summary>
/// MCP 客户端信息
/// </summary>
public class McpClientInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// MCP 初始化响应
/// </summary>
public class McpInitializeResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("result")]
    public McpInitializeResult Result { get; set; } = new();
}

/// <summary>
/// MCP 初始化结果
/// </summary>
public class McpInitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = McpProtocolVersion.V2024_11_05;
    
    [JsonPropertyName("capabilities")]
    public McpServerCapabilities Capabilities { get; set; } = new();
    
    [JsonPropertyName("serverInfo")]
    public McpServerInfo ServerInfo { get; set; } = new();
}

/// <summary>
/// MCP 服务器能力
/// </summary>
public class McpServerCapabilities
{
    [JsonPropertyName("roots")]
    public McpRootsCapability? Roots { get; set; }
    
    [JsonPropertyName("sampling")]
    public object? Sampling { get; set; }
    
    [JsonPropertyName("tools")]
    public object? Tools { get; set; }
    
    [JsonPropertyName("resources")]
    public object? Resources { get; set; }
    
    [JsonPropertyName("prompts")]
    public object? Prompts { get; set; }
}

/// <summary>
/// MCP 服务器信息
/// </summary>
public class McpServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "EU.Core FastMCP Server";
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
}

/// <summary>
/// MCP 工具列表请求
/// </summary>
public class McpToolsListRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("method")]
    public string Method { get; set; } = "tools/list";
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

/// <summary>
/// MCP 工具列表响应
/// </summary>
public class McpToolsListResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("result")]
    public McpToolsListResult Result { get; set; } = new();
}

/// <summary>
/// MCP 工具列表结果
/// </summary>
public class McpToolsListResult
{
    [JsonPropertyName("tools")]
    public List<McpTool> Tools { get; set; } = new();
}

/// <summary>
/// MCP 工具调用请求
/// </summary>
public class McpToolsCallRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("method")]
    public string Method { get; set; } = "tools/call";
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("params")]
    public McpToolsCallParams Params { get; set; } = new();
}

/// <summary>
/// MCP 工具调用参数
/// </summary>
public class McpToolsCallParams
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("arguments")]
    public object? Arguments { get; set; }
}

/// <summary>
/// MCP 工具调用响应
/// </summary>
public class McpToolsCallResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("result")]
    public McpToolsCallResult Result { get; set; } = new();
}

/// <summary>
/// MCP 工具调用结果
/// </summary>
public class McpToolsCallResult
{
    [JsonPropertyName("content")]
    public List<McpContent> Content { get; set; } = new();
    
    [JsonPropertyName("isError")]
    public bool IsError { get; set; }
}

/// <summary>
/// MCP 内容
/// </summary>
public class McpContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";
    
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// MCP 资源列表请求
/// </summary>
public class McpResourcesListRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("method")]
    public string Method { get; set; } = "resources/list";
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

/// <summary>
/// MCP 资源列表响应
/// </summary>
public class McpResourcesListResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("result")]
    public McpResourcesListResult Result { get; set; } = new();
}

/// <summary>
/// MCP 资源列表结果
/// </summary>
public class McpResourcesListResult
{
    [JsonPropertyName("resources")]
    public List<McpResource> Resources { get; set; } = new();
}

/// <summary>
/// MCP 资源读取请求
/// </summary>
public class McpResourcesReadRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("method")]
    public string Method { get; set; } = "resources/read";
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("params")]
    public McpResourcesReadParams Params { get; set; } = new();
}

/// <summary>
/// MCP 资源读取参数
/// </summary>
public class McpResourcesReadParams
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
}

/// <summary>
/// MCP 资源读取响应
/// </summary>
public class McpResourcesReadResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("result")]
    public McpResourcesReadResult Result { get; set; } = new();
}

/// <summary>
/// MCP 资源读取结果
/// </summary>
public class McpResourcesReadResult
{
    [JsonPropertyName("contents")]
    public List<McpContent> Contents { get; set; } = new();
    
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;
} 

/// <summary>
/// MCP 通知基础模型
/// </summary>
public class McpNotification
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
    
    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

/// <summary>
/// MCP 初始化完成通知
/// </summary>
public class McpInitializedNotification : McpNotification
{
    public McpInitializedNotification()
    {
        Method = "notifications/initialized";
        Params = new { };
    }
} 