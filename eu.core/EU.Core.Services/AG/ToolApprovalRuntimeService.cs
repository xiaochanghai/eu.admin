using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EU.Core.IServices.Approvals;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;
using EU.Core.IServices.UnifiedEntry;

#nullable enable

namespace EU.Core.Services;

// 文件职责：ToolApprovalRuntimeService 职责实现

/// <summary>
/// 处理运行时工具审批的创建、恢复和执行。
/// </summary>
/// <param name="approvals">用于读取和持久化工具审批请求的仓储。</param>
/// <param name="payloadProtector">用于加密和解密审批载荷的保护器。</param>
/// <param name="tools">用于查询已发布 MCP 工具版本的目录。</param>
/// <param name="policy">用于在审批执行前重新校验调用权限和约束的策略。</param>
/// <param name="invoker">用于执行获批 MCP 工具调用的调用器。</param>
/// <param name="approvalLifetime">审批请求有效时长；为 null 时使用 15 分钟，指定值须大于零且不超过 1 小时。</param>
/// <param name="executionTimeout">获批工具执行超时时长；为 null 时使用 65 秒，指定值须大于零且不超过 10 分钟。</param>
/// <param name="maximumPersistedResultUtf8Bytes">审批执行结果允许持久化的 UTF-8 字节上限，默认 30,000 字节。</param>
public sealed class ToolApprovalRuntimeService(
    IToolApprovalRepository approvals,
    IToolApprovalPayloadProtector payloadProtector,
    IPublishedMcpToolCatalog tools,
    IToolApprovalExecutionPolicy policy,
    IApprovedMcpRuntimeToolInvoker invoker,
    TimeSpan? approvalLifetime = null,
    TimeSpan? executionTimeout = null,
    int maximumPersistedResultUtf8Bytes = 30_000) : IAgentToolApprovalHandler
{
    /// <summary>工具调用审批请求的默认有效时长。</summary>
    public static readonly TimeSpan DefaultApprovalLifetime = TimeSpan.FromMinutes(15);

    /// <summary>获批工具调用的默认执行超时时长。</summary>
    public static readonly TimeSpan DefaultExecutionTimeout = TimeSpan.FromSeconds(65);
    private const int MaximumArgumentsUtf8Bytes = 32_768;
    private readonly int _maximumPersistedResultUtf8Bytes =
        maximumPersistedResultUtf8Bytes is >= 4_096
            and <= ToolApprovalStateMachine.MaximumResultPlaintextUtf8Bytes
            ? maximumPersistedResultUtf8Bytes
            : throw new ArgumentOutOfRangeException(
                nameof(maximumPersistedResultUtf8Bytes));
    private readonly TimeSpan _approvalLifetime = approvalLifetime is null
        ? DefaultApprovalLifetime
        : approvalLifetime.Value > TimeSpan.Zero
            && approvalLifetime.Value <= TimeSpan.FromHours(1)
            ? approvalLifetime.Value
            : throw new ArgumentOutOfRangeException(nameof(approvalLifetime));
    private readonly TimeSpan _executionTimeout = executionTimeout is null
        ? DefaultExecutionTimeout
        : executionTimeout.Value > TimeSpan.Zero
            && executionTimeout.Value <= TimeSpan.FromMinutes(10)
            ? executionTimeout.Value
            : throw new ArgumentOutOfRangeException(nameof(executionTimeout));

    #region 处理（RequestAsync）
    /// <summary>
    /// 处理（RequestAsync）
    /// </summary>
    /// <param name="request">工具审批申请，包含会话绑定、执行身份、工具版本和调用参数。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>使用当前时间及配置有效期创建并持久化的待审批请求。</returns>
    async Task<ToolApprovalRequestRecord> IAgentToolApprovalHandler.RequestAsync(AgentToolApprovalRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return await RequestAsync(new ToolApprovalRuntimeRequest(
            request.Binding.ConversationId,
            request.Binding.EntryRunId,
            request.AgentRunId,
            request.AgentVersionId,
            request.Tool,
            request.ArgumentsJson,
            request.Requester,
            now,
            now.Add(_approvalLifetime)), cancellationToken);
    }
    #endregion

    #region 处理（RequestAsync）
    /// <summary>
    /// 处理（RequestAsync）
    /// </summary>
    /// <param name="request">审批创建参数，包含绑定身份、工具参数以及申请和过期时间。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>包含参数和 Schema 摘要的已持久化待审批请求；受保护载荷保存失败时抛出异常。</returns>
    public async Task<ToolApprovalRequestRecord> RequestAsync(ToolApprovalRuntimeRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        string argumentsJson = NormalizeArguments(request.ArgumentsJson);
        string argumentsSha256 = Sha256(argumentsJson);
        string schemaSha256 = Sha256(NormalizeSchema(request.Tool.InputSchemaJson));
        Guid approvalId = Guid.NewGuid();
        var record = new ToolApprovalRequestRecord(
            approvalId,
            request.Requester.TenantId,
            request.Requester.UserId,
            request.ConversationId,
            request.EntryRunId,
            request.AgentRunId,
            request.AgentVersionId,
            request.Tool.ServerId,
            request.Tool.ToolVersionId,
            request.Tool.ToolName,
            request.Tool.Risk,
            schemaSha256,
            argumentsSha256,
            SafeSummary(argumentsJson),
            ToolApprovalStatus.Pending,
            0,
            request.RequestedAtUtc,
            request.ExpiresAtUtc,
            string.Empty,
            string.Empty,
            null,
            null,
            null,
            string.Empty);
        string protectedPayload = payloadProtector.Protect(
            new ToolApprovalPayloadContext(
                approvalId,
                request.Requester.TenantId,
                argumentsSha256),
            argumentsJson);
        if (!await approvals.TryCreateAsync(record, protectedPayload, cancellationToken))
        {
            throw new ToolApprovalException(
                ToolApprovalErrorCodes.InvalidState,
                "The tool approval request could not be persisted.");
        }

        return record;
    }
    #endregion

    #region 处理（ResumeApprovedAsync）
    /// <summary>
    /// 处理（ResumeApprovedAsync）
    /// </summary>
    /// <param name="request">已批准工具调用的恢复请求，包含审批标识和当前执行身份。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>重新校验并执行后的工具结果，或已完成审批的持久化结果重放；无效状态及安全校验失败会抛出异常。</returns>
    public async Task<McpRuntimeToolResult> ResumeApprovedAsync(ToolApprovalResumeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ToolApprovalRequestRecord approval = await approvals.GetAsync(
            request.ApprovalId,
            request.Requester.TenantId,
            cancellationToken) ?? throw InvalidState();
        if (!string.Equals(
                approval.RequesterUserId,
                request.Requester.UserId,
                StringComparison.Ordinal))
        {
            throw InvalidState();
        }

        if (approval.Status is ToolApprovalStatus.Consumed
            or ToolApprovalStatus.Failed)
        {
            return await ReadCompletedResultAsync(
                approval,
                cancellationToken);
        }

        if (approval.Status != ToolApprovalStatus.Approved
            || approval.LogicalRevision != request.ExpectedLogicalRevision
            )
        {
            throw InvalidState();
        }

        PublishedMcpToolReference? tool = (await tools.ListAsync(cancellationToken))
            .SingleOrDefault(value => value.ToolVersionId == approval.ToolVersionId);
        if (tool is null || !FrozenToolMatches(approval, tool))
        {
            await InvalidateAsync(
                approval,
                ToolApprovalErrorCodes.RevalidationFailed,
                request.ResumedAtUtc,
                cancellationToken);
            throw InvalidState();
        }
        ToolApprovalPolicyResult policyResult = await policy.RevalidateAsync(
            approval,
            tool,
            request.Requester,
            cancellationToken);
        if (!policyResult.Allowed)
        {
            await InvalidateAsync(
                approval,
                policyResult.ErrorCode,
                request.ResumedAtUtc,
                cancellationToken);
            throw new ToolApprovalException(
                policyResult.ErrorCode,
                "The approved tool call no longer satisfies Runtime Policy.");
        }

        ToolApprovalExecutionClaim claim = await approvals.TryClaimExecutionAsync(
            approval.Id,
            approval.TenantId,
            approval.LogicalRevision,
            request.ResumedAtUtc,
            cancellationToken) ?? throw InvalidState();
        IReadOnlyDictionary<string, object?> arguments;
        try
        {
            ToolApprovalStateMachine.ValidateProtectedPayload(
                claim.ProtectedResumePayload);
            if (!string.Equals(
                Sha256(claim.ProtectedResumePayload),
                claim.ProtectedResumePayloadSha256,
                StringComparison.Ordinal))
            {
                throw InvalidState();
            }

            string argumentsJson = payloadProtector.Unprotect(
                new ToolApprovalPayloadContext(
                    approval.Id,
                    approval.TenantId,
                    approval.ArgumentsSha256),
                claim.ProtectedResumePayload);
            if (!string.Equals(
                Sha256(argumentsJson),
                approval.ArgumentsSha256,
                StringComparison.Ordinal)
                || Encoding.UTF8.GetByteCount(argumentsJson)
                    > MaximumArgumentsUtf8Bytes)
            {
                throw InvalidState();
            }

            arguments = DeserializeArguments(argumentsJson);
        }
        catch
        {
            await MarkCompletedAsync(
                claim.Request,
                new McpRuntimeToolResult(
                    false,
                    false,
                    string.Empty,
                    ToolApprovalErrorCodes.PayloadInvalid),
                request.ResumedAtUtc,
                CancellationToken.None);
            throw;
        }

        McpRuntimeToolResult result;
        using var executionCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task<McpRuntimeToolResult>? invocation = null;
        try
        {
            invocation = invoker.InvokeApprovedAsync(
                claim,
                tool,
                arguments,
                new McpInvocationContext(request.Requester, approval.AgentRunId),
                executionCancellation.Token);
            result = await invocation.WaitAsync(_executionTimeout, cancellationToken);
            result = NormalizeResult(result);
        }
        catch (TimeoutException)
        {
            executionCancellation.Cancel();
            ObserveLateInvocation(invocation);
            result = new McpRuntimeToolResult(
                false,
                false,
                string.Empty,
                ToolApprovalErrorCodes.ExecutionOutcomeUnknown);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await MarkCompletedAsync(
                claim.Request,
                new McpRuntimeToolResult(
                    false,
                    false,
                    string.Empty,
                    AgentRunErrorCodes.ToolFailed),
                DateTimeOffset.UtcNow,
                CancellationToken.None);
            throw;
        }
        catch
        {
            await MarkCompletedAsync(
                claim.Request,
                new McpRuntimeToolResult(
                    false,
                    false,
                    string.Empty,
                    AgentRunErrorCodes.ToolFailed),
                DateTimeOffset.UtcNow,
                CancellationToken.None);
            throw;
        }

        await MarkCompletedAsync(
            claim.Request,
            result,
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        return result;
    }
    #endregion

    #region 处理（ObserveLateInvocation）
    /// <summary>
    /// 处理（ObserveLateInvocation）
    /// </summary>
    /// <param name="invocation">调用上下文。</param>
    private static void ObserveLateInvocation(Task<McpRuntimeToolResult>? invocation)
    {
        if (invocation is null)
        {
            return;
        }

        _ = invocation.ContinueWith(
            static task => _ = task.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
    #endregion

    #region 规范化（NormalizeResult）
    /// <summary>
    /// 规范化（NormalizeResult）
    /// </summary>
    /// <param name="result">操作结果。</param>
    /// <returns>内容规范化且符合持久化 UTF-8 字节上限的工具结果副本；超限时按 Unicode 字符截断并追加提示。</returns>
    private McpRuntimeToolResult NormalizeResult(McpRuntimeToolResult result)
    {
        string content = result.Content ?? string.Empty;
        if (Encoding.UTF8.GetByteCount(content) <= _maximumPersistedResultUtf8Bytes)
        {
            return result with { Content = content };
        }

        const string suffix = "\n\n[Tool result truncated by the approval safety limit.]";
        int budget = _maximumPersistedResultUtf8Bytes
            - Encoding.UTF8.GetByteCount(suffix);
        var builder = new StringBuilder();
        int used = 0;
        foreach (Rune rune in content.EnumerateRunes())
        {
            int bytes = rune.Utf8SequenceLength;
            if (used + bytes > budget)
            {
                break;
            }
            builder.Append(rune.ToString());
            used += bytes;
        }
        builder.Append(suffix);
        return result with { Content = builder.ToString() };
    }
    #endregion

    #region 读取（ReadCompletedResultAsync）
    /// <summary>
    /// 读取（ReadCompletedResultAsync）
    /// </summary>
    /// <param name="approval">审批记录。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>校验审批绑定、解密内容及摘要后重建的已完成工具结果；数据无效时抛出 InvalidState 异常。</returns>
    private async Task<McpRuntimeToolResult> ReadCompletedResultAsync(ToolApprovalRequestRecord approval, CancellationToken cancellationToken)
    {
        ToolApprovalExecutionResultRecord result =
            await approvals.GetExecutionResultAsync(
                approval.Id,
                approval.TenantId,
                cancellationToken) ?? throw InvalidState();
        try
        {
            ToolApprovalStateMachine.ValidateExecutionResultEnvelope(result);
        }
        catch (ToolApprovalException)
        {
            throw InvalidState();
        }

        if (result.ApprovalId != approval.Id
            || !string.Equals(
                result.TenantId,
                approval.TenantId,
                StringComparison.Ordinal)
            || result.Succeeded != (approval.Status == ToolApprovalStatus.Consumed)
            || !string.Equals(
                result.ErrorCode,
                approval.ErrorCode,
                StringComparison.Ordinal))
        {
            throw InvalidState();
        }

        string content;
        try
        {
            content = payloadProtector.Unprotect(
                new ToolApprovalPayloadContext(
                    approval.Id,
                    approval.TenantId,
                    approval.ArgumentsSha256),
                result.ProtectedContent);
        }
        catch
        {
            // Completed results are durable security state. Do not expose
            // protector, key, format, or authentication details on replay.
            throw InvalidState();
        }

        if (!string.Equals(Sha256(content), result.ContentSha256, StringComparison.Ordinal)
            || Encoding.UTF8.GetByteCount(content) > _maximumPersistedResultUtf8Bytes)
        {
            throw InvalidState();
        }

        return new McpRuntimeToolResult(
            result.Succeeded,
            result.Blocked,
            content,
            result.ErrorCode);
    }
    #endregion

    #region 处理（InvalidateAsync）
    /// <summary>
    /// 处理（InvalidateAsync）
    /// </summary>
    /// <param name="approval">审批记录。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <param name="invalidatedAtUtc">失效时间（UTC）。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task InvalidateAsync(
        ToolApprovalRequestRecord approval,
        string errorCode,
        DateTimeOffset invalidatedAtUtc,
        CancellationToken cancellationToken)
    {
        ToolApprovalRequestRecord invalidated = ToolApprovalStateMachine.Invalidate(
            approval,
            errorCode,
            invalidatedAtUtc);
        if (!await approvals.TryReplaceAsync(
            invalidated,
            approval.LogicalRevision,
            cancellationToken))
        {
            throw InvalidState();
        }
    }
    #endregion

    #region 处理（MarkCompletedAsync）
    /// <summary>
    /// 处理（MarkCompletedAsync）
    /// </summary>
    /// <param name="consuming">正在消费的执行状态。</param>
    /// <param name="result">操作结果。</param>
    /// <param name="finishedAtUtc">完成时间（UTC）。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task MarkCompletedAsync(
        ToolApprovalRequestRecord consuming,
        McpRuntimeToolResult result,
        DateTimeOffset finishedAtUtc,
        CancellationToken cancellationToken)
    {
        ToolApprovalRequestRecord completed = ToolApprovalStateMachine.Complete(
            consuming,
            result.Succeeded,
            result.ErrorCode,
            finishedAtUtc < consuming.ClaimedAtUtc!.Value
                ? consuming.ClaimedAtUtc!.Value
                : finishedAtUtc);
        string protectedContent = payloadProtector.Protect(
            new ToolApprovalPayloadContext(
                consuming.Id,
                consuming.TenantId,
                consuming.ArgumentsSha256),
            result.Content ?? string.Empty);
        var executionResult = new ToolApprovalExecutionResultRecord(
            consuming.Id,
            consuming.TenantId,
            result.Succeeded,
            result.Blocked,
            protectedContent,
            Sha256(protectedContent),
            Sha256(result.Content ?? string.Empty),
            completed.ErrorCode,
            completed.FinishedAtUtc!.Value);
        if (!await approvals.TryCompleteExecutionAsync(
            completed,
            consuming.LogicalRevision,
            executionResult,
            cancellationToken))
        {
            throw new ToolApprovalException(
                ToolApprovalErrorCodes.ExecutionOutcomeUnknown,
                "The tool ran, but its terminal approval state could not be persisted.");
        }
    }
    #endregion

    #region 校验（ValidateRequest）
    /// <summary>
    /// 校验（ValidateRequest）
    /// </summary>
    /// <param name="request">审批创建参数，包含绑定身份、工具参数以及申请和过期时间。</param>
    private static void ValidateRequest(ToolApprovalRuntimeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Tool);
        ArgumentNullException.ThrowIfNull(request.Requester);
        if (request.ConversationId == Guid.Empty
            || request.EntryRunId == Guid.Empty
            || request.AgentRunId == Guid.Empty
            || request.AgentVersionId == Guid.Empty
            || request.Tool.ServerId == Guid.Empty
            || request.Tool.ToolVersionId == Guid.Empty
            || request.Tool.Risk is not (McpToolRisk.Mutating or McpToolRisk.HighRisk)
            || request.RequestedAtUtc >= request.ExpiresAtUtc)
        {
            throw Invalid();
        }
    }
    #endregion

    #region 核对审批时冻结的工具信息（FrozenToolMatches）
    /// <summary>
    /// 核对审批时冻结的工具信息（FrozenToolMatches）。
    /// </summary>
    /// <param name="approval">保存审批时工具版本、风险和输入结构摘要的审批记录。</param>
    /// <param name="tool">执行前重新加载的已发布工具引用。</param>
    /// <returns>服务器标识、工具版本、风险级别、工具名及规范化输入结构摘要均一致时返回 true，否则返回 false。</returns>
    private static bool FrozenToolMatches(ToolApprovalRequestRecord approval, PublishedMcpToolReference tool) =>
        approval.McpServerId == tool.ServerId
            && approval.ToolVersionId == tool.ToolVersionId
            && approval.Risk == tool.Risk
            && string.Equals(approval.ToolName, tool.ToolName, StringComparison.Ordinal)
            && string.Equals(
                approval.ToolSchemaSha256,
                Sha256(NormalizeSchema(tool.InputSchemaJson)),
                StringComparison.Ordinal);
    #endregion

    #region 规范化（NormalizeArguments）
    /// <summary>
    /// 规范化（NormalizeArguments）
    /// </summary>
    /// <param name="argumentsJson">工具调用参数的 JSON 文本。</param>
    /// <returns>满足深度、对象类型和字节上限的紧凑参数 JSON；不满足条件时抛出 Invalid 审批异常。</returns>
    private static string NormalizeArguments(string argumentsJson)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(argumentsJson, new JsonDocumentOptions
            {
                MaxDepth = 32,
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            });
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw Invalid();
            }

            string normalized = JsonSerializer.Serialize(document.RootElement);
            return Encoding.UTF8.GetByteCount(normalized) <= MaximumArgumentsUtf8Bytes
                ? normalized
                : throw Invalid();
        }
        catch (JsonException)
        {
            throw Invalid();
        }
    }
    #endregion

    #region 规范化（NormalizeSchema）
    /// <summary>
    /// 规范化（NormalizeSchema）
    /// </summary>
    /// <param name="schemaJson">JSON 架构文本。</param>
    /// <returns>满足对象类型、深度和字节上限的紧凑 Schema JSON；不满足条件时抛出审批异常。</returns>
    private static string NormalizeSchema(string schemaJson) =>
        NormalizeArguments(schemaJson);
    #endregion

    #region 处理（SafeSummary）
    /// <summary>
    /// 处理（SafeSummary）
    /// </summary>
    /// <param name="argumentsJson">工具调用参数的 JSON 文本。</param>
    /// <returns>包含字段数量及安全字段描述的摘要 JSON；超过摘要字节上限时抛出审批异常。</returns>
    private static string SafeSummary(string argumentsJson)
    {
        using JsonDocument document = JsonDocument.Parse(argumentsJson);
        var fields = new List<object>();
        CollectSummaryFields(document.RootElement, string.Empty, fields);
        string summary = JsonSerializer.Serialize(new
        {
            fieldCount = fields.Count,
            fields
        });
        return Encoding.UTF8.GetByteCount(summary)
                <= ToolApprovalStateMachine.MaximumSafeSummaryUtf8Bytes
            ? summary
            : throw Invalid();
    }
    #endregion

    #region 处理（CollectSummaryFields）
    /// <summary>
    /// 处理（CollectSummaryFields）
    /// </summary>
    /// <param name="value">当前遍历的参数 JSON 节点。</param>
    /// <param name="path">当前参数 JSON 节点的字段路径，用于生成安全摘要。</param>
    /// <param name="fields">收集字段路径、类型等安全描述的输出集合。</param>
    private static void CollectSummaryFields(JsonElement value, string path, ICollection<object> fields)
    {
        if (fields.Count >= 128)
        {
            throw Invalid();
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in value.EnumerateObject())
            {
                string segment = UnifiedEntryPayloadProtector
                    .ProtectInternal(property.Name)
                    .Content;
                CollectSummaryFields(
                    property.Value,
                    path.Length == 0 ? segment : $"{path}.{segment}",
                    fields);
            }
            return;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in value.EnumerateArray())
            {
                CollectSummaryFields(item, $"{path}[{index++}]", fields);
            }
            if (index == 0)
            {
                fields.Add(new
                {
                    path,
                    kind = "Array",
                    sha256 = Sha256("[]")
                });
            }
            return;
        }

        fields.Add(new
        {
            path,
            kind = value.ValueKind.ToString(),
            sha256 = Sha256(value.GetRawText())
        });
    }
    #endregion

    #region 处理（DeserializeArguments）
    /// <summary>
    /// 处理（DeserializeArguments）
    /// </summary>
    /// <param name="argumentsJson">工具调用参数的 JSON 文本。</param>
    /// <returns>按属性名索引、各值为独立 JsonElement 副本的只读参数字典。</returns>
    private static IReadOnlyDictionary<string, object?> DeserializeArguments(string argumentsJson)
    {
        Dictionary<string, JsonElement>? values = JsonSerializer.Deserialize<
            Dictionary<string, JsonElement>>(argumentsJson);
        if (values is null)
        {
            throw Invalid();
        }

        return new ReadOnlyDictionary<string, object?>(
            values.ToDictionary(
                pair => pair.Key,
                pair => (object?)pair.Value.Clone(),
                StringComparer.Ordinal));
    }
    #endregion

    #region 处理（Sha256）
    /// <summary>
    /// 处理（Sha256）
    /// </summary>
    /// <param name="value">用于计算 SHA-256 摘要的原始文本。</param>
    /// <returns>输入文本 UTF-8 字节的 SHA-256 小写十六进制摘要。</returns>
    private static string Sha256(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    #endregion

    #region 处理（Invalid）
    /// <summary>
    /// 处理（Invalid）
    /// </summary>
    /// <returns>表示审批请求无效的 Invalid 异常。</returns>
    private static ToolApprovalException Invalid() =>
        new(ToolApprovalErrorCodes.Invalid, "The tool approval request is invalid.");
    #endregion

    #region 处理（InvalidState）
    /// <summary>
    /// 处理（InvalidState）
    /// </summary>
    /// <returns>表示审批已不可执行的 InvalidState 异常。</returns>
    private static ToolApprovalException InvalidState() =>
        new(ToolApprovalErrorCodes.InvalidState, "The tool approval is no longer executable.");
    #endregion
}

