using EU.Core.IServices.Mcp;
using EU.Core.IServices.BASE;

#nullable enable

namespace EU.Core.IServices;

// 文件职责：IAgMcpServerDefinitionServices 服务契约

/// <summary>
/// 定义 MCP 服务及工具版本的持久化服务。
/// </summary>
public interface IAgMcpServerDefinitionServices : IBaseServices<AgMcpServerDefinition>
{
    #region 创建MCP 服务定义。
    /// <summary>创建MCP 服务定义。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<McpServerDefinition>> CreateAsync(CreateMcpServerCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 获取MCP 服务定义。
    /// <summary>获取MCP 服务定义。</summary>
    /// <param name="id">MCP 服务标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>包含参数及工具版本历史的 MCP 服务定义；不存在时为 null。</returns>
    Task<McpServerDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    #endregion

    #region 查询MCP 服务定义列表。
    /// <summary>查询MCP 服务定义列表。</summary>
    /// <param name="query">查询筛选条件。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>匹配搜索和状态条件的完整 MCP 服务定义，按编码及标识排序；未指定状态时排除已归档服务。</returns>
    Task<IReadOnlyList<McpServerDefinition>> ListAsync(McpServerQuery query, CancellationToken cancellationToken = default);
    #endregion

    #region 更新MCP 服务定义。
    /// <summary>更新MCP 服务定义。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<McpServerDefinition>> UpdateAsync(UpdateMcpServerCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 同步 MCP 服务提供的工具定义。
    /// <summary>同步 MCP 服务提供的工具定义。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<McpServerDefinition>> SyncAsync(SyncMcpServerCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 设置 MCP 工具的风险等级。
    /// <summary>设置 MCP 工具的风险等级。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<McpServerDefinition>> ClassifyToolAsync(ClassifyMcpToolCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 设置MCP 服务定义的归档状态。
    /// <summary>设置MCP 服务定义的归档状态。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含MCP 服务定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<McpServerDefinition>> SetArchivedAsync(SetMcpServerArchiveCommand command, CancellationToken cancellationToken = default);
    #endregion
}
