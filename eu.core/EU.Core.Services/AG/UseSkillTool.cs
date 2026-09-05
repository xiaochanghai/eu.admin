using EU.Core.IServices.UnifiedEntry;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.Skills;

#nullable enable

namespace EU.Core.Services;

// 文件职责：UseSkillTool 职责实现

/// <summary>
/// 实现加载并使用已发布技能的内部工具。
/// </summary>
public sealed class UseSkillTool : IAgentInternalTool
{
    #region 技能工具执行

    private const int MaximumTaskCharacters = 32_768;
    private const int MaximumReasonCharacters = 1_024;
    private readonly IReadOnlyDictionary<Guid, PublishedSkillContent> _skills;

    #region 构造（UseSkillTool）
    /// <summary>
    /// 构造（UseSkillTool）
    /// </summary>
    /// <param name="skills">技能服务。</param>
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
    #endregion

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

    #region 调用（InvokeAsync）
    /// <summary>
    /// 调用（InvokeAsync）
    /// </summary>
    /// <param name="argumentsJson">工具调用参数的 JSON 文本。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>授权技能版本的指令文本；参数无效或版本未授权时返回带错误码的失败结果。</returns>
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
    #endregion

    #region 处理（Failure）
    /// <summary>
    /// 处理（Failure）
    /// </summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="content">内部工具失败时对调用方展示的安全提示。</param>
    /// <returns>包含指定内容和错误码、成功标志为 false 的内部工具结果。</returns>
    private static AgentInternalToolResult Failure(string code, string content) =>
        new(false, content, code);
    #endregion

    #endregion
}
