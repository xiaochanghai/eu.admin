#nullable enable

using System.Collections.ObjectModel;
using EU.Core.IServices.Agents;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.Model;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Knowledge;
using EU.Core.IServices.Skills;
using EU.Core.IServices.Approvals;

namespace EU.Core.IServices.Runtime;

/// <summary>
/// Agent 运行的状态。
/// </summary>
public enum AgentRunStatus
{
    /// <summary>正在运行。</summary>
    Running,
    /// <summary>等待工具调用审批。</summary>
    WaitingForApproval,
    /// <summary>已完成。</summary>
    Completed,
    /// <summary>运行失败。</summary>
    Failed,
    /// <summary>已取消。</summary>
    Cancelled
}

/// <summary>
/// Agent 运行过程中产生的事件类型。
/// </summary>
public enum AgentRunEventKind
{
    /// <summary>运行已开始。</summary>
    Started,
    /// <summary>技能执行已开始。</summary>
    SkillStarted,
    /// <summary>已完成知识检索。</summary>
    KnowledgeRetrieved,
    /// <summary>产生增量输出。</summary>
    Delta,
    /// <summary>产生引用信息。</summary>
    Citation,
    /// <summary>工具调用已开始。</summary>
    ToolStarted,
    /// <summary>工具调用成功。</summary>
    ToolSucceeded,
    /// <summary>工具调用被策略阻止。</summary>
    ToolBlocked,
    /// <summary>工具调用失败。</summary>
    ToolFailed,
    /// <summary>工具调用需要审批。</summary>
    ApprovalRequired,
    /// <summary>运行已完成。</summary>
    Completed,
    /// <summary>运行失败。</summary>
    Failed,
    /// <summary>运行已取消。</summary>
    Cancelled
}

/// <summary>
/// Agent 会话消息的参与方角色。
/// </summary>
public enum AgentConversationRole
{
    /// <summary>用户。</summary>
    User,
    /// <summary>Agent 助手。</summary>
    Assistant
}

/// <summary>
/// 提供给 Agent 的会话消息。
/// </summary>
/// <param name="Role">会话消息的参与方角色。</param>
/// <param name="Content">消息或工具返回内容。</param>
public sealed record AgentConversationMessage(
    AgentConversationRole Role,
    string Content);

/// <summary>
/// Agent 运行过程中产生的事件。
/// </summary>
/// <param name="RunId">运行标识。</param>
/// <param name="Sequence">运行事件的顺序号。</param>
/// <param name="Kind">事件类型。</param>
/// <param name="OccurredAtUtc">事件发生的 UTC 时间。</param>
/// <param name="Text">事件携带的文本内容。</param>
/// <param name="ToolVersionId">工具版本标识。</param>
/// <param name="ToolName">工具名称。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
/// <param name="ToolCallId">工具调用标识。</param>
/// <param name="SkillVersionId">技能版本标识。</param>
/// <param name="SkillName">技能名称。</param>
public sealed record AgentRunEvent(
    Guid RunId,
    long Sequence,
    AgentRunEventKind Kind,
    DateTimeOffset OccurredAtUtc,
    string Text = "",
    Guid? ToolVersionId = null,
    string ToolName = "",
    string ErrorCode = "",
    Guid? ToolCallId = null,
    Guid? SkillVersionId = null,
    string SkillName = "")
{
    /// <summary>
    /// 工具调用参数的 JSON 内容。
    /// </summary>
    public string ArgumentsJson { get; init; } = "";

    /// <summary>
    /// 关联的审批请求标识。
    /// </summary>
    public Guid? ApprovalId { get; init; }

    /// <summary>
    /// 参与检索的知识库数量。
    /// </summary>
    public int KnowledgeBaseCount { get; init; }

    /// <summary>
    /// 知识检索命中数量。
    /// </summary>
    public int KnowledgeHitCount { get; init; }
}

