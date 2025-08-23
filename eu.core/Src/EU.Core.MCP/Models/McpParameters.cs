namespace EU.Core.MCP.Models;

/// <summary>
/// 数据库查询参数
/// </summary>
public class DatabaseQueryParams
{
    public string Table { get; set; } = string.Empty;
    public string[] Fields { get; set; } = Array.Empty<string>();
    public string Where { get; set; } = string.Empty;
    public int Limit { get; set; } = 100;
}

/// <summary>
/// 文件操作参数
/// </summary>
public class FileOperationParams
{
    public string Operation { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Content { get; set; }
}

/// <summary>
/// 业务逻辑参数
/// </summary>
public class BusinessLogicParams
{
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public object? Data { get; set; }
}

/// <summary>
/// 工具调用参数
/// </summary>
public class ToolCallParams
{
    public string Name { get; set; } = string.Empty;
    public object Arguments { get; set; } = new();
}

/// <summary>
/// 资源读取参数
/// </summary>
public class ResourceReadParams
{
    public string Uri { get; set; } = string.Empty;
} 