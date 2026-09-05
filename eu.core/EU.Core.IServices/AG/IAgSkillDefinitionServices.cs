using EU.Core.IServices.Skills;

#nullable enable

namespace EU.Core.IServices;

// 文件职责：IAgSkillDefinitionServices 服务契约

/// <summary>
/// Skill 定义及生命周期服务。
/// </summary>
public interface IAgSkillDefinitionServices : IBaseServices<AgSkillDefinition>
{
    #region 创建技能定义。
    /// <summary>创建技能定义。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<SkillDefinition>> CreateAsync(CreateSkillCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 获取技能定义。
    /// <summary>获取技能定义。</summary>
    /// <param name="id">技能标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>包含已发布版本及文件清单的技能定义；不存在时为 null。</returns>
    Task<SkillDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    #endregion

    #region 查询技能定义列表。
    /// <summary>查询技能定义列表。</summary>
    /// <param name="query">查询筛选条件。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>匹配搜索、分类和状态条件的技能摘要集合；未指定状态时排除已归档技能。</returns>
    Task<IReadOnlyList<SkillListItem>> ListAsync(SkillQuery query, CancellationToken cancellationToken = default);
    #endregion

    #region 更新技能定义。
    /// <summary>更新技能定义。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<SkillDefinition>> UpdateAsync(UpdateSkillCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 保存技能草稿文件。
    /// <summary>保存技能草稿文件。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<SkillDefinition>> SaveFileAsync(SaveSkillFileCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 删除技能草稿文件。
    /// <summary>删除技能草稿文件。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<SkillDefinition>> DeleteFileAsync(DeleteSkillFileCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 发布技能定义。
    /// <summary>发布技能定义。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<SkillDefinition>> PublishAsync(PublishSkillCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 查询技能草稿文件列表。
    /// <summary>查询技能草稿文件列表。</summary>
    /// <param name="id">技能标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能文件条目集合，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<IReadOnlyList<SkillFileEntry>>> ListFilesAsync(Guid id, CancellationToken cancellationToken = default);
    #endregion

    #region 设置技能定义的归档状态。
    /// <summary>设置技能定义的归档状态。</summary>
    /// <param name="command">当前业务操作的命令参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能定义，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<SkillDefinition>> SetArchivedAsync(SetSkillArchiveCommand command, CancellationToken cancellationToken = default);
    #endregion

    #region 读取技能草稿文件内容。
    /// <summary>读取技能草稿文件内容。</summary>
    /// <param name="id">技能标识。</param>
    /// <param name="relativePath">相对于存储根目录的文件路径。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含技能文件文本内容，失败时包含错误状态和提示。</returns>
    Task<ServiceResult<string>> ReadFileAsync(Guid id, string relativePath, CancellationToken cancellationToken = default);
    #endregion

    #region 根据受控 Skill 文件目录和发布清单重建附件路径索引。
    /// <summary>
    /// 根据受控 Skill 文件目录和发布清单重建附件路径索引。
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    Task ReconcileFileAttachmentsAsync(CancellationToken cancellationToken = default);
    #endregion
}
