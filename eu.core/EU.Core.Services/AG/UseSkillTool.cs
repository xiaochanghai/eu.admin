using EU.Core.IServices.UnifiedEntry;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.Skills;

#nullable enable

namespace EU.Core.Services;

#region 文件职责：UseSkillTool 职责实现

/// <summary>
/// 实现加载并使用已发布技能的内部工具。
/// </summary>
public sealed class UseSkillTool : IAgentInternalTool
{
    #region 技能工具执行

    private const int MaximumTaskCharacters = 32_768;
    private const int MaximumReasonCharacters = 1_024;
    private readonly IReadOnlyDictionary<Guid, PublishedSkillContent> _skills;

    public UseSkillTool(IReadOnlyList<PublishedSkillContent> skills)
    {
        ArgumentNullException.ThrowIfNull(skills);
        PublishedSkillContent[] copied = skills
            .Select(SkillContractCloner.Clone)
            .OrderBy(value => value.SkillCode, StringComparer.Ordinal)
            .ThenBy(value => value.SkillVersionId)
            .ToArray();
        _skills = copied.ToDictionary(value => value.SkillVersionId);
        Description =
            "Load one controlled Skill version frozen in the Main Agent publication. "
            + "Use the returned instructions for the current task while obeying Main Agent and platform policy. "
            + "Authorized Skills: "
            + string.Join(
                "; ",
                copied.Select(value =>
                    $"code={value.SkillCode}, name={value.SkillName}, version={value.VersionLabel}, skillVersionId={value.SkillVersionId}"));
        InputSchemaJson = InternalToolSchemaBuilder.Build(
            "skillVersionId",
            copied.Select(value => value.SkillVersionId).ToArray(),
            "task",
            MaximumTaskCharacters,
            MaximumReasonCharacters);
    }

    /// <summary>
    /// 获取内部工具名称。
    /// </summary>
    public string Name => "use_skill";

    /// <summary>
    /// 获取内部工具说明。
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 获取内部工具输入参数的 JSON Schema。
    /// </summary>
    public string InputSchemaJson { get; }

    public Task<AgentInternalToolResult> InvokeAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!InternalToolArgumentParser.TryParse(
                argumentsJson,
                "skillVersionId",
                "task",
                MaximumTaskCharacters,
                MaximumReasonCharacters,
                UnifiedEntryPayloadProtector.InternalPayloadLimitUtf8Bytes,
                out InternalToolArguments arguments))
        {
            return Task.FromResult(Failure(
                UnifiedEntryErrorCodes.InternalArgumentsInvalid,
                "The use_skill arguments are invalid."));
        }

        if (!_skills.TryGetValue(
                arguments.VersionId,
                out PublishedSkillContent? selected))
        {
            return Task.FromResult(Failure(
                UnifiedEntryErrorCodes.SkillVersionUnauthorized,
                "The requested Skill version is not authorized by the frozen Main Agent publication."));
        }

        return Task.FromResult(new AgentInternalToolResult(
            true,
            selected.Instructions,
            string.Empty));
    }

    private static AgentInternalToolResult Failure(string code, string content) =>
        new(false, content, code);

    #endregion
}

#endregion
