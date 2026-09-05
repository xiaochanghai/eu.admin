using EU.Core.IServices.Skills;

#nullable enable

namespace EU.Core.IServices;

#region 文件职责：IAgSkillDefinitionServices 服务契约

/// <summary>
/// Skill 定义及生命周期服务。
/// </summary>
public interface IAgSkillDefinitionServices : IBaseServices<AgSkillDefinition>
{
    /// <summary>创建技能定义。</summary>
    Task<ServiceResult<SkillDefinition>> CreateAsync(CreateSkillCommand command, CancellationToken cancellationToken = default);

    /// <summary>获取技能定义。</summary>
    Task<SkillDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>查询技能定义列表。</summary>
    Task<IReadOnlyList<SkillListItem>> ListAsync(SkillQuery query, CancellationToken cancellationToken = default);

    /// <summary>更新技能定义。</summary>
    Task<ServiceResult<SkillDefinition>> UpdateAsync(UpdateSkillCommand command, CancellationToken cancellationToken = default);

    /// <summary>保存技能草稿文件。</summary>
    Task<ServiceResult<SkillDefinition>> SaveFileAsync(SaveSkillFileCommand command, CancellationToken cancellationToken = default);

    /// <summary>删除技能草稿文件。</summary>
    Task<ServiceResult<SkillDefinition>> DeleteFileAsync(DeleteSkillFileCommand command, CancellationToken cancellationToken = default);

    /// <summary>发布技能定义。</summary>
    Task<ServiceResult<SkillDefinition>> PublishAsync(PublishSkillCommand command, CancellationToken cancellationToken = default);

    /// <summary>查询技能草稿文件列表。</summary>
    Task<ServiceResult<IReadOnlyList<SkillFileEntry>>> ListFilesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>设置技能定义的归档状态。</summary>
    Task<ServiceResult<SkillDefinition>> SetArchivedAsync(SetSkillArchiveCommand command, CancellationToken cancellationToken = default);

    /// <summary>读取技能草稿文件内容。</summary>
    Task<ServiceResult<string>> ReadFileAsync(Guid id, string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据受控 Skill 文件目录和发布清单重建附件路径索引。
    /// </summary>
    Task ReconcileFileAttachmentsAsync(CancellationToken cancellationToken = default);
}

#endregion
