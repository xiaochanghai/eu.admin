using EU.Core.IServices.UnifiedEntry;
using System.Text;
using System.Text.Json;
using EU.Core.IServices.Agents;
using EU.Core.Model.ViewModels.Extend;
using EU.Core.IServices.Mcp;
using EU.Core.IServices.Runtime;

#nullable enable

namespace EU.Core.Services;

// 文件职责：DelegateToAgentTool 职责实现

/// <summary>
/// 实现向子 Agent 委派任务的内部工具。
/// </summary>
public sealed class DelegateToAgentTool : IAgentInternalTool
{
    private const int MaximumTaskCharacters = 32_768;
    private const int MaximumReasonCharacters = 1_024;
    private const int MaximumCatalogNameCharacters = 128;
    private const int MaximumCatalogDescriptionCharacters = 512;
    private readonly IAgentRuntimeService _agentRuntime;
    private readonly AgentVersionSnapshot _authorizingSnapshot;
    private readonly UnifiedAgentExecutionLease _parentLease;
    private readonly Guid _parentRunId;
    private readonly UnifiedEntryExecutionScope _scope;
    private readonly AgentExecutionIdentity? _executionIdentity;
    private readonly BusinessQueryToolPolicy? _businessQueryPolicy;
    private readonly IAgentToolApprovalHandler? _toolApprovalHandler;
    private readonly AgentToolApprovalBinding? _toolApprovalBinding;
    private readonly string _description;
    private readonly string _inputSchemaJson;

    #region 构造（DelegateToAgentTool）
    /// <summary>
    /// 构造（DelegateToAgentTool）
    /// </summary>
    /// <param name="agentRuntime">Agent 运行时服务。</param>
    /// <param name="scope">执行范围。</param>
    /// <param name="authorizingSnapshot">授予访问权限的发布快照。</param>
    /// <param name="parentLease">父级执行租约。</param>
    /// <param name="parentRunId">父运行标识。</param>
    /// <param name="executionIdentity">当前执行使用的用户、租户及权限身份。</param>
    /// <param name="businessQueryPolicy">受控业务查询的工具调用策略。</param>
    /// <param name="toolApprovalHandler">工具调用审批处理器。</param>
    /// <param name="toolApprovalBinding">工具审批绑定。</param>
    public DelegateToAgentTool(
        IAgentRuntimeService agentRuntime,
        UnifiedEntryExecutionScope scope,
        AgentVersionSnapshot authorizingSnapshot,
        UnifiedAgentExecutionLease parentLease,
        Guid parentRunId,
        AgentExecutionIdentity? executionIdentity = null,
        BusinessQueryToolPolicy? businessQueryPolicy = null,
        IAgentToolApprovalHandler? toolApprovalHandler = null,
        AgentToolApprovalBinding? toolApprovalBinding = null)
    {
        _agentRuntime = agentRuntime
            ?? throw new ArgumentNullException(nameof(agentRuntime));
        _scope = scope
            ?? throw new ArgumentNullException(nameof(scope));
        _authorizingSnapshot = AgentContractCloner.Clone(
            authorizingSnapshot
            ?? throw new ArgumentNullException(nameof(authorizingSnapshot)));
        _parentLease = parentLease
            ?? throw new ArgumentNullException(nameof(parentLease));
        if (parentRunId == Guid.Empty)
        {
            throw new ArgumentException(
                "A parent unified Agent run ID is required.",
                nameof(parentRunId));
        }

        _parentRunId = parentRunId;
        _executionIdentity = executionIdentity;
        _businessQueryPolicy = businessQueryPolicy;
        _toolApprovalHandler = toolApprovalHandler;
        _toolApprovalBinding = toolApprovalBinding;
        if (_authorizingSnapshot.ChildAgents.Count
            > AgentDelegationPolicy.MaximumChildAgentBindings)
        {
            throw new UnifiedEntryException(
                UnifiedEntryErrorCodes.ChildCatalogInvalid,
                "The frozen child Agent catalog exceeds the supported bound.");
        }

        AgentChildBindingSnapshot[] bindings = _authorizingSnapshot.ChildAgents
            .OrderBy(value => value.AgentId)
            .ThenBy(value => value.AgentVersionId)
            .ToArray();
        _description =
            "Automatically select the best matching child Agent when the user request needs one of the authorized specialties, then delegate a bounded task. "
            + "Answer directly when no child specialty is relevant. Never ask the user for an Agent ID or version ID. "
            + "Authorized frozen child Agent catalog: "
            + string.Join(
                "; ",
                bindings.Select(DescribeBinding));
        _inputSchemaJson = InternalToolSchemaBuilder.Build(
            "agentVersionId",
            bindings.Select(value => value.AgentVersionId).ToArray(),
            "task",
            MaximumTaskCharacters,
            MaximumReasonCharacters);
    }
    #endregion