/// <summary>
/// 表示 Agent 运行过程中的领域异常。
/// </summary>
/// <param name="errorCode">用于标识失败原因的领域错误码。</param>
/// <param name="message">描述异常原因的错误消息。</param>
public sealed class AgentRuntimeException(string errorCode, string message)
    : Exception(message)
{
    /// <summary>
    /// 获取领域异常对应的错误码。
    /// </summary>
    public string ErrorCode { get; } = errorCode;
}

/// <summary>
/// Agent 运行的审计记录。
/// </summary>
/// <param name="RunId">运行标识。</param>
/// <param name="AgentId">Agent 标识。</param>
/// <param name="AgentVersionId">Agent 版本标识。</param>
/// <param name="AgentCode">Agent 业务编码。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="StartedAtUtc">执行开始的 UTC 时间。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
/// <param name="InputSha256">输入内容的 SHA-256 摘要。</param>
/// <param name="OutputCharacters">输出内容的字符数量。</param>
/// <param name="ToolCallCount">工具调用总次数。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
/// <param name="ToolCalls">工具调用记录集合。</param>
public sealed record AgentRunAuditRecord(
    Guid RunId,
    Guid AgentId,
    Guid AgentVersionId,
    string AgentCode,
    AgentRunStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string InputSha256,
    int OutputCharacters,
    int ToolCallCount,
    string ErrorCode,
    IReadOnlyList<AgentToolCallAuditRecord> ToolCalls);

/// <summary>
/// Agent 工具调用的审计记录。
/// </summary>
/// <param name="ToolVersionId">工具版本标识。</param>
/// <param name="ToolName">工具名称。</param>
/// <param name="Risk">工具风险等级。</param>
/// <param name="Status">当前运行或生命周期状态。</param>
/// <param name="StartedAtUtc">执行开始的 UTC 时间。</param>
/// <param name="FinishedAtUtc">执行结束的 UTC 时间。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
public sealed record AgentToolCallAuditRecord(
    Guid ToolVersionId,
    string ToolName,
    McpToolRisk Risk,
    AgentRunEventKind Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    string ErrorCode);

/// <summary>
/// 执行 Agent 所需的运行上下文。
/// </summary>
/// <param name="RunId">运行标识。</param>
/// <param name="AgentId">Agent 标识。</param>
/// <param name="Snapshot">执行或发布使用的不可变版本快照。</param>
/// <param name="Input">运行、任务或节点的输入内容。</param>
/// <param name="InputSha256">输入内容的 SHA-256 摘要。</param>
/// <param name="StartedAtUtc">执行开始的 UTC 时间。</param>
/// <param name="Tools">运行可使用的 MCP 工具集合。</param>
public sealed record AgentRunContext(
    Guid RunId,
    Guid AgentId,
    AgentVersionSnapshot Snapshot,
    string Input,
    string InputSha256,
    DateTimeOffset StartedAtUtc,
    IReadOnlyList<PublishedMcpToolReference> Tools)
{
    /// <summary>
    /// 当前运行加载的技能集合。
    /// </summary>
    public IReadOnlyList<PublishedSkillContent> Skills { get; init; } =
        SkillContractCloner.ReadOnly(Array.Empty<PublishedSkillContent>());

    /// <summary>
    /// 当前运行可用的知识检索器。
    /// </summary>
    public IReadOnlyList<KnowledgeSearchResult> Knowledge { get; init; } =
        Array.AsReadOnly(Array.Empty<KnowledgeSearchResult>());

    /// <summary>
    /// 提供给模型的会话历史。
    /// </summary>
    public IReadOnlyList<AgentConversationMessage> ConversationHistory { get; init; } =
        new ReadOnlyCollection<AgentConversationMessage>(
            Array.Empty<AgentConversationMessage>());

    /// <summary>
    /// 当前运行可用的内部工具集合。
    /// </summary>
    public IReadOnlyList<IAgentInternalTool> InternalTools { get; init; } =
        Array.Empty<IAgentInternalTool>();

    /// <summary>
    /// MCP 调用配额守卫。
    /// </summary>
    public IAgentMcpCallGuard? McpCallGuard { get; init; }

    /// <summary>
    /// MCP 调用结果守卫。
    /// </summary>
    public IAgentMcpResultGuard? McpResultGuard { get; init; }

    /// <summary>
    /// 各 MCP 工具的调用限制。
    /// </summary>
    public IReadOnlyList<AgentMcpToolCallLimit> McpToolCallLimits { get; init; } =
        Array.Empty<AgentMcpToolCallLimit>();

    /// <summary>
    /// 当前执行身份信息。
    /// </summary>
    public AgentExecutionIdentity? ExecutionIdentity { get; init; }

    /// <summary>
    /// 工具审批绑定信息。
    /// </summary>
    public AgentToolApprovalBinding? ToolApprovalBinding { get; init; }

    /// <summary>
    /// 工具审批处理器。
    /// </summary>
    public IAgentToolApprovalHandler? ToolApprovalHandler { get; init; }
}