/// <summary>
/// 提供工具审批恢复执行的默认安全策略。
/// </summary>
public sealed class DefaultToolApprovalExecutionPolicy : IToolApprovalExecutionPolicy
{
    // TODO(agent-authorization): Re-enable these permissions together with the
    // Agent authorization policies when fine-grained authorization is introduced.
    // public const string RunPermission = "agent.chat";
    // public const string AdminPermission = "agent.admin";

    #region 处理（RevalidateAsync）
    /// <summary>
    /// 处理（RevalidateAsync）
    /// </summary>
    /// <param name="approval">审批记录。</param>
    /// <param name="currentTool">当前工具版本。</param>
    /// <param name="requester">请求发起方。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>租户和请求用户匹配且工具风险为 Mutating 或 HighRisk 时允许，否则返回 RevalidationFailed 拒绝结果。</returns>
    public Task<ToolApprovalPolicyResult> RevalidateAsync(
        ToolApprovalRequestRecord approval,
        PublishedMcpToolReference currentTool,
        AgentExecutionIdentity requester,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(approval);
        ArgumentNullException.ThrowIfNull(currentTool);
        ArgumentNullException.ThrowIfNull(requester);
        bool allowed = string.Equals(
                approval.TenantId,
                requester.TenantId,
                StringComparison.Ordinal)
            && string.Equals(
                approval.RequesterUserId,
                requester.UserId,
                StringComparison.Ordinal)
            // Fine-grained Agent permissions are temporarily disabled. Restore this
            // condition together with RunPermission and AdminPermission above.
            // && (requester.Permissions.Contains(RunPermission, StringComparer.Ordinal)
            //     || requester.Permissions.Contains(AdminPermission, StringComparer.Ordinal))
            && currentTool.Risk is McpToolRisk.Mutating or McpToolRisk.HighRisk;
        return Task.FromResult(allowed
            ? ToolApprovalPolicyResult.Allow()
            : ToolApprovalPolicyResult.Deny(
                ToolApprovalErrorCodes.RevalidationFailed));
    }
    #endregion
}