    /// <summary>
    /// 获取内部工具名称。
    /// </summary>
    public string Name => "delegate_to_agent";

    /// <summary>
    /// 获取内部工具说明。
    /// </summary>
    public string Description => _description;

    /// <summary>
    /// 获取内部工具输入参数的 JSON Schema。
    /// </summary>
    public string InputSchemaJson => _inputSchemaJson;

    #region 处理（DescribeBinding）
    /// <summary>
    /// 处理（DescribeBinding）
    /// </summary>
    /// <param name="binding">资源绑定。</param>
    /// <returns>包含固定 Agent 和版本标识、编码、名称及职责的目录说明；缺少语义描述时附带使用限制提示。</returns>
    private static string DescribeBinding(AgentChildBindingSnapshot binding)
    {
        string code = CompactCatalogValue(
            binding.AgentCode,
            MaximumCatalogNameCharacters);
        string name = CompactCatalogValue(
            binding.AgentName,
            MaximumCatalogNameCharacters);
        string description = CompactCatalogValue(
            binding.AgentDescription,
            MaximumCatalogDescriptionCharacters);
        if (code.Length == 0 && name.Length == 0 && description.Length == 0)
        {
            description =
                "No frozen semantic description is available; use this version only when the Main Agent instructions explicitly identify it.";
        }

        return $"agentId={binding.AgentId}, agentVersionId={binding.AgentVersionId}, "
            + $"code={JsonSerializer.Serialize(code)}, "
            + $"name={JsonSerializer.Serialize(name)}, "
            + $"responsibility={JsonSerializer.Serialize(description)}";
    }
    #endregion

    #region 处理（CompactCatalogValue）
    /// <summary>
    /// 处理（CompactCatalogValue）
    /// </summary>
    /// <param name="value">待合并空白并限制长度的目录说明文本。</param>
    /// <param name="maximumCharacters">允许的最大字符数。</param>
    /// <returns>合并空白并限制字符数的目录文本，截断时避免拆分 UTF-16 代理对。</returns>
    private static string CompactCatalogValue(string? value, int maximumCharacters)
    {
        string compact = string.Join(
            ' ',
            (value ?? string.Empty).Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));
        if (compact.Length <= maximumCharacters)
        {
            return compact;
        }

        int length = maximumCharacters;
        if (char.IsHighSurrogate(compact[length - 1])
            && char.IsLowSurrogate(compact[length]))
        {
            length--;
        }