/// <summary>
/// 单个 MCP 工具的调用次数限制。
/// </summary>
/// <param name="ToolVersionId">工具版本标识。</param>
/// <param name="MaximumCalls">允许调用该工具的最大次数。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
/// <param name="Message">面向调用方的错误说明。</param>
public sealed record AgentMcpToolCallLimit(
    Guid ToolVersionId,
    int MaximumCalls,
    string ErrorCode,
    string Message);

/// <summary>
/// 工具审批与统一入口运行的关联信息。
/// </summary>
/// <param name="ConversationId">关联会话标识。</param>
/// <param name="EntryRunId">统一入口运行标识。</param>
public sealed record AgentToolApprovalBinding(
    Guid ConversationId,
    Guid EntryRunId);

/// <summary>
/// Agent 运行时发起的工具审批请求。
/// </summary>
/// <param name="Binding">审批请求的运行关联信息。</param>
/// <param name="AgentRunId">Agent 运行标识。</param>
/// <param name="AgentVersionId">Agent 版本标识。</param>
/// <param name="Tool">申请调用的已发布 MCP 工具。</param>
/// <param name="ArgumentsJson">工具调用参数 JSON。</param>
/// <param name="Requester">发起调用的执行身份。</param>
public sealed record AgentToolApprovalRequest(
    AgentToolApprovalBinding Binding,
    Guid AgentRunId,
    Guid AgentVersionId,
    PublishedMcpToolReference Tool,
    string ArgumentsJson,
    AgentExecutionIdentity Requester);

