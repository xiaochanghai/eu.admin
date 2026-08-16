using EU.Core.Agent.Application.Skills;
using EU.Core.IServices.BASE;

#nullable enable

namespace EU.Core.IServices;

/// <summary>
/// Skill 定义及生命周期服务。
/// </summary>
public interface IAgSkillDefinitionServices : IBaseServices<AgSkillDefinition>
{
    Task<SkillOperationResult<SkillDefinition>> CreateAsync(
        CreateSkillCommand command,
        CancellationToken cancellationToken = default);

    Task<SkillDefinition?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillListItem>> ListAsync(
        SkillQuery query,
        CancellationToken cancellationToken = default);

    Task<SkillOperationResult<SkillDefinition>> UpdateAsync(
        UpdateSkillCommand command,
        CancellationToken cancellationToken = default);

    Task<SkillOperationResult<SkillDefinition>> SaveFileAsync(
        SaveSkillFileCommand command,
        CancellationToken cancellationToken = default);

    Task<SkillOperationResult<SkillDefinition>> DeleteFileAsync(
        DeleteSkillFileCommand command,
        CancellationToken cancellationToken = default);

    Task<SkillOperationResult<SkillDefinition>> PublishAsync(
        PublishSkillCommand command,
        CancellationToken cancellationToken = default);

    Task<SkillOperationResult<IReadOnlyList<SkillFileEntry>>> ListFilesAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SkillOperationResult<SkillDefinition>> SetArchivedAsync(
        SetSkillArchiveCommand command,
        CancellationToken cancellationToken = default);

    Task<SkillOperationResult<string>> ReadFileAsync(
        Guid id,
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据受控 Skill 文件目录和发布清单重建附件路径索引。
    /// </summary>
    Task ReconcileFileAttachmentsAsync(
        CancellationToken cancellationToken = default);
}