        return compact[..length];
    }
    #endregion

    #region 调用（InvokeAsync）
    /// <summary>
    /// 调用（InvokeAsync）
    /// </summary>
    /// <param name="argumentsJson">工具调用参数的 JSON 文本。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>子 Agent 委派结果，成功时携带受保护输出或业务查询摘要，失败时携带错误码及安全提示。</returns>
    public async Task<AgentInternalToolResult> InvokeAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!InternalToolArgumentParser.TryParse(
                argumentsJson,
                "agentVersionId",
                "task",
                MaximumTaskCharacters,
                MaximumReasonCharacters,
                _scope.Limits.InternalPayloadUtf8Bytes,
                out InternalToolArguments arguments))
        {
            return Failure(
                UnifiedEntryErrorCodes.InternalArgumentsInvalid,
                "The delegate_to_agent arguments are invalid.");
        }

        AgentChildBindingSnapshot? binding = _authorizingSnapshot.ChildAgents
            .FirstOrDefault(value => value.AgentVersionId == arguments.VersionId);
        if (binding is null
            || _parentLease.AgentVersionId != _authorizingSnapshot.VersionId)
        {
            return Failure(
                UnifiedEntryErrorCodes.AgentVersionUnauthorized,
                "The requested Agent version is not authorized by the frozen Main Agent publication.");
        }

        if (cancellationToken.IsCancellationRequested
            || _parentLease.CancellationToken.IsCancellationRequested)
        {
            return CancellationFailure(cancellationToken, lease: null);
        }

        UnifiedAgentExecutionLease? childLease = null;
        Guid? childRunId = null;
        AgentRunContext? preparedContext = null;
        bool runtimeStreamingStarted = false;
        try
        {
            childLease = _scope.ReserveChildAgent(
                binding.AgentVersionId,
                _parentLease);
            using var effectiveCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    childLease.CancellationToken);
            CancellationToken effectiveToken = effectiveCancellation.Token;
            effectiveToken.ThrowIfCancellationRequested();

            AgentRunPreparationResult prepared =
                await _agentRuntime.PrepareVersionAsync(
                    binding.AgentId,
                    binding.AgentVersionId,
                    arguments.Value,
                    effectiveToken).ConfigureAwait(false);
            if (!prepared.Succeeded)
            {
                return Failure(
                    prepared.Error!.Code,
                    "The frozen child Agent version could not be prepared.");
            }

            AgentRunContext preparedChildContext = prepared.Context!;
            PublishedMcpToolReference? businessQueryTool = _businessQueryPolicy is null
                ? null
                : preparedChildContext.Tools.SingleOrDefault(
                    _businessQueryPolicy.Matches);
            bool reservedBusinessQueryBinding = _businessQueryPolicy is not null
                && preparedChildContext.Tools.Any(tool =>
                    string.Equals(
                        tool.ServerCode,
                        _businessQueryPolicy.ServerCode,
                        StringComparison.Ordinal)
                    || string.Equals(
                        tool.ToolName,
                        _businessQueryPolicy.ToolName,
                        StringComparison.Ordinal));
            if (reservedBusinessQueryBinding
                && (businessQueryTool is null || preparedChildContext.Tools.Count != 1))
            {
                return Failure(
                    UnifiedEntryErrorCodes.BusinessQueryEvidenceRequired,
                    "The controlled business query Agent publication is invalid.");
            }

            bool controlledBusinessQuery = businessQueryTool is not null;
            AgentRunContext childContext = preparedChildContext with
            {
                InternalTools = Array.Empty<IAgentInternalTool>(),
                McpCallGuard = controlledBusinessQuery
                    ? new BusinessQueryMcpCallGuard(_scope)
                    : _scope,
                McpResultGuard = _scope,
                McpToolCallLimits = BusinessQueryMcpToolCallLimits.Create(
                    _businessQueryPolicy,
                    preparedChildContext.Tools),
                ExecutionIdentity = _executionIdentity,
                ToolApprovalBinding = _toolApprovalBinding,
                ToolApprovalHandler = _toolApprovalHandler
            };
            preparedContext = childContext;
            childRunId = childContext.RunId;
            await RegisterChildRunAsync(
                childContext,
                childLease,
                arguments.Value,
                CancellationToken.None).ConfigureAwait(false);
            effectiveToken.ThrowIfCancellationRequested();

            var output = new StringBuilder();
            BusinessQueryAuthoritativeResult? authoritativeResult = null;
            int businessQueryAttempts = 0;
            int businessQuerySuccesses = 0;
            Guid? businessQueryCallId = null;
            string businessQueryViolation = string.Empty;
            string terminalError = string.Empty;
            UnifiedRunStatus? terminalStatus = null;
            runtimeStreamingStarted = true;
            await foreach (AgentRunEvent source in _agentRuntime
                .StreamAsync(childContext, effectiveToken)
                .WithCancellation(effectiveToken)
                .ConfigureAwait(false))
            {
                AgentRunEvent persistedSource = source;
                if (source.Kind == AgentRunEventKind.Delta && !controlledBusinessQuery)
                {
                    output.Append(source.Text);
                }

                if (controlledBusinessQuery
                    && source.Kind == AgentRunEventKind.ToolStarted)
                {
                    businessQueryAttempts++;
                    if (businessQueryAttempts == 1)
                    {
                        businessQueryCallId = source.ToolCallId;
                    }
                    if (source.ToolVersionId != businessQueryTool!.ToolVersionId
                        || !string.Equals(
                            source.ToolName,
                            businessQueryTool.ToolName,
                            StringComparison.Ordinal))
                    {
                        businessQueryViolation =
                            UnifiedEntryErrorCodes.BusinessQueryEvidenceRequired;
                    }
                    else if (businessQueryAttempts > 1)
                    {
                        businessQueryViolation =
                            UnifiedEntryErrorCodes.BusinessQueryCallLimitExceeded;
                    }
                }

                if (controlledBusinessQuery
                    && source.Kind == AgentRunEventKind.ToolSucceeded)
                {
                    businessQuerySuccesses++;
                    if (businessQueryAttempts != 1
                        || businessQuerySuccesses != 1
                        || source.ToolCallId != businessQueryCallId
                        || source.ToolVersionId != businessQueryTool!.ToolVersionId
                        || !BusinessQueryAuthoritativeResult.TryParse(
                            source.Text,
                            _businessQueryPolicy!,
                            out authoritativeResult))
                    {
                        authoritativeResult = null;
                        if (businessQueryViolation.Length == 0)
                        {
                            businessQueryViolation =
                                BusinessQueryAuthoritativeResult.TryReadFailureCode(
                                    source.Text,
                                    out string businessError)
                                    ? businessError
                                    : UnifiedEntryErrorCodes.BusinessQueryEvidenceRequired;
                        }
                    }
                }

                if (source.Kind == AgentRunEventKind.Failed)
                {
                    terminalStatus = UnifiedRunStatus.Failed;
                    terminalError = string.IsNullOrWhiteSpace(source.ErrorCode)
                        ? UnifiedEntryErrorCodes.ChildExecutionFailed
                        : source.ErrorCode;
                }
                else if (source.Kind == AgentRunEventKind.Cancelled)
                {
                    terminalStatus = UnifiedRunStatus.Cancelled;
                    terminalError = UnifiedEntryErrorCodes.Cancelled;
                }
                else if (source.Kind == AgentRunEventKind.Completed
                         && terminalStatus is null)
                {
                    if (controlledBusinessQuery
                        && (businessQueryViolation.Length > 0
                            || businessQueryAttempts != 1
                            || businessQuerySuccesses != 1
                            || authoritativeResult is null))
                    {
                        terminalStatus = UnifiedRunStatus.Failed;
                        terminalError = businessQueryViolation.Length == 0
                            ? UnifiedEntryErrorCodes.BusinessQueryEvidenceRequired
                            : businessQueryViolation;
                        persistedSource = source with
                        {
                            Kind = AgentRunEventKind.Failed,
                            Text = "The controlled business query did not produce valid server evidence.",
                            ErrorCode = terminalError
                        };
                    }
                    else
                    {
                        terminalStatus = UnifiedRunStatus.Completed;
                    }
                }

                await PersistChildEventAsync(
                    childContext.RunId,
                    childLease,
                    persistedSource,
                    output.ToString(),
                    arguments.Reason,
                    controlledBusinessQuery,
                    IsTerminal(persistedSource.Kind)
                        ? CancellationToken.None
                        : effectiveToken).ConfigureAwait(false);
            }

            if (terminalStatus == UnifiedRunStatus.Completed)
            {
                if (controlledBusinessQuery)
                {
                    if (authoritativeResult is null
                        || !_scope.TryRegisterBusinessQueryResult(
                            childContext.RunId,
                            authoritativeResult))
                    {
                        return Failure(
                            UnifiedEntryErrorCodes.BusinessQueryEvidenceRequired,
                            "The controlled business query evidence could not be registered.");
                    }

                    return new AgentInternalToolResult(
                        true,
                        authoritativeResult.ToModelSummary(),
                        string.Empty);
                }

                ProtectedUnifiedPayload protectedOutput = Protect(output.ToString());
                return new AgentInternalToolResult(
                    true,
                    protectedOutput.Content,
                    string.Empty);
            }

            return Failure(
                terminalError.Length == 0
                    ? UnifiedEntryErrorCodes.ChildExecutionFailed
                    : terminalError,
                "The child Agent execution did not complete successfully.");
        }
        catch (OperationCanceledException)
        {
            string errorCode = _scope.ClassifyCancellation(
                childLease,
                cancellationToken);
            await TryTerminateUnstartedAuditAsync(
                preparedContext,
                runtimeStreamingStarted,
                AgentRunStatus.Cancelled,
                errorCode).ConfigureAwait(false);
            if (childRunId.HasValue)
            {
                await TryPersistChildFailureAsync(
                    childRunId.Value,
                    childLease?.Depth ?? checked(_parentLease.Depth + 1),
                    UnifiedRunStatus.Cancelled,
                    errorCode).ConfigureAwait(false);
            }

            return Failure(errorCode, CancellationMessage(errorCode));
        }
        catch (UnifiedEntryException exception)
        {
            await TryTerminateUnstartedAuditAsync(
                preparedContext,
                runtimeStreamingStarted,
                AgentRunStatus.Failed,
                exception.ErrorCode).ConfigureAwait(false);
            if (childRunId.HasValue)
            {
                await TryPersistChildFailureAsync(
                    childRunId.Value,
                    childLease?.Depth ?? checked(_parentLease.Depth + 1),
                    UnifiedRunStatus.Failed,
                    exception.ErrorCode).ConfigureAwait(false);
            }

            return Failure(
                exception.ErrorCode,
                "The child Agent delegation was rejected by the unified entry scope.");
        }
        catch
        {
            await TryTerminateUnstartedAuditAsync(
                preparedContext,
                runtimeStreamingStarted,
                AgentRunStatus.Failed,
                UnifiedEntryErrorCodes.ChildExecutionFailed).ConfigureAwait(false);
            if (childRunId.HasValue)
            {
                await TryPersistChildFailureAsync(
                    childRunId.Value,
                    childLease?.Depth ?? checked(_parentLease.Depth + 1),
                    UnifiedRunStatus.Failed,
                    UnifiedEntryErrorCodes.ChildExecutionFailed).ConfigureAwait(false);
            }

            return Failure(
                UnifiedEntryErrorCodes.ChildExecutionFailed,
                "The child Agent execution failed.");
        }
        finally
        {
            childLease?.Dispose();
        }
    }
    #endregion

    #region 处理（RegisterChildRunAsync）
    /// <summary>
    /// 处理（RegisterChildRunAsync）
    /// </summary>
    /// <param name="context">Agent 运行上下文，包含固定版本快照、输入和工具资源。</param>
    /// <param name="lease">执行租约。</param>
    /// <param name="input">执行输入内容。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task RegisterChildRunAsync(AgentRunContext context, UnifiedAgentExecutionLease lease, string input, CancellationToken cancellationToken)
    {
        ProtectedUnifiedPayload protectedInput = Protect(input);
        await _scope.MutateAggregateAsync(aggregate =>
        {
            if (!aggregate.Details.AgentRuns.Any(value => value.Id == _parentRunId))
            {
                throw new UnifiedEntryException(
                    UnifiedEntryErrorCodes.InvalidState,
                    "The parent unified Agent run does not exist.");
            }

            if (aggregate.Details.AgentRuns.Any(value => value.Id == context.RunId))
            {
                throw new UnifiedEntryException(
                    UnifiedEntryErrorCodes.InvalidState,
                    "The child unified Agent run already exists.");
            }

            var child = new UnifiedAgentRunRecord(
                context.RunId,
                aggregate.Details.EntryRun.Id,
                _parentRunId,
                UnifiedAgentRunKind.Child,
                context.AgentId,
                context.Snapshot.VersionId,
                lease.Depth,
                UnifiedRunStatus.Running,
                context.StartedAtUtc,
                null,
                null,
                protectedInput.Content,
                protectedInput.OriginalSha256,
                string.Empty,
                string.Empty,
                string.Empty);
            return aggregate.WithDetails(new UnifiedRunDetails(
                aggregate.Details.EntryRun,
                aggregate.Details.AgentRuns.Append(child).ToArray(),
                aggregate.Details.Orchestrations,
                aggregate.Details.ToolCalls));
        }, cancellationToken).ConfigureAwait(false);
    }
    #endregion

    #region 处理（PersistChildEventAsync）
    /// <summary>
    /// 处理（PersistChildEventAsync）
    /// </summary>
    /// <param name="childRunId">子运行标识。</param>
    /// <param name="lease">执行租约。</param>
    /// <param name="source">源数据。</param>
    /// <param name="accumulatedOutput">当前累计的输出内容。</param>
    /// <param name="reason">操作原因或判断依据。</param>
    /// <param name="controlledBusinessQuery">是否按受控业务查询流程执行。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task PersistChildEventAsync(
        Guid childRunId,
        UnifiedAgentExecutionLease lease,
        AgentRunEvent source,
        string accumulatedOutput,
        string reason,
        bool controlledBusinessQuery,
        CancellationToken cancellationToken)
    {
        ProtectedUnifiedPayload protectedOutput = Protect(accumulatedOutput);
        UnifiedRunStatus status = source.Kind switch
        {
            AgentRunEventKind.Completed => UnifiedRunStatus.Completed,
            AgentRunEventKind.Failed => UnifiedRunStatus.Failed,
            AgentRunEventKind.Cancelled => UnifiedRunStatus.Cancelled,
            _ => UnifiedRunStatus.Running
        };
        string errorCode = status switch
        {
            UnifiedRunStatus.Failed when !string.IsNullOrWhiteSpace(source.ErrorCode) =>
                source.ErrorCode,
            UnifiedRunStatus.Failed => UnifiedEntryErrorCodes.ChildExecutionFailed,
            UnifiedRunStatus.Cancelled => UnifiedEntryErrorCodes.Cancelled,
            _ => string.Empty
        };
        await _scope.MutateAggregateAsync(aggregate =>
        {
            UnifiedAgentRunRecord current = aggregate.Details.AgentRuns
                .Single(value => value.Id == childRunId);
            DateTimeOffset? finishedAt = status == UnifiedRunStatus.Running
                ? null
                : source.OccurredAtUtc;
            TimeSpan? duration = finishedAt.HasValue
                ? NonNegative(finishedAt.Value - current.StartedAtUtc)
                : null;
            UnifiedAgentRunRecord updated = current with
            {
                Status = status,
                FinishedAtUtc = finishedAt,
                Duration = duration,
                Output = protectedOutput.Content,
                OutputSha256 = accumulatedOutput.Length == 0
                    ? string.Empty
                    : protectedOutput.OriginalSha256,
                ErrorCode = errorCode
            };
            return aggregate.WithDetails(new UnifiedRunDetails(
                aggregate.Details.EntryRun,
                aggregate.Details.AgentRuns
                    .Select(value => value.Id == childRunId ? updated : value)
                    .ToArray(),
                aggregate.Details.Orchestrations,
                aggregate.Details.ToolCalls));
        }, cancellationToken).ConfigureAwait(false);

        string rawPayloadJson = JsonSerializer.Serialize(new
        {
            agentRunId = childRunId,
            agentVersionId = lease.AgentVersionId,
            eventKind = source.Kind.ToString(),
            text = source.Text,
            argumentsJson = source.ArgumentsJson,
            source.ErrorCode,
            source.ToolVersionId,
            source.ToolName,
            source.ToolCallId,
            source.SkillVersionId,
            source.SkillName,
            source.KnowledgeBaseCount,
            source.KnowledgeHitCount,
            capability = controlledBusinessQuery && source.Kind is
                AgentRunEventKind.ToolStarted or
                AgentRunEventKind.ToolSucceeded or
                AgentRunEventKind.ToolBlocked or
                AgentRunEventKind.ToolFailed
                    ? "business-query"
                    : string.Empty,
            reason = source.Kind == AgentRunEventKind.Started
                ? reason
                : string.Empty
        });
        ProtectedUnifiedPayload rawPayload = Protect(rawPayloadJson);
        string persistedPayloadJson = JsonSerializer.Serialize(new
        {
            agentRunId = childRunId,
            agentVersionId = lease.AgentVersionId,
            eventKind = source.Kind.ToString(),
            text = Protect(source.Text).Content,
            argumentsJson = Protect(source.ArgumentsJson).Content,
            source.ErrorCode,
            source.ToolVersionId,
            source.ToolName,
            source.ToolCallId,
            source.SkillVersionId,
            source.SkillName,
            source.KnowledgeBaseCount,
            source.KnowledgeHitCount,
            capability = controlledBusinessQuery && source.Kind is
                AgentRunEventKind.ToolStarted or
                AgentRunEventKind.ToolSucceeded or
                AgentRunEventKind.ToolBlocked or
                AgentRunEventKind.ToolFailed
                    ? "business-query"
                    : string.Empty,
            reason = source.Kind == AgentRunEventKind.Started
                ? Protect(reason).Content
                : string.Empty
        });
        ProtectedUnifiedPayload persistedPayload = Protect(persistedPayloadJson);
        Guid entryRunId = (await _scope.GetAggregateSnapshotAsync(
            cancellationToken).ConfigureAwait(false)).Details.EntryRun.Id;
        await _scope.AppendEventAsync(sequence => new UnifiedRunEventRecord(
            Guid.NewGuid(),
            entryRunId,
            sequence,
            _scope.CorrelationId,
            MapEventKind(source.Kind),
            source.OccurredAtUtc,
            _parentRunId,
            lease.Depth,
            persistedPayload.Content,
            rawPayload.OriginalSha256),
            cancellationToken).ConfigureAwait(false);
    }
    #endregion

    #region 处理（PersistChildFailureAsync）
    /// <summary>
    /// 处理（PersistChildFailureAsync）
    /// </summary>
    /// <param name="childRunId">子运行标识。</param>
    /// <param name="depth">当前递归或执行树深度。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task PersistChildFailureAsync(Guid childRunId, int depth, UnifiedRunStatus status, string errorCode)
    {
        DateTimeOffset finishedAt = _scope.GetUtcNow();
        bool updated = false;
        await _scope.MutateAggregateAsync(aggregate =>
        {
            UnifiedAgentRunRecord? current = aggregate.Details.AgentRuns
                .FirstOrDefault(value => value.Id == childRunId);
            if (current is null
                || current.Status is UnifiedRunStatus.Completed
                    or UnifiedRunStatus.Failed
                    or UnifiedRunStatus.Cancelled)
            {
                return aggregate;
            }

            updated = true;
            UnifiedAgentRunRecord terminal = current with
            {
                Status = status,
                FinishedAtUtc = finishedAt,
                Duration = NonNegative(finishedAt - current.StartedAtUtc),
                ErrorCode = errorCode
            };
            return aggregate.WithDetails(new UnifiedRunDetails(
                aggregate.Details.EntryRun,
                aggregate.Details.AgentRuns
                    .Select(value => value.Id == childRunId ? terminal : value)
                    .ToArray(),
                aggregate.Details.Orchestrations,
                aggregate.Details.ToolCalls));
        }, CancellationToken.None).ConfigureAwait(false);
        if (!updated)
        {
            return;
        }

        ProtectedUnifiedPayload payload = Protect(JsonSerializer.Serialize(new
        {
            agentRunId = childRunId,
            errorCode
        }));
        Guid entryRunId = (await _scope.GetAggregateSnapshotAsync(
            CancellationToken.None).ConfigureAwait(false)).Details.EntryRun.Id;
        await _scope.AppendEventAsync(sequence => new UnifiedRunEventRecord(
            Guid.NewGuid(),
            entryRunId,
            sequence,
            _scope.CorrelationId,
            status == UnifiedRunStatus.Cancelled ? "cancelled" : "failed",
            finishedAt,
            _parentRunId,
            depth,
            payload.Content,
            payload.OriginalSha256),
            CancellationToken.None).ConfigureAwait(false);
    }
    #endregion

    #region 尝试执行（TryPersistChildFailureAsync）
    /// <summary>
    /// 尝试执行（TryPersistChildFailureAsync）
    /// </summary>
    /// <param name="childRunId">子运行标识。</param>
    /// <param name="depth">当前递归或执行树深度。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task TryPersistChildFailureAsync(Guid childRunId, int depth, UnifiedRunStatus status, string errorCode)
    {
        try
        {
            await PersistChildFailureAsync(
                childRunId,
                depth,
                status,
                errorCode).ConfigureAwait(false);
        }
        catch
        {
            // The internal-tool boundary returns only its stable primary error.
        }
    }
    #endregion

    #region 尝试执行（TryTerminateUnstartedAuditAsync）
    /// <summary>
    /// 尝试执行（TryTerminateUnstartedAuditAsync）
    /// </summary>
    /// <param name="context">已准备但尚未开始流式执行的子 Agent 上下文；为 null 时不更新审计。</param>
    /// <param name="runtimeStreamingStarted">运行时是否已开始流式输出。</param>
    /// <param name="status">当前操作使用的状态值。</param>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <returns>表示该异步操作完成的任务。</returns>
    private async Task TryTerminateUnstartedAuditAsync(AgentRunContext? context, bool runtimeStreamingStarted, AgentRunStatus status, string errorCode)
    {
        if (context is null || runtimeStreamingStarted)
        {
            return;
        }

        try
        {
            await _agentRuntime.TerminatePreparedRunAsync(
                context,
                status,
                errorCode,
                CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // The stable unified entry failure remains the primary error.
        }
    }
    #endregion

    #region 加密保护（Protect）
    /// <summary>
    /// 加密保护（Protect）
    /// </summary>
    /// <param name="value">待校验字节上限并脱敏的原始文本；null 按空字符串处理。</param>
    /// <returns>按当前执行范围内部载荷上限处理的脱敏内容、原始摘要及字节数。</returns>
    private ProtectedUnifiedPayload Protect(string? value) =>
        UnifiedEntryPayloadProtector.Protect(
            value,
            _scope.Limits.InternalPayloadUtf8Bytes,
            _scope.Limits.InternalPayloadUtf8Bytes);
    #endregion

    #region 取消（CancellationFailure）
    /// <summary>
    /// 取消（CancellationFailure）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <param name="lease">执行租约。</param>
    /// <returns>按执行范围取消原因分类的内部工具失败结果。</returns>
    private AgentInternalToolResult CancellationFailure(CancellationToken cancellationToken, UnifiedAgentExecutionLease? lease)
    {
        string errorCode = _scope.ClassifyCancellation(lease, cancellationToken);
        return Failure(errorCode, CancellationMessage(errorCode));
    }
    #endregion

    #region 取消（CancellationMessage）
    /// <summary>
    /// 取消（CancellationMessage）
    /// </summary>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <returns>与入口超时、子 Agent 超时或普通取消对应的提示文本。</returns>
    private static string CancellationMessage(string errorCode) =>
        errorCode switch
        {
            UnifiedEntryErrorCodes.EntryTimeout =>
                "The unified entry execution timed out.",
            UnifiedEntryErrorCodes.ChildTimeout =>
                "The child Agent execution timed out.",
            _ => "The unified entry execution was cancelled."
        };
    #endregion

    #region 映射（MapEventKind）
    /// <summary>
    /// 映射（MapEventKind）
    /// </summary>
    /// <param name="kind">记录或事件类型。</param>
    /// <returns>映射到统一入口协议的子 Agent 事件名称，未单独映射的类型使用 message。</returns>
    private static string MapEventKind(AgentRunEventKind kind) =>
        kind switch
        {
            AgentRunEventKind.Started => "child-agent-started",
            AgentRunEventKind.SkillStarted => "skill-started",
            AgentRunEventKind.KnowledgeRetrieved => "knowledge-retrieved",
            AgentRunEventKind.Citation => "knowledge-citation",
            AgentRunEventKind.ToolStarted => "tool-started",
            AgentRunEventKind.ToolSucceeded => "tool-succeeded",
            AgentRunEventKind.ToolBlocked => "tool-blocked",
            AgentRunEventKind.ToolFailed => "tool-failed",
            AgentRunEventKind.Completed => "child-agent-completed",
            AgentRunEventKind.Failed => "failed",
            AgentRunEventKind.Cancelled => "cancelled",
            _ => "message"
        };
    #endregion

    #region 判断 Agent 事件是否表示运行结束（IsTerminal）
    /// <summary>
    /// 判断 Agent 事件是否表示运行结束（IsTerminal）。
    /// </summary>
    /// <param name="kind">待检查的 Agent 运行事件类型。</param>
    /// <returns>事件类型为 Completed、Failed 或 Cancelled 时返回 true，否则返回 false。</returns>
    private static bool IsTerminal(AgentRunEventKind kind) =>
        kind is AgentRunEventKind.Completed
            or AgentRunEventKind.Failed
            or AgentRunEventKind.Cancelled;
    #endregion

    #region 处理（NonNegative）
    /// <summary>
    /// 将负时长归零（NonNegative）。
    /// </summary>
    /// <param name="value">待检查的持续时间。</param>
    /// <returns>输入为负数时返回零时长，否则返回原时长。</returns>
    private static TimeSpan NonNegative(TimeSpan value) =>
        value < TimeSpan.Zero ? TimeSpan.Zero : value;
    #endregion

    #region 处理（Failure）
    /// <summary>
    /// 处理（Failure）
    /// </summary>
    /// <param name="errorCode">失败对应的错误码。</param>
    /// <param name="content">内部工具失败时对调用方展示的安全提示。</param>
    /// <returns>包含指定内容和错误码、成功标志为 false 的内部工具结果。</returns>
    private static AgentInternalToolResult Failure(string errorCode, string content) =>
        new(false, content, errorCode);
    #endregion
}

