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

#region 文件职责：ToolApprovalRuntimeService 职责实现

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
    public static readonly TimeSpan DefaultApprovalLifetime = TimeSpan.FromMinutes(15);
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

    async Task<ToolApprovalRequestRecord> IAgentToolApprovalHandler.RequestAsync(
        AgentToolApprovalRequest request,
        CancellationToken cancellationToken)
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

    private static bool FrozenToolMatches(ToolApprovalRequestRecord approval, PublishedMcpToolReference tool) =>
        approval.McpServerId == tool.ServerId
            && approval.ToolVersionId == tool.ToolVersionId
            && approval.Risk == tool.Risk
            && string.Equals(approval.ToolName, tool.ToolName, StringComparison.Ordinal)
            && string.Equals(
                approval.ToolSchemaSha256,
                Sha256(NormalizeSchema(tool.InputSchemaJson)),
                StringComparison.Ordinal);

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

    private static string NormalizeSchema(string schemaJson) =>
        NormalizeArguments(schemaJson);

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

    private static string Sha256(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static ToolApprovalException Invalid() =>
        new(ToolApprovalErrorCodes.Invalid, "The tool approval request is invalid.");

    private static ToolApprovalException InvalidState() =>
        new(ToolApprovalErrorCodes.InvalidState, "The tool approval is no longer executable.");
}

public sealed class DefaultToolApprovalExecutionPolicy : IToolApprovalExecutionPolicy
{
    // TODO(agent-authorization): Re-enable these permissions together with the
    // Agent authorization policies when fine-grained authorization is introduced.
    // public const string RunPermission = "agent.chat";
    // public const string AdminPermission = "agent.admin";

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
}

#endregion
