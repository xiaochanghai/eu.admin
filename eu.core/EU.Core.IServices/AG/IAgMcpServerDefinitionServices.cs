using EU.Core.IServices.Mcp;
using EU.Core.IServices.BASE;

#nullable enable

namespace EU.Core.IServices;

#region 文件职责：IAgMcpServerDefinitionServices 服务契约

/// <summary>
/// 定义 MCP 服务及工具版本的持久化服务。
/// </summary>
public interface IAgMcpServerDefinitionServices : IBaseServices<AgMcpServerDefinition>
{
    /// <summary>创建MCP 服务定义。</summary>
    Task<ServiceResult<McpServerDefinition>> CreateAsync(CreateMcpServerCommand command, CancellationToken cancellationToken = default);

    /// <summary>获取MCP 服务定义。</summary>
    Task<McpServerDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>查询MCP 服务定义列表。</summary>
    Task<IReadOnlyList<McpServerDefinition>> ListAsync(McpServerQuery query, CancellationToken cancellationToken = default);

    /// <summary>更新MCP 服务定义。</summary>
    Task<ServiceResult<McpServerDefinition>> UpdateAsync(UpdateMcpServerCommand command, CancellationToken cancellationToken = default);

    /// <summary>同步 MCP 服务提供的工具定义。</summary>
    Task<ServiceResult<McpServerDefinition>> SyncAsync(SyncMcpServerCommand command, CancellationToken cancellationToken = default);

    /// <summary>设置 MCP 工具的风险等级。</summary>
    Task<ServiceResult<McpServerDefinition>> ClassifyToolAsync(ClassifyMcpToolCommand command, CancellationToken cancellationToken = default);

    /// <summary>设置MCP 服务定义的归档状态。</summary>
    Task<ServiceResult<McpServerDefinition>> SetArchivedAsync(SetMcpServerArchiveCommand command, CancellationToken cancellationToken = default);
}

#endregion