internal sealed record InternalToolArguments(
    Guid VersionId,
    string Value,
    string Reason);

internal static class InternalToolArgumentParser
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    #region 尝试执行（TryParse）
    /// <summary>
    /// 尝试执行（TryParse）
    /// </summary>
    /// <param name="argumentsJson">工具调用参数的 JSON 文本。</param>
    /// <param name="versionPropertyName">保存版本号的属性名称。</param>
    /// <param name="valuePropertyName">保存值的属性名称。</param>
    /// <param name="maximumValueCharacters">字段值允许的最大字符数。</param>
    /// <param name="maximumReasonCharacters">原因说明允许的最大字符数。</param>
    /// <param name="maximumValueUtf8Bytes">字段值按 UTF-8 编码后允许的最大字节数。</param>
    /// <param name="arguments">调用参数。</param>
    /// <returns>操作是否成功；未满足执行条件或更新未生效时返回 false。</returns>
    public static bool TryParse(
        string? argumentsJson,
        string versionPropertyName,
        string valuePropertyName,
        int maximumValueCharacters,
        int maximumReasonCharacters,
        int maximumValueUtf8Bytes,
        out InternalToolArguments arguments)
    {
        arguments = new InternalToolArguments(Guid.Empty, string.Empty, string.Empty);
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return false;
        }

        try
        {
            if (StrictUtf8.GetByteCount(argumentsJson)
                > maximumValueUtf8Bytes)
            {
                return false;
            }

            using JsonDocument document = JsonDocument.Parse(argumentsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var properties = new Dictionary<string, JsonElement>(
                StringComparer.Ordinal);
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (property.Name is not "reason"
                    && property.Name != versionPropertyName
                    && property.Name != valuePropertyName)
                {
                    return false;
                }

                if (!properties.TryAdd(property.Name, property.Value)
                    || property.Value.ValueKind != JsonValueKind.String)
                {
                    return false;
                }
            }

            if (properties.Count != 3
                || !properties.TryGetValue(
                    versionPropertyName,
                    out JsonElement versionElement)
                || !properties.TryGetValue(
                    valuePropertyName,
                    out JsonElement valueElement)
                || !properties.TryGetValue("reason", out JsonElement reasonElement)
                || !Guid.TryParse(versionElement.GetString(), out Guid versionId)
                || versionId == Guid.Empty)
            {
                return false;
            }

            string value = valueElement.GetString()?.Trim() ?? string.Empty;
            string reason = reasonElement.GetString()?.Trim() ?? string.Empty;
            if (value.Length is 0
                || value.Length > maximumValueCharacters
                || reason.Length is 0
                || reason.Length > maximumReasonCharacters
                || StrictUtf8.GetByteCount(value) > maximumValueUtf8Bytes)
            {
                return false;
            }

            arguments = new InternalToolArguments(versionId, value, reason);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (EncoderFallbackException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
    #endregion
}
