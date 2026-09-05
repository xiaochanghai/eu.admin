#nullable enable

using EU.Core.IServices.Runtime;

namespace EU.Core.IServices.Orchestration;

/// <summary>
/// 编排执行过程中各类载荷的字符数限制。
/// </summary>
/// <param name="NodeInputCharacters">单个节点输入允许的最大字符数。</param>
/// <param name="NodeOutputCharacters">单个节点输出允许的最大字符数。</param>
/// <param name="ToolArgumentsCharacters">单次工具调用参数允许的最大字符数。</param>
/// <param name="ToolResultCharacters">单次工具结果允许的最大字符数。</param>
/// <param name="FinalOutputCharacters">编排最终输出允许的最大字符数。</param>
public sealed record ExecutionPayloadLimits(
    int NodeInputCharacters = 131_072,
    int NodeOutputCharacters = 262_144,
    int ToolArgumentsCharacters = 262_144,
    int ToolResultCharacters = 1_048_576,
    int FinalOutputCharacters = 262_144);

/// <summary>
/// 编排节点中的工具调用记录。
/// </summary>
/// <param name="ToolCallId">工具调用标识。</param>
/// <param name="AgentRunId">Agent 运行标识。</param>
/// <param name="ToolVersionId">工具版本标识。</param>
/// <param name="ToolName">工具名称。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="ArgumentsJson">工具调用参数 JSON。</param>
/// <param name="ResultContent">工具返回的内容。</param>
/// <param name="ResultSha256">工具结果的 SHA-256 摘要。</param>
/// <param name="ResultCharacters">工具结果的字符数量。</param>
/// <param name="StartedAtUtc">执行开始的 UTC 时间。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
public sealed record OrchestrationToolCallRecord(
    Guid ToolCallId,
    Guid AgentRunId,
    Guid ToolVersionId,
    string ToolName,
    AgentRunEventKind Status,
    string ArgumentsJson,
    string ResultContent,
    string ResultSha256,
    int ResultCharacters,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string ErrorCode);

/// <summary>
/// 编排节点单次执行尝试的明细。
/// </summary>
/// <param name="NodeId">编排节点标识。</param>
/// <param name="Attempt">当前执行尝试序号。</param>
/// <param name="AgentRunId">Agent 运行标识。</param>
/// <param name="Input">运行、任务或节点的输入内容。</param>
/// <param name="InputSha256">输入内容的 SHA-256 摘要。</param>
/// <param name="Output">运行或节点产生的输出内容。</param>
/// <param name="OutputSha256">输出内容的 SHA-256 摘要。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="StartedAtUtc">执行开始的 UTC 时间。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
/// <param name="ToolCalls">工具调用记录集合。</param>
public sealed record OrchestrationNodeAttemptRecord(
    string NodeId,
    int Attempt,
    Guid AgentRunId,
    string Input,
    string InputSha256,
    string Output,
    string OutputSha256,
    OrchestrationNodeRunStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string ErrorCode,
    IReadOnlyList<OrchestrationToolCallRecord> ToolCalls);

/// <summary>
/// 编排运行的输入、输出及节点尝试详情。
/// </summary>
/// <param name="RunId">运行标识。</param>
/// <param name="OrchestrationId">编排定义标识。</param>
/// <param name="Input">运行、任务或节点的输入内容。</param>
/// <param name="Output">运行或节点产生的输出内容。</param>
/// <param name="Attempts">执行尝试次数或尝试明细集合。</param>
public sealed record OrchestrationRunDetails(
    Guid RunId,
    Guid OrchestrationId,
    string Input,
    string Output,
    IReadOnlyList<OrchestrationNodeAttemptRecord> Attempts);