/// <summary>
/// 定义 Agent 工具调用的审批处理能力。
/// </summary>
public interface IAgentToolApprovalHandler
{
    #region 创建或处理 Agent 工具审批请求。
    /// <summary>创建或处理 Agent 工具审批请求。</summary>
    /// <param name="request">工具审批申请，包含会话绑定、执行身份、工具版本和调用参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>为当前工具调用创建的待审批请求记录。</returns>
    Task<ToolApprovalRequestRecord> RequestAsync(AgentToolApprovalRequest request, CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// Agent 运行准备或执行错误。
/// </summary>
/// <param name="Code">业务唯一编码。</param>
/// <param name="Message">面向调用方的错误说明。</param>
public sealed record AgentRunError(string Code, string Message);

/// <summary>
/// Agent 运行准备结果。
/// </summary>
/// <param name="Context">准备完成的 Agent 运行上下文。</param>
/// <param name="Error">运行准备失败信息。</param>
public sealed record AgentRunPreparationResult(
    AgentRunContext? Context,
    AgentRunError? Error)
{
    /// <summary>
    /// 获取操作是否成功。
    /// </summary>
    public bool Succeeded => Error is null;

    #region 处理（Success）
    /// <summary>
    /// 处理（Success）
    /// </summary>
    /// <param name="context">Agent 运行上下文，包含固定版本快照、输入和工具资源。</param>
    /// <returns>包含运行上下文且无错误信息的 Agent 准备成功结果。</returns>
    public static AgentRunPreparationResult Success(AgentRunContext context) =>
        new(context, null);
    #endregion

    #region 处理（Failure）
    /// <summary>
    /// 处理（Failure）
    /// </summary>
    /// <param name="code">对象编码或业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含指定错误码和消息、不含运行上下文的 Agent 准备失败结果。</returns>
    public static AgentRunPreparationResult Failure(string code, string message) =>
        new(null, new AgentRunError(code, message));
    #endregion
}

/// <summary>
/// Agent 运行时的扩展执行选项。
/// </summary>
/// <param name="InternalTools">本次运行可使用的内部工具集合。</param>
/// <param name="McpCallGuard">MCP 工具调用前置校验器。</param>
public sealed record AgentRunExecutionOptions(
    IReadOnlyList<IAgentInternalTool>? InternalTools = null,
    IAgentMcpCallGuard? McpCallGuard = null)
{
    /// <summary>
    /// MCP 调用结果守卫。
    /// </summary>
    public IAgentMcpResultGuard? McpResultGuard { get; init; }

    /// <summary>
    /// 当前执行身份信息。
    /// </summary>
    public AgentExecutionIdentity? ExecutionIdentity { get; init; }

    /// <summary>
    /// 工具审批绑定信息。
    /// </summary>
    public AgentToolApprovalBinding? ToolApprovalBinding { get; init; }

    /// <summary>
    /// 工具审批处理器。
    /// </summary>
    public IAgentToolApprovalHandler? ToolApprovalHandler { get; init; }
}

/// <summary>
/// 定义 Agent 运行领域错误码。
/// </summary>
public static class AgentRunErrorCodes
{
    /// <summary>表示 <c>AgentNotFound</c> 场景的错误码。</summary>
    public const string AgentNotFound = "AGENT_NOT_FOUND";
    /// <summary>表示 <c>AgentDisabled</c> 场景的错误码。</summary>
    public const string AgentDisabled = "AGENT_RUNTIME_DISABLED";
    /// <summary>表示 <c>VersionMissing</c> 场景的错误码。</summary>
    public const string VersionMissing = "AGENT_PUBLISHED_VERSION_MISSING";
    /// <summary>表示 <c>InputInvalid</c> 场景的错误码。</summary>
    public const string InputInvalid = "AGENT_RUN_INPUT_INVALID";
    /// <summary>表示 <c>SkillUnavailable</c> 场景的错误码。</summary>
    public const string SkillUnavailable = "SKILL_VERSION_UNAVAILABLE";
    /// <summary>表示 <c>ToolUnavailable</c> 场景的错误码。</summary>
    public const string ToolUnavailable = "MCP_TOOL_VERSION_UNAVAILABLE";
    /// <summary>表示 <c>KnowledgeUnavailable</c> 场景的错误码。</summary>
    public const string KnowledgeUnavailable = "KNOWLEDGE_BASE_UNAVAILABLE";
    /// <summary>表示 <c>KnowledgeServiceUnavailable</c> 场景的错误码。</summary>
    public const string KnowledgeServiceUnavailable = "KNOWLEDGE_SERVICE_UNAVAILABLE";
    /// <summary>表示 <c>KnowledgeRevisionStale</c> 场景的错误码。</summary>
    public const string KnowledgeRevisionStale = "KNOWLEDGE_REVISION_STALE";
    /// <summary>表示 <c>KnowledgeBindingUnavailable</c> 场景的错误码。</summary>
    public const string KnowledgeBindingUnavailable = "KNOWLEDGE_BINDING_UNAVAILABLE";
    /// <summary>表示 <c>ToolBlocked</c> 场景的错误码。</summary>
    public const string ToolBlocked = "MCP_TOOL_CALL_BLOCKED";
    /// <summary>表示 <c>ToolFailed</c> 场景的错误码。</summary>
    public const string ToolFailed = "MCP_TOOL_CALL_FAILED";
    /// <summary>表示 <c>ToolTimedOut</c> 场景的错误码。</summary>
    public const string ToolTimedOut = "MCP_TOOL_CALL_TIMEOUT";
    /// <summary>表示 <c>ToolResultTooLarge</c> 场景的错误码。</summary>
    public const string ToolResultTooLarge = "MCP_TOOL_RESULT_TOO_LARGE";
    /// <summary>表示 <c>ToolArgumentLimitExceeded</c> 场景的错误码。</summary>
    public const string ToolArgumentLimitExceeded = "TOOL_ARGUMENT_LIMIT_EXCEEDED";
    /// <summary>表示 <c>InternalToolResultTooLarge</c> 场景的错误码。</summary>
    public const string InternalToolResultTooLarge =
        "INTERNAL_TOOL_RESULT_TOO_LARGE";
    /// <summary>表示 <c>InternalToolCallLimitExceeded</c> 场景的错误码。</summary>
    public const string InternalToolCallLimitExceeded =
        "INTERNAL_TOOL_CALL_LIMIT_EXCEEDED";
    /// <summary>表示 <c>McpToolCallLimitExceeded</c> 场景的错误码。</summary>
    public const string McpToolCallLimitExceeded =
        "MCP_TOOL_CALL_LIMIT_EXCEEDED";
    /// <summary>表示 <c>ToolApprovalRequired</c> 场景的错误码。</summary>
    public const string ToolApprovalRequired = "MCP_TOOL_APPROVAL_REQUIRED";
    /// <summary>表示 <c>ToolConfigurationInvalid</c> 场景的错误码。</summary>
    public const string ToolConfigurationInvalid = "AGENT_TOOL_CONFIGURATION_INVALID";
    /// <summary>表示 <c>ModelCredentialMissing</c> 场景的错误码。</summary>
    public const string ModelCredentialMissing = "MODEL_CREDENTIAL_MISSING";
    /// <summary>表示 <c>ModelFailed</c> 场景的错误码。</summary>
    public const string ModelFailed = "MODEL_INVOCATION_FAILED";
    /// <summary>表示 <c>ModelOutputLimitExceeded</c> 场景的错误码。</summary>
    public const string ModelOutputLimitExceeded = "MODEL_OUTPUT_LIMIT_EXCEEDED";
    /// <summary>表示 <c>ModelOutputEventLimitExceeded</c> 场景的错误码。</summary>
    public const string ModelOutputEventLimitExceeded =
        "MODEL_OUTPUT_EVENT_LIMIT_EXCEEDED";
    /// <summary>表示 <c>ModelInputLimitExceeded</c> 场景的错误码。</summary>
    public const string ModelInputLimitExceeded = "MODEL_INPUT_LIMIT_EXCEEDED";
    /// <summary>表示 <c>OutputInvalid</c> 场景的错误码。</summary>
    public const string OutputInvalid = "AGENT_OUTPUT_INVALID";
}

/// <summary>
/// 定义执行 Agent 版本快照并产生流式事件的运行引擎。
/// </summary>
public interface IAgentRuntimeEngine
{
    #region 启动Agent 运行并流式返回事件。
    /// <summary>启动Agent 运行并流式返回事件。</summary>
    /// <param name="context">Agent 运行上下文，包含固定版本快照、输入和工具资源。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>按执行顺序产生的异步事件流。</returns>
    IAsyncEnumerable<AgentRunEvent> StreamAsync(AgentRunContext context, CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 定义 Agent 运行审计记录的存储边界。
/// </summary>
public interface IAgentRunAuditRepository
{
    #region 保存Agent 运行审计记录。
    /// <summary>保存Agent 运行审计记录。</summary>
    /// <param name="record">业务记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示操作完成的异步任务。</returns>
    Task SaveAsync(AgentRunAuditRecord record, CancellationToken cancellationToken = default);
    #endregion

    #region 查询Agent 运行审计记录列表。
    /// <summary>查询Agent 运行审计记录列表。</summary>
    /// <param name="agentId">Agent 定义标识。</param>
    /// <param name="take">最多返回的记录数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定 Agent 的运行审计及工具调用明细集合，受读取数量限制。</returns>
    Task<IReadOnlyList<AgentRunAuditRecord>> ListAsync(Guid agentId, int take, CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 内部工具调用结果。
/// </summary>
/// <param name="Succeeded">执行是否成功。</param>
/// <param name="Content">消息或工具返回内容。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
public sealed record AgentInternalToolResult(
    bool Succeeded,
    string Content,
    string ErrorCode);

/// <summary>
/// 定义 Agent 运行时可调用的内部工具。
/// </summary>
public interface IAgentInternalTool
{
    /// <summary>获取内部工具名称。</summary>
    string Name { get; }

    /// <summary>获取内部工具说明。</summary>
    string Description { get; }

    /// <summary>获取内部工具输入参数的 JSON Schema。</summary>
    string InputSchemaJson { get; }

    #region 调用Agent 内部工具。
    /// <summary>调用Agent 内部工具。</summary>
    /// <param name="argumentsJson">工具调用参数的 JSON 文本。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>内部工具执行结果，包含成功标志、结果内容及错误码。</returns>
    Task<AgentInternalToolResult> InvokeAsync(string argumentsJson, CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// MCP 工具调用前置校验的拒绝结果。
/// </summary>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
/// <param name="Message">面向调用方的错误说明。</param>
public sealed record AgentMcpCallDenial(
    string ErrorCode,
    string Message);

/// <summary>
/// MCP 工具调用前置校验结果。
/// </summary>
/// <param name="Denial">校验拒绝原因；为空表示允许。</param>
public sealed record AgentMcpCallGuardResult(
    AgentMcpCallDenial? Denial)
{
    /// <summary>
    /// 获取校验是否允许继续执行。
    /// </summary>
    public bool Allowed => Denial is null;

    #region 处理（Allow）
    /// <summary>
    /// 处理（Allow）
    /// </summary>
    /// <returns>没有拒绝信息的 MCP 调用预算允许结果。</returns>
    public static AgentMcpCallGuardResult Allow() =>
        new((AgentMcpCallDenial?)null);
    #endregion

    #region 处理（Deny）
    /// <summary>
    /// 处理（Deny）
    /// </summary>
    /// <param name="errorCode">操作失败对应的业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含指定错误码和提示的 MCP 调用预算拒绝结果。</returns>
    public static AgentMcpCallGuardResult Deny(string errorCode, string message) =>
        new(new AgentMcpCallDenial(errorCode, message));
    #endregion
}

/// <summary>
/// 定义 MCP 工具调用前的运行时校验器。
/// </summary>
public interface IAgentMcpCallGuard
{
    #region 校验并预留MCP 工具调用配额。
    /// <summary>校验并预留MCP 工具调用配额。</summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>本次调用是否成功预留预算，以及预算不足或策略拒绝时的说明。</returns>
    ValueTask<AgentMcpCallGuardResult> ReserveAsync(CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// MCP 工具的运行时调用结果。
/// </summary>
/// <param name="Succeeded">执行是否成功。</param>
/// <param name="Blocked">调用是否被策略阻止。</param>
/// <param name="Content">消息或工具返回内容。</param>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
public sealed record McpRuntimeToolResult(
    bool Succeeded,
    bool Blocked,
    string Content,
    string ErrorCode);

/// <summary>
/// 定义 MCP 工具的运行时调用能力。
/// </summary>
public interface IMcpRuntimeToolInvoker
{
    #region 调用MCP 工具。
    /// <summary>调用MCP 工具。</summary>
    /// <param name="toolVersionId">工具版本标识。</param>
    /// <param name="expectedRisk">调用时要求工具匹配的风险等级。</param>
    /// <param name="arguments">调用参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定工具版本调用后的成功、阻止状态、结果内容及错误码。</returns>
    Task<McpRuntimeToolResult> InvokeAsync(
        Guid toolVersionId,
        McpToolRisk expectedRisk,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default);
    #endregion

    #region 调用MCP 工具。
    /// <summary>调用MCP 工具。</summary>
    /// <param name="toolVersionId">工具版本标识。</param>
    /// <param name="expectedRisk">调用时要求工具匹配的风险等级。</param>
    /// <param name="arguments">调用参数。</param>
    /// <param name="invocationContext">MCP 调用所用的执行身份和运行上下文。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>工具调用结果；接口默认实现转发到不带调用上下文的重载。</returns>
    Task<McpRuntimeToolResult> InvokeAsync(
        Guid toolVersionId,
        McpToolRisk expectedRisk,
        IReadOnlyDictionary<string, object?> arguments,
        McpInvocationContext? invocationContext,
        CancellationToken cancellationToken = default) =>
        InvokeAsync(toolVersionId, expectedRisk, arguments, cancellationToken);
    #endregion
}

/// <summary>
/// MCP 工具结果校验的拒绝原因。
/// </summary>
/// <param name="ErrorCode">失败错误码；成功时为空。</param>
/// <param name="Message">面向调用方的错误说明。</param>
public sealed record AgentMcpResultDenial(
    string ErrorCode,
    string Message);

/// <summary>
/// MCP 工具结果校验结果。
/// </summary>
/// <param name="Denial">校验拒绝原因；为空表示允许。</param>
public sealed record AgentMcpResultGuardResult(
    AgentMcpResultDenial? Denial)
{
    /// <summary>
    /// 获取校验是否允许继续执行。
    /// </summary>
    public bool Allowed => Denial is null;

    #region 处理（Allow）
    /// <summary>
    /// 处理（Allow）
    /// </summary>
    /// <returns>没有拒绝信息的 MCP 结果预算允许结果。</returns>
    public static AgentMcpResultGuardResult Allow() =>
        new((AgentMcpResultDenial?)null);
    #endregion

    #region 处理（Deny）
    /// <summary>
    /// 处理（Deny）
    /// </summary>
    /// <param name="errorCode">操作失败对应的业务错误码。</param>
    /// <param name="message">消息或提示文本。</param>
    /// <returns>包含指定错误码和提示的 MCP 结果预算拒绝结果。</returns>
    public static AgentMcpResultGuardResult Deny(string errorCode, string message) =>
        new(new AgentMcpResultDenial(errorCode, message));
    #endregion
}

/// <summary>
/// 定义 MCP 工具结果返回 Agent 前的校验器。
/// </summary>
public interface IAgentMcpResultGuard
{
    #region 校验并预留MCP 工具结果配额。
    /// <summary>校验并预留MCP 工具结果配额。</summary>
    /// <param name="resultUtf8Bytes">工具结果按 UTF-8 编码后的字节数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>指定 UTF-8 字节数的工具结果是否成功预留容量，以及拒绝时的说明。</returns>
    ValueTask<AgentMcpResultGuardResult> ReserveAsync(int resultUtf8Bytes, CancellationToken cancellationToken = default);
    #endregion
}

/// <summary>
/// 提供 Agent 运行契约对象的防御性复制。
/// </summary>
public static class AgentRunContractCloner
{
    #region 复制（Clone）
    /// <summary>
    /// 复制（Clone）
    /// </summary>
    /// <param name="record">业务记录。</param>
    /// <returns>复制工具调用记录并包装为只读集合的运行审计副本。</returns>
    public static AgentRunAuditRecord Clone(AgentRunAuditRecord record) =>
        record with
        {
            ToolCalls = new ReadOnlyCollection<AgentToolCallAuditRecord>(
                record.ToolCalls.Select(call => call with { }).ToArray())
        };
    #endregion
}
